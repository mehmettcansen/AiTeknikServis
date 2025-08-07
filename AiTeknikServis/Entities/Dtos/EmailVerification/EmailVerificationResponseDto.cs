using AiTeknikServis.Entities.Models;

namespace AiTeknikServis.Entities.Dtos.EmailVerification
{
    /// <summary>
    /// Email doğrulama yanıt DTO'su
    /// </summary>
    public class EmailVerificationResponseDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string VerificationCode { get; set; } = string.Empty;
        public EmailVerificationType Type { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsUsed { get; set; }
        public DateTime? UsedDate { get; set; }
        public string? Purpose { get; set; }
        public int RetryCount { get; set; }
        public int MaxRetries { get; set; }
        public bool IsValid { get; set; }
        public bool IsExpired { get; set; }
        public int RemainingMinutes { get; set; }
        public string TypeDisplayName { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Email doğrulama sonuç DTO'su
    /// </summary>
    public class EmailVerificationResultDto
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ErrorCode { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int? RemainingAttempts { get; set; }
        public bool CanRetry { get; set; } = true;
        public string? NextActionUrl { get; set; }
    }
}