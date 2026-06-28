using System.ComponentModel.DataAnnotations;

namespace SecureVotingSystem.Models
{
    public class RevokedToken
    {
        public int Id { get; set; }
        [Required]
        public string TokenHash { get; set; } = string.Empty;
        public int UserId { get; set; }
        public DateTime ExpirationTime { get; set; }
        public DateTime RevokedAt { get; set; } = DateTime.UtcNow;
    }
}
