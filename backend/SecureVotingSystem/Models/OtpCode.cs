using System.ComponentModel.DataAnnotations;

namespace SecureVotingSystem.Models
{
    public class OtpCode
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        [Required]
        public string Code { get; set; } = string.Empty;
        public DateTime ExpirationTime { get; set; }
        public bool IsUsed { get; set; } = false;
    }
}
