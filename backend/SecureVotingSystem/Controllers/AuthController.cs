using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureVotingSystem.DTOs;
using SecureVotingSystem.Services;

namespace SecureVotingSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;

        public AuthController(IAuthService auth)
        {
            _auth = auth;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray();
                return BadRequest(new { errors });
            }

            try
            {
                var result = await _auth.RegisterAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var result = await _auth.LoginAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
        {
            var result = await _auth.VerifyOtpAsync(dto);
            if (result == null || string.IsNullOrEmpty(result.Token)) return BadRequest(new { error = "Invalid or expired OTP" });
            return Ok(new { token = result.Token, role = result.Role, userId = result.UserId });
        }

        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpDto dto)
        {
            var sent = await _auth.ResendOtpAsync(dto);
            if (!sent) return BadRequest(new { error = "Unable to resend OTP" });
            return Ok(new { success = true });
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            var token = authHeader?.StartsWith("Bearer ") == true ? authHeader[7..].Trim() : string.Empty;
            if (string.IsNullOrEmpty(token)) return Unauthorized(new { error = "No token provided" });

            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId)) return Unauthorized(new { error = "Invalid user" });

            await _auth.LogoutAsync(token, userId);
            return Ok(new { success = true });
        }
    }
}
