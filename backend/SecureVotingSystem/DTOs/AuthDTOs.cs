namespace SecureVotingSystem.DTOs
{
    public class RegisterDto
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string FullName { get; set; } = string.Empty;
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.EmailAddress]
        public string Email { get; set; } = string.Empty;
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.MinLength(8)]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class VerifyOtpDto
    {
        public int UserId { get; set; }
        public string Code { get; set; } = string.Empty;
    }

    public class ResendOtpDto
    {
        public int UserId { get; set; }
    }

    public class AuthResultDto
    {
        public string Token { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string Role { get; set; } = string.Empty;
    }
}