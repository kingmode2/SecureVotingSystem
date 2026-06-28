using SecureVotingSystem.DTOs;

namespace SecureVotingSystem.Services
{
    public interface IAuthService
    {
        Task<AuthResultDto> RegisterAsync(RegisterDto dto);
        Task<AuthResultDto> LoginAsync(LoginDto dto);
        Task<AuthResultDto?> VerifyOtpAsync(VerifyOtpDto dto);
        Task<bool> ResendOtpAsync(ResendOtpDto dto);
        Task LogoutAsync(string token, int userId);
    }
}
