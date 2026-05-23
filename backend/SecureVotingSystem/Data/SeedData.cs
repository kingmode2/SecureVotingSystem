using SecureVotingSystem.Models;

namespace SecureVotingSystem.Data
{
    public static class SeedData
    {
        public static void EnsureSeedData(ApplicationDbContext db)
        {
            if (!db.Users.Any(u => u.Email == "admin@local"))
            {
                var admin = new User
                {
                    FullName = "Admin User",
                    Email = "admin@local",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123"),
                    Role = "Admin",
                    IsVerified = true
                };
                db.Users.Add(admin);
                db.SaveChanges();
            }

            if (!db.Elections.Any())
            {
                var e = new Election { Title = "Sample Election", Description = "Demo election", StartDate = DateTime.UtcNow.AddMinutes(-10), EndDate = DateTime.UtcNow.AddDays(7), IsActive = true };
                db.Elections.Add(e);
                db.SaveChanges();
                db.Candidates.Add(new Candidate { ElectionId = e.Id, Name = "Alice", Party = "A" });
                db.Candidates.Add(new Candidate { ElectionId = e.Id, Name = "Bob", Party = "B" });
                db.SaveChanges();
            }
        }
    }
}
