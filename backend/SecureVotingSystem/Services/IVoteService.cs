using SecureVotingSystem.Models;

namespace SecureVotingSystem.Services
{
    public interface IVoteService
    {
        Task<bool> CastVoteAsync(int userId, int electionId, int candidateId);
    }
}
