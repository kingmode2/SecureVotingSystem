using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace SecureVotingSystem.Services
{
    public interface IEmailService
    {
        Task SendOtpEmailAsync(string toEmail, string otp, CancellationToken cancellationToken = default);
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _logger = logger;

            _settings = new EmailSettings();
            config.GetSection("MailSettings").Bind(_settings);
        }

        public async Task SendOtpEmailAsync(string toEmail, string otp, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                throw new ArgumentException("Email is required", nameof(toEmail));

            if (string.IsNullOrWhiteSpace(otp))
                throw new ArgumentException("OTP is required", nameof(otp));

            var htmlBody = $@"
                <html>
                <body style='font-family: Arial;'>
                    <h2>Your OTP Code</h2>
                    <p>Your verification code is:</p>
                    <h1 style='letter-spacing:4px'>{otp}</h1>
                    <p>This code expires in 5 minutes.</p>
                </body>
                </html>";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = "OTP Verification Code";
            message.Body = new TextPart("html") { Text = htmlBody };

            using var smtp = new MailKit.Net.Smtp.SmtpClient();
            smtp.Timeout = _settings.TimeoutSeconds * 1000;

            try
            {
                // Choose secure socket options: use StartTls for port 587, SslOnConnect for 465
                SecureSocketOptions socketOptions = SecureSocketOptions.Auto;
                if (_settings.Port == 465) socketOptions = SecureSocketOptions.SslOnConnect;
                else if (_settings.Port == 587) socketOptions = SecureSocketOptions.StartTls;

                await smtp.ConnectAsync(_settings.Host, _settings.Port, socketOptions, cancellationToken);

                if (!string.IsNullOrWhiteSpace(_settings.Username))
                {
                    await smtp.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);
                }

                await smtp.SendAsync(message, cancellationToken);
                await smtp.DisconnectAsync(true, cancellationToken);

                _logger.LogInformation("OTP sent to {Email} using {Host}:{Port}", toEmail, _settings.Host, _settings.Port);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP email to {Email} via {Host}:{Port}", toEmail, _settings.Host, _settings.Port);
                throw;
            }
        }

        private class EmailSettings
        {
            public string Host { get; set; } = "localhost";
            public int Port { get; set; } = 1025; // MailHog default
            public bool EnableSsl { get; set; } = false;

            public string SenderEmail { get; set; } = "no-reply@securevoting.local";
            public string SenderName { get; set; } = "Secure Voting System";

            public string? Username { get; set; }
            public string? Password { get; set; }

            public int TimeoutSeconds { get; set; } = 30;
        }
    }
}