using System.ComponentModel.DataAnnotations;

namespace AiTeknikServis.Entities.Models
{
    /// <summary>
    /// Email doğrulama kodlarını tutan entity
    /// </summary>
    public class EmailVerification
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(6)]
        public string VerificationCode { get; set; } = string.Empty;
        
        public EmailVerificationType Type { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        
        public DateTime ExpiryDate { get; set; }
        
        public bool IsUsed { get; set; } = false;
        
        public DateTime? UsedDate { get; set; }
        
        [MaxLength(200)]
        public string? Purpose { get; set; }
        
        [MaxLength(500)]
        public string? AdditionalData { get; set; } // JSON formatında ek veriler
        
        public int RetryCount { get; set; } = 0;
        
        public int MaxRetries { get; set; } = 3;
        
        /// <summary>
        /// Doğrulama kodunun geçerli olup olmadığını kontrol eder
        /// </summary>
        public bool IsValid => !IsUsed && DateTime.UtcNow <= ExpiryDate && RetryCount < MaxRetries;
        
        /// <summary>
        /// Doğrulama kodunun süresinin dolup dolmadığını kontrol eder
        /// </summary>
        public bool IsExpired => DateTime.UtcNow > ExpiryDate;
    }
    
    /// <summary>
    /// Email doğrulama türleri
    /// </summary>
    public enum EmailVerificationType
    {
        /// <summary>
        /// Müşteri kayıt doğrulaması
        /// </summary>
        CustomerRegistration = 1,
        
        /// <summary>
        /// Admin/Manager/Teknisyen oluşturma doğrulaması
        /// </summary>
        UserCreation = 2,
        
        /// <summary>
        /// Şifre sıfırlama doğrulaması
        /// </summary>
        PasswordReset = 3,
        
        /// <summary>
        /// Email değişikliği doğrulaması
        /// </summary>
        EmailChange = 4,
        
        /// <summary>
        /// Hesap aktivasyonu
        /// </summary>
        AccountActivation = 5
    }
}