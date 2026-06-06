using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;
using SecureVotingSystem.Data;
using SecureVotingSystem.Services;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// =========================
// DATABASE
// =========================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionStrings = new[]
    {
        builder.Configuration.GetConnectionString("DefaultConnection"),
        Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING"),
        Environment.GetEnvironmentVariable("SQLSERVER_CONNECTION_STRING"),
        "Host=postgres;Port=5432;Database=securevoting;Username=postgres;Password=postgres",
        "Server=localhost\\SQLEXPRESS;Database=SecureVotingDB;Trusted_Connection=True;TrustServerCertificate=True;"
    }
    .Where(cs => !string.IsNullOrWhiteSpace(cs))
    .ToArray();

    string? selectedConnection = null;
    bool isPostgres = false;

    foreach (var cs in connectionStrings)
    {
        try
        {
            try
            {
                using var pg = new Npgsql.NpgsqlConnection(cs);
                pg.Open();
                selectedConnection = cs;
                isPostgres = true;
                break;
            }
            catch { }

            try
            {
                using var sql = new Microsoft.Data.SqlClient.SqlConnection(cs);
                sql.Open();
                selectedConnection = cs;
                isPostgres = false;
                break;
            }
            catch { }
        }
        catch { }
    }

    if (selectedConnection is null)
    {
        options.UseSqlite("Data Source=SecureVoting.db");
    }
    else if (isPostgres)
    {
        options.UseNpgsql(selectedConnection);
    }
    else
    {
        options.UseSqlServer(selectedConnection);
    }
});

// =========================
// JWT
// =========================
var jwt = configuration.GetSection("Jwt");
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
    options.AddPolicy("AllowLocalhost", p =>
        p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
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

app.UseCors("AllowLocalhost");

app.UseAuthentication();
app.UseAuthorization();

app.MapMetrics();
app.MapControllers();

// =========================
// SEED DATABASE (BEFORE RUN)
// =========================
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
        Console.WriteLine($"DB init error: {ex}");
    }
}

// =========================
// START APP
// =========================
app.Run();