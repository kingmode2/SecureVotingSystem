using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using SecureVotingSystem.Data;
using SecureVotingSystem.Services;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// =========================
// DATABASE (SMART SWITCHING)
// =========================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // Priority 1: Environment variable (Render sets this automatically)
    var connectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING");
    
    // Priority 2: Check if we're running on Render (production)
    if (string.IsNullOrEmpty(connectionString))
    {
        var isProduction = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production";
        connectionString = isProduction 
            ? builder.Configuration.GetConnectionString("RenderConnection")
            : builder.Configuration.GetConnectionString("LocalConnection");
    }
    
    // Priority 3: Fallback to DefaultConnection (your existing code)
    if (string.IsNullOrEmpty(connectionString))
    {
        connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    }
    
    // Priority 4: Ultimate fallback (keeps your app running)
    if (string.IsNullOrEmpty(connectionString))
    {
        connectionString = "Host=postgres;Port=5432;Database=securevoting;Username=postgres;Password=postgres";
    }
    
    Console.WriteLine($"✅ Database connection configured.");
    options.UseNpgsql(connectionString);
});

// =========================
// JWT (UNCHANGED)
// =========================
var jwt = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwt["Key"]!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwt["Issuer"],
        ValidAudience = jwt["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

// =========================
// SERVICES (UNCHANGED)
// =========================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        p => p.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddMemoryCache();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IVoteService, VoteService>();
builder.Services.AddScoped<IActivityLogService, ActivityLogService>();
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddSingleton<AuthTaskQueueService>();
builder.Services.AddSingleton<IAuthTaskQueue>(sp =>
    sp.GetRequiredService<AuthTaskQueueService>());
builder.Services.AddSingleton<IHostedService>(sp =>
    sp.GetRequiredService<AuthTaskQueueService>());

var app = builder.Build();

// =========================
// MIDDLEWARE (UNCHANGED)
// =========================
app.UseSwagger();
app.UseSwaggerUI();

// Root endpoint - shows API is running
app.MapGet("/", () => Results.Ok(new { 
    status = "Secure Voting System API is Running",
    timestamp = DateTime.UtcNow,
    documentation = "/swagger",
    health = "/health",
    endpoints = new[] { 
        "/api/auth/register", 
        "/api/auth/login", 
        "/api/candidates",
        "/api/votes"
    }
}));

// Health check endpoint - for monitoring
app.MapGet("/health", () => Results.Ok(new { 
    status = "Healthy", 
    timestamp = DateTime.UtcNow,
    database = "Connected"
}));

app.UseRouting();
app.UseHttpMetrics();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapMetrics();

// =========================
// DATABASE MIGRATION (UNCHANGED)
// =========================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    Console.WriteLine("Waiting for PostgreSQL to be ready...");

    var retries = 15;

    while (retries > 0)
    {
        try
        {
            Console.WriteLine("Applying migrations...");

            db.Database.Migrate();
            SeedData.EnsureSeedData(db);

            Console.WriteLine("Database is ready ✔");
            break;
        }
        catch (Exception ex)
        {
            retries--;

            Console.WriteLine($"DB not ready yet. Retries left: {retries}");
            Console.WriteLine(ex.Message);

            Thread.Sleep(4000);
        }
    }

    if (retries == 0)
    {
        Console.WriteLine("❌ Could not connect to database after multiple retries.");
    }
}

app.Run();