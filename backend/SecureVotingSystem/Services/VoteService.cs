using Microsoft.EntityFrameworkCore;
using SecureVotingSystem.Data;
using SecureVotingSystem.Models;

namespace SecureVotingSystem.Services
{
    public class VoteService : IVoteService
    {
        private readonly ApplicationDbContext _db;
        private readonly IActivityLogService _activityLog;

        public VoteService(ApplicationDbContext db, IActivityLogService activityLog)
        {
            _db = db;
            _activityLog = activityLog;
        }

        public async Task<bool> CastVoteAsync(int userId, int electionId, int candidateId)
        {
            // ensure election active
            var election = await _db.Elections.FindAsync(electionId);
            if (election == null || !election.IsActive) return false;
            var now = DateTime.UtcNow;
            if (now < election.StartDate || now > election.EndDate) return false;

            // duplicate vote prevention
            var existing = await _db.Votes.FirstOrDefaultAsync(v => v.UserId == userId && v.ElectionId == electionId);
            if (existing != null) return false;

            var vote = new Vote
            {
                UserId = userId,
                ElectionId = electionId,
                CandidateId = candidateId
            };
            _db.Votes.Add(vote);

            await _db.SaveChangesAsync();

            await _activityLog.LogAsync(userId, "VoteCast", $"Cast vote for candidate {candidateId} in election {electionId}");
            return true;
        }
    }
}
