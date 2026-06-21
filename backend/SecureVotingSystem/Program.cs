using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using SecureVotingSystem.Data;
using SecureVotingSystem.Services;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// =========================
// DATABASE (CLEAN + FIXED)
// =========================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString =
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING")
        ?? "Host=postgres;Port=5432;Database=securevoting;Username=postgres;Password=postgres";

    options.UseNpgsql(connectionString);
});

// =========================
// JWT
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
// SERVICES
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
// MIDDLEWARE
// =========================
app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();
app.UseHttpMetrics();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapMetrics();

// =========================
// DATABASE MIGRATION (FIXED + RETRY)
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