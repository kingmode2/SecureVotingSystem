using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using SecureVotingSystem.Data;
using SecureVotingSystem.Services;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// DB
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionStrings = new[]
    {
        builder.Configuration.GetConnectionString("DefaultConnection"),
        Environment.GetEnvironmentVariable("SQLSERVER_CONNECTION_STRING"),
        "Server=localhost\\SQLEXPRESS;Database=SecureVotingDB;Trusted_Connection=True;TrustServerCertificate=True;",
        "Server=(localdb)\\MSSQLLocalDB;Database=SecureVotingDB;Trusted_Connection=True;TrustServerCertificate=True;"
    }
    .Where(cs => !string.IsNullOrWhiteSpace(cs))
    .ToArray();

    string? selectedConnection = null;
    foreach (var cs in connectionStrings)
    {
        try
        {
            using var testConnection = new Microsoft.Data.SqlClient.SqlConnection(cs);
            testConnection.Open();
            selectedConnection = cs;
            testConnection.Close();
            break;
        }
        catch
        {
            // ignore and try the next configured connection string
        }
    }

    if (selectedConnection is null)
    {
        var sqliteDbPath = Path.Combine(AppContext.BaseDirectory, "SecureVoting.db");
        Console.WriteLine($"SQL Server unavailable, falling back to SQLite at {sqliteDbPath}.");
        options.UseSqlite($"Data Source={sqliteDbPath}");
    }
    else
    {
        options.UseSqlServer(selectedConnection);
    }
});

// JWT
var jwt = configuration.GetSection("Jwt");
var keyStr = jwt["Key"] ?? throw new Exception("JWT Key not configured. Set Jwt:Key in appsettings.json");
var key = Encoding.UTF8.GetBytes(keyStr);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
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
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var rawToken = context.Request.Headers["Authorization"].ToString();
            if (!rawToken.StartsWith("Bearer "))
            {
                context.Fail("Missing bearer token");
                return;
            }

            rawToken = rawToken[7..].Trim();
            if (string.IsNullOrEmpty(rawToken))
            {
                context.Fail("Invalid token");
                return;
            }

            var tokenHash = ComputeSha256Hash(rawToken);
            var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
            var revoked = await db.RevokedTokens.AnyAsync(rt => rt.TokenHash == tokenHash);
            if (revoked)
            {
                context.Fail("Token revoked");
            }
        }
    };
});

static string ComputeSha256Hash(string rawData)
{
    using var sha256 = SHA256.Create();
    var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
    return Convert.ToHexString(bytes);
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

// Memory cache for request deduplication
builder.Services.AddMemoryCache();

// Application services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IVoteService, VoteService>();
builder.Services.AddScoped<IActivityLogService, ActivityLogService>();

// Background task queue for non-blocking OTP operations
builder.Services.AddSingleton<AuthTaskQueueService>();
builder.Services.AddSingleton<IAuthTaskQueue>(sp => sp.GetRequiredService<AuthTaskQueueService>());
builder.Services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<AuthTaskQueueService>());

var app = builder.Build();

// MIDDLEWARE
app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();
app.UseCors("AllowLocalhost");
app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

// apply seed data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        db.Database.Migrate();
        SeedData.EnsureSeedData(db);
    }
    catch (Exception ex)
    {
        // Log full exception (includes inner exceptions and stack trace)
        Console.WriteLine($"Warning: could not initialize database: {ex}");
    }
}

app.Run();
