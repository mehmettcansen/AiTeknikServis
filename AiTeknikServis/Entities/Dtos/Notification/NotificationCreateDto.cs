using System.ComponentModel.DataAnnotations;
using AiTeknikServis.Entities.Models;

namespace AiTeknikServis.Entities.Dtos.Notification
{
    public class NotificationCreateDto
    {
        [Required(ErrorMessage = "Başlık alanı zorunludur")]
        [StringLength(200, ErrorMessage = "Başlık en fazla 200 karakter olabilir")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mesaj alanı zorunludur")]
        [StringLength(1000, ErrorMessage = "Mesaj en fazla 1000 karakter olabilir")]
        public string Message { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bildirim türü zorunludur")]
        public NotificationType Type { get; set; }

        [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz")]
        [StringLength(100, ErrorMessage = "Email en fazla 100 karakter olabilir")]
        public string? RecipientEmail { get; set; }

        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
        [StringLength(20, ErrorMessage = "Telefon numarası en fazla 20 karakter olabilir")]
        public string? RecipientPhone { get; set; }

        public int? ServiceRequestId { get; set; }

        public int? UserId { get; set; }
    }
}