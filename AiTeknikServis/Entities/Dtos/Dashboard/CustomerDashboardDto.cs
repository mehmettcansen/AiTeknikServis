using AiTeknikServis.Entities.Dtos.ServiceRequest;
using AiTeknikServis.Entities.Dtos.Notification;

namespace AiTeknikServis.Entities.Dtos.Dashboard
{
    public class CustomerDashboardDto
    {
        /// <summary>
        /// Müşterinin servis talepleri
        /// </summary>
        public List<ServiceRequestResponseDto> MyRequests { get; set; } = new List<ServiceRequestResponseDto>();

        /// <summary>
        /// Son bildirimler
        /// </summary>
        public List<NotificationResponseDto> RecentNotifications { get; set; } = new List<NotificationResponseDto>();

        /// <summary>
        /// Toplam talep sayısı
        /// </summary>
        public int TotalRequests { get; set; }

        /// <summary>
        /// Bekleyen talep sayısı
        /// </summary>
        public int PendingRequests { get; set; }

        /// <summary>
        /// Aktif talep sayısı
        /// </summary>
        public int ActiveRequests { get; set; }

        /// <summary>
        /// Tamamlanan talep sayısı
        /// </summary>
        public int CompletedRequests { get; set; }
    }
}