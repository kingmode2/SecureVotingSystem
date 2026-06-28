using SecureVotingSystem.Models;

namespace SecureVotingSystem.Data
{
    public static class SeedData
    {
        public static void EnsureSeedData(ApplicationDbContext db)
        {
            var admin = db.Users.FirstOrDefault(u => u.Email == "admin@local");
            if (admin == null)
            {
                admin = new User
                {
                    FullName = "Admin User",
                    Email = "admin@local",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                    Role = "Admin",
                    IsVerified = true
                };
                db.Users.Add(admin);
                db.SaveChanges();
            }
            else if (!BCrypt.Net.BCrypt.Verify("Admin123!", admin.PasswordHash))
            {
                admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!");
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
            else
            {
                var sampleElection = db.Elections.FirstOrDefault(e => e.Title == "Sample Election");
                if (sampleElection != null && (sampleElection.EndDate < DateTime.UtcNow || !sampleElection.IsActive))
                {
                    sampleElection.StartDate = DateTime.UtcNow.AddMinutes(-10);
                    sampleElection.EndDate = DateTime.UtcNow.AddDays(7);
                    sampleElection.IsActive = true;
                    db.SaveChanges();
                }
            }
        }
    }
}
