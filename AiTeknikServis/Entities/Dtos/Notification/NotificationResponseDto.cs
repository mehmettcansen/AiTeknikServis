using AiTeknikServis.Entities.Models;

namespace AiTeknikServis.Entities.Dtos.Notification
{
    public class NotificationResponseDto
    {
        /// <summary>
        /// Bildirim ID'si
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Bildirim başlığı
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Bildirim mesajı
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Bildirim türü
        /// </summary>
        public NotificationType Type { get; set; }

        /// <summary>
        /// Bildirim türü adı
        /// </summary>
        public string TypeName { get; set; } = string.Empty;

        /// <summary>
        /// Kullanıcı ID'si
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Servis talebi ID'si (varsa)
        /// </summary>
        public int? ServiceRequestId { get; set; }

        /// <summary>
        /// Alıcı email adresi
        /// </summary>
        public string? RecipientEmail { get; set; }

        /// <summary>
        /// Alıcı telefon numarası
        /// </summary>
        public string? RecipientPhone { get; set; }

        /// <summary>
        /// Okundu mu?
        /// </summary>
        public bool IsRead { get; set; }

        /// <summary>
        /// Email gönderildi mi?
        /// </summary>
        public bool IsEmailSent { get; set; }

        /// <summary>
        /// SMS gönderildi mi?
        /// </summary>
        public bool IsSmsSent { get; set; }

        /// <summary>
        /// Oluşturulma tarihi
        /// </summary>
        public DateTime CreatedDate { get; set; }
    }
}