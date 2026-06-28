namespace SecureVotingSystem.Services
{
    public interface IAuthTaskQueue
    {
        /// <summary>
        /// Queue an OTP generation and email task to run in the background
        /// </summary>
        ValueTask QueueOtpTaskAsync(int userId, string email, string otp);

        /// <summary>
        /// Queue an OTP resend task to run in the background
        /// </summary>
        ValueTask QueueResendOtpTaskAsync(int userId, string email, string otp);

        /// <summary>
        /// Check if a login request is a duplicate
        /// </summary>
        bool IsDuplicateLoginRequest(string email);

        /// <summary>
        /// Mark a login request as processed
        /// </summary>
        void MarkLoginRequestProcessed(string email);
    }
}
