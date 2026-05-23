using System.Threading.Tasks;

namespace SecureVotingSystem.Services
{
    public interface IActivityLogService
    {
        Task LogAsync(int? userId, string action, string description);
    }
}
