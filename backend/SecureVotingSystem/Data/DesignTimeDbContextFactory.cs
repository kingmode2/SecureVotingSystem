using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace SecureVotingSystem.Data
{
    // Ensures design-time tools (dotnet ef) use the Postgres provider
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables();

            var config = builder.Build();
            var cs = config.GetConnectionString("DefaultConnection") ?? Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING");
            if (string.IsNullOrWhiteSpace(cs))
            {
                cs = "Host=postgres;Port=5432;Database=securevoting;Username=postgres;Password=postgres";
            }

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseNpgsql(cs);
            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
