using System.ComponentModel.DataAnnotations;

namespace SecureVotingSystem.Models
{
    public class Candidate
    {
        public int Id { get; set; }
        public int ElectionId { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        public string Party { get; set; } = string.Empty;
    }
}
