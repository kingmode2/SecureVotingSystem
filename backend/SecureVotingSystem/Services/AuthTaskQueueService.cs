using System.Linq;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SecureVotingSystem.Data;
using SecureVotingSystem.Models;

namespace SecureVotingSystem.Services
{
    public class OtpTaskItem
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
    }

    public class AuthTaskQueueService : BackgroundService, IAuthTaskQueue
    {
        private readonly Channel<OtpTaskItem> _channel = Channel.CreateUnbounded<OtpTaskItem>();
        private readonly IServiceProvider _serviceProvider;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<AuthTaskQueueService> _logger;
        private const string DUPLICATE_REQUEST_KEY = "login_request_{0}";
        private const int DUPLICATE_REQUEST_WINDOW_SECONDS = 2; // Prevent duplicates within 2 seconds

        public AuthTaskQueueService(IServiceProvider serviceProvider, IMemoryCache memoryCache, ILogger<AuthTaskQueueService> logger)
        {
            _serviceProvider = serviceProvider;
            _memoryCache = memoryCache;
            _logger = logger;
        }

        private static string NormalizeEmail(string email)
        {
            return string.IsNullOrWhiteSpace(email)
                ? string.Empty
                : email.Trim().ToLowerInvariant();
        }

        public bool IsDuplicateLoginRequest(string email)
        {
            var key = string.Format(DUPLICATE_REQUEST_KEY, NormalizeEmail(email));
            return _memoryCache.TryGetValue(key, out _);
        }

        public void MarkLoginRequestProcessed(string email)
        {
            var key = string.Format(DUPLICATE_REQUEST_KEY, NormalizeEmail(email));
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(DUPLICATE_REQUEST_WINDOW_SECONDS));
            _memoryCache.Set(key, true, cacheOptions);
        }

        public ValueTask QueueOtpTaskAsync(int userId, string email, string otp)
        {
            var item = new OtpTaskItem { UserId = userId, Email = email, Otp = otp };
            if (_channel.Writer.TryWrite(item))
            {
                return ValueTask.CompletedTask;
            }

            return _channel.Writer.WriteAsync(item);
        }

        public ValueTask QueueResendOtpTaskAsync(int userId, string email, string otp)
        {
            // Resend is the same as initial send - just queue it
            var item = new OtpTaskItem { UserId = userId, Email = email, Otp = otp };
            if (_channel.Writer.TryWrite(item))
            {
                return ValueTask.CompletedTask;
            }

            return _channel.Writer.WriteAsync(item);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var item in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await ProcessOtpTaskAsync(item, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing OTP task for {Email}", item.Email);
                }
            }
        }

        private async Task ProcessOtpTaskAsync(OtpTaskItem item, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            try
            {
                // Mark all previous unused OTPs as used in a single update, then enqueue the new OTP
                await db.OtpCodes
                    .Where(o => o.UserId == item.UserId && !o.IsUsed)
                    .ExecuteUpdateAsync(o => o.SetProperty(otp => otp.IsUsed, true), cancellationToken);

                var otpEntry = new OtpCode
                {
                    UserId = item.UserId,
                    Code = item.Otp,
                    ExpirationTime = DateTime.UtcNow.AddMinutes(5),
                    IsUsed = false
                };

                db.OtpCodes.Add(otpEntry);
                await db.SaveChangesAsync(cancellationToken);

                // Send OTP email asynchronously without blocking the request pipeline
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                await emailService.SendOtpEmailAsync(item.Email, item.Otp, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process OTP task for {Email}", item.Email);
                throw;
            }
        }
    }
}
