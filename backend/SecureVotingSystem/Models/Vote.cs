using System.ComponentModel.DataAnnotations;

namespace SecureVotingSystem.Models
{
    public class Vote
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ElectionId { get; set; }
        public int CandidateId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
