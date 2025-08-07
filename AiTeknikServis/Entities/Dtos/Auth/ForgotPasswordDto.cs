using System.ComponentModel.DataAnnotations;

namespace AiTeknikServis.Entities.Dtos.Auth
{
    /// <summary>
    /// Şifremi unuttum DTO'su
    /// </summary>
    public class ForgotPasswordDto
    {
        [Required(ErrorMessage = "Email adresi gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz")]
        [Display(Name = "Email Adresi")]
        public string Email { get; set; } = string.Empty;
    }
}