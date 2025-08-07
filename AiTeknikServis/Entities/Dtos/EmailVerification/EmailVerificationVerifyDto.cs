using System.ComponentModel.DataAnnotations;
using AiTeknikServis.Entities.Models;

namespace AiTeknikServis.Entities.Dtos.EmailVerification
{
    /// <summary>
    /// Email doğrulama kodu doğrulama DTO'su
    /// </summary>
    public class EmailVerificationVerifyDto
    {
        [Required(ErrorMessage = "Email adresi gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz")]
        [MaxLength(100, ErrorMessage = "Email adresi en fazla 100 karakter olabilir")]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Doğrulama kodu gereklidir")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Doğrulama kodu 6 haneli olmalıdır")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Doğrulama kodu sadece rakamlardan oluşmalıdır")]
        public string VerificationCode { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Doğrulama türü gereklidir")]
        public EmailVerificationType Type { get; set; }
    }
}