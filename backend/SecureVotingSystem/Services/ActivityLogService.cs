using SecureVotingSystem.Data;
using SecureVotingSystem.Models;

namespace SecureVotingSystem.Services
{
    public class ActivityLogService : IActivityLogService
    {
        private readonly ApplicationDbContext _db;

        public ActivityLogService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task LogAsync(int? userId, string action, string description)
        {
            var entry = new ActivityLog
            {
                UserId = userId,
                Action = action ?? string.Empty,
                Description = description ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };
            _db.ActivityLogs.Add(entry);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log full exception but do not throw — activity logging must not block core flows
                Console.WriteLine($"[ActivityLogService] Failed to save activity log: {ex}");
            }
        }
    }
}
