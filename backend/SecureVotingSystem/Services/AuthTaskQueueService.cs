using System.Linq;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        private const string DUPLICATE_REQUEST_KEY = "login_request_{0}";
        private const int DUPLICATE_REQUEST_WINDOW_SECONDS = 2; // Prevent duplicates within 2 seconds

        public AuthTaskQueueService(IServiceProvider serviceProvider, IMemoryCache memoryCache)
        {
            _serviceProvider = serviceProvider;
            _memoryCache = memoryCache;
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
                    System.Diagnostics.Debug.WriteLine($"[AuthTaskQueue] Error processing OTP task for {item.Email}: {ex.Message}");
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
                await SendOtpEmailAsync(item.Email, item.Otp, config);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AuthTaskQueue] Failed to process OTP for {item.Email}: {ex.Message}");
            }
        }

        private async Task SendOtpEmailAsync(string destinationEmail, string otp, IConfiguration config)
        {
            try
            {
                var mailSection = config.GetSection("MailSettings");
                var host = mailSection["Host"] ?? "localhost";
                var port = int.TryParse(mailSection["Port"], out var parsedPort) ? parsedPort : 1025;
                var enableSsl = bool.TryParse(mailSection["EnableSsl"], out var ssl) ? ssl : false;
                var senderEmail = mailSection["SenderEmail"] ?? "no-reply@securevoting.local";
                var senderName = mailSection["SenderName"] ?? "Secure Voting System";
                var timeoutSeconds = int.TryParse(mailSection["TimeoutSeconds"], out var timeout) ? timeout : 5;

                using var message = new System.Net.Mail.MailMessage();
                message.From = new System.Net.Mail.MailAddress(senderEmail, senderName);
                message.To.Add(new System.Net.Mail.MailAddress(destinationEmail));
                message.Subject = "Your SecureVoting OTP Code";

                var htmlBody = $@"
                        <html>
                        <body style='font-family: Arial, sans-serif; color:#333;'>
                            <h2 style='color:#2a6fdb;'>Your SecureVoting OTP</h2>
                            <p>Your one-time verification code is:</p>
                            <div style='font-size:24px; font-weight:bold; margin:12px 0; letter-spacing:4px;'>{otp}</div>
                            <p style='color:#666;'>This code is valid for 5 minutes. If you did not request this, please ignore this email.</p>
                            <hr />
                            <p style='font-size:12px; color:#999;'>SecureVoting System</p>
                        </body>
                        </html>
                ";

                message.Body = htmlBody;
                message.IsBodyHtml = true;

                using var smtpClient = new System.Net.Mail.SmtpClient(host, port)
                {
                    EnableSsl = enableSsl,
                    Timeout = timeoutSeconds * 1000
                };

                await smtpClient.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SMTP] Failed to send OTP email to {destinationEmail}: {ex.Message}");
            }
        }
    }
}
