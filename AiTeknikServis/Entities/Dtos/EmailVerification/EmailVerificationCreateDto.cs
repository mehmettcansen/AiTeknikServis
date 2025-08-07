using System.ComponentModel.DataAnnotations;
using AiTeknikServis.Entities.Models;

namespace AiTeknikServis.Entities.Dtos.EmailVerification
{
    /// <summary>
    /// Email doğrulama kodu oluşturma DTO'su
    /// </summary>
    public class EmailVerificationCreateDto
    {
        [Required(ErrorMessage = "Email adresi gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz")]
        [MaxLength(100, ErrorMessage = "Email adresi en fazla 100 karakter olabilir")]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Doğrulama türü gereklidir")]
        public EmailVerificationType Type { get; set; }
        
        [MaxLength(200, ErrorMessage = "Amaç açıklaması en fazla 200 karakter olabilir")]
        public string? Purpose { get; set; }
        
        [MaxLength(500, ErrorMessage = "Ek veriler en fazla 500 karakter olabilir")]
        public string? AdditionalData { get; set; }
        
        /// <summary>
        /// Doğrulama kodunun geçerlilik süresi (dakika cinsinden, varsayılan 15 dakika)
        /// </summary>
        public int ExpiryMinutes { get; set; } = 15;
        
        /// <summary>
        /// Maksimum deneme sayısı (varsayılan 3)
        /// </summary>
        public int MaxRetries { get; set; } = 3;
    }
}
