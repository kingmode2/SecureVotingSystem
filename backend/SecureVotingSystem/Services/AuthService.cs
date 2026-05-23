using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SecureVotingSystem.Data;
using SecureVotingSystem.DTOs;
using SecureVotingSystem.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SecureVotingSystem.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;
        private readonly IAuthTaskQueue _taskQueue;
        private readonly IActivityLogService _activityLog;
        private const int BCRYPT_COST = 11; // Reduced from default 12 for better performance (still secure)

        public AuthService(ApplicationDbContext db, IConfiguration config, IAuthTaskQueue taskQueue, IActivityLogService activityLog)
        {
            _db = db;
            _config = config;
            _taskQueue = taskQueue;
            _activityLog = activityLog;
        }

        public async Task<AuthResultDto> RegisterAsync(RegisterDto dto)
        {
            // validate email uniqueness
            if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
                throw new Exception("Email already registered");

            // password strength validation: min 8, uppercase, lowercase, digit, special
            if (!IsStrongPassword(dto.Password))
                throw new Exception("Password must be at least 8 characters and include uppercase, lowercase, digit and special character.");

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, BCRYPT_COST),
                Role = "Voter",
                IsVerified = false
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // No OTP generation on registration. OTP is sent only when the user logs in.
            return new AuthResultDto { UserId = user.Id, Token = string.Empty, Role = user.Role };
        }

        private bool IsStrongPassword(string pwd)
        {
            if (string.IsNullOrEmpty(pwd) || pwd.Length < 8) return false;
            bool hasUpper = pwd.Any(char.IsUpper);
            bool hasLower = pwd.Any(char.IsLower);
            bool hasDigit = pwd.Any(char.IsDigit);
            bool hasSpecial = pwd.Any(ch => !char.IsLetterOrDigit(ch));
            return hasUpper && hasLower && hasDigit && hasSpecial;
        }

        public async Task<AuthResultDto> LoginAsync(LoginDto dto)
        {
            try
            {
            // Check for duplicate requests - instant return if detected
            if (_taskQueue.IsDuplicateLoginRequest(dto.Email))
                throw new Exception("Too many login attempts. Please try again later.");

            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
            {
                await _activityLog.LogAsync(null, "LoginFailed", $"Email: {dto.Email}");
                throw new Exception("Invalid credentials");
            }
            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                await _activityLog.LogAsync(user.Id, "LoginFailed", $"Email: {dto.Email}");
                throw new Exception("Invalid credentials");
            }

            // Mark this login request as processed (within deduplication window)
            _taskQueue.MarkLoginRequestProcessed(dto.Email);

            // Generate OTP
            var otp = GenerateOtpCode();

            // Queue OTP generation and email sending - DON'T AWAIT!
            // This allows the login endpoint to respond instantly
            _ = _taskQueue.QueueOtpTaskAsync(user.Id, user.Email, otp);

            // Log OTP send and login success sequentially to avoid DbContext concurrency issues
            await _activityLog.LogAsync(user.Id, "LoginSuccess", "Credentials verified; OTP queued");
            await _activityLog.LogAsync(user.Id, "OtpSent", $"OTP queued for {user.Email}");

            return new AuthResultDto { UserId = user.Id, Token = string.Empty, Role = user.Role };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AuthService.LoginAsync] Exception: {ex}");
                throw;
            }
        }

        public async Task<AuthResultDto?> VerifyOtpAsync(VerifyOtpDto dto)
        {
            try
            {
            var now = DateTime.UtcNow;
            var otp = await _db.OtpCodes
                .Where(o => o.UserId == dto.UserId && o.Code == dto.Code && !o.IsUsed && o.ExpirationTime >= now)
                .OrderByDescending(o => o.ExpirationTime)
                .FirstOrDefaultAsync();
            if (otp == null) return null;

            var user = await _db.Users.FindAsync(dto.UserId);
            if (user == null) return null;

            // Combined save: mark OTP as used AND mark user as verified in single DB roundtrip
            otp.IsUsed = true;
            user.IsVerified = true;
            await _db.SaveChangesAsync();
            // Log OTP verification
            await _activityLog.LogAsync(user.Id, "OtpVerified", $"OTP code {dto.Code} verified");

            var token = GenerateJwtToken(user);
            return new AuthResultDto { Token = token, UserId = user.Id, Role = user.Role };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AuthService.VerifyOtpAsync] Exception: {ex}");
                throw;
            }
        }

        public async Task<bool> ResendOtpAsync(ResendOtpDto dto)
        {
            // Check for duplicate requests
            var user = await _db.Users.FindAsync(dto.UserId);
            if (user == null) return false;

            if (_taskQueue.IsDuplicateLoginRequest(user.Email))
                return false; // Silently fail to prevent spam without blocking

            // Mark this resend request as processed (within deduplication window)
            _taskQueue.MarkLoginRequestProcessed(user.Email);

            // Queue the OTP resend operation - DON'T AWAIT!
            var otpCode = GenerateOtpCode();
            _ = _taskQueue.QueueResendOtpTaskAsync(user.Id, user.Email, otpCode);
            await _activityLog.LogAsync(user.Id, "OtpSent", $"OTP resent to {user.Email}");
            return true;
        }

        public async Task LogoutAsync(string token, int userId)
        {
            if (string.IsNullOrWhiteSpace(token)) return;

            var tokenHash = ComputeSha256Hash(token);
            if (await _db.RevokedTokens.AnyAsync(rt => rt.TokenHash == tokenHash)) return;

            var handler = new JwtSecurityTokenHandler();
            JwtSecurityToken jwtToken;
            try
            {
                jwtToken = handler.ReadJwtToken(token);
            }
            catch
            {
                return;
            }

            var expiresAt = jwtToken.Payload.Expiration.HasValue
                ? DateTimeOffset.FromUnixTimeSeconds(jwtToken.Payload.Expiration.Value).UtcDateTime
                : DateTime.UtcNow;

            _db.RevokedTokens.Add(new Models.RevokedToken
            {
                UserId = userId,
                TokenHash = tokenHash,
                ExpirationTime = expiresAt,
                RevokedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            await _activityLog.LogAsync(userId, "Logout", "User logged out and token revoked");
        }

        private static string ComputeSha256Hash(string rawData)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            return Convert.ToHexString(bytes);
        }

        private static string GenerateOtpCode()
        {
            var value = RandomNumberGenerator.GetInt32(100000, 1000000);
            return value.ToString("D6");
        }

        private string GenerateJwtToken(User user)
        {
            var keyStr = _config["Jwt:Key"] ?? throw new Exception("JWT Key not configured");
            var key = Encoding.UTF8.GetBytes(keyStr);
            var tokenHandler = new JwtSecurityTokenHandler();
            var claims = new[] {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(double.TryParse(_config["Jwt:ExpireMinutes"], out var m) ? m : 60),
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
