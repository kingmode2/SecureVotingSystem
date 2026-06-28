using System.ComponentModel.DataAnnotations;

namespace SecureVotingSystem.DTOs
{
    public class CreateElectionDto
    {
        [Required]
        [StringLength(200, MinimumLength = 3)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(2000, MinimumLength = 10)]
        public string Description { get; set; } = string.Empty;
    }
}
