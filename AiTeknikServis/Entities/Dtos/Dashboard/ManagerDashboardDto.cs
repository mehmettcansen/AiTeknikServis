using AiTeknikServis.Entities.Dtos.ServiceRequest;

namespace AiTeknikServis.Entities.Dtos.Dashboard
{
    public class ManagerDashboardDto
    {
        /// <summary>
        /// Toplam talep sayısı
        /// </summary>
        public int TotalRequests { get; set; }

        /// <summary>
        /// Bekleyen talep sayısı
        /// </summary>
        public int PendingRequests { get; set; }

        /// <summary>
        /// İşlemdeki talep sayısı
        /// </summary>
        public int InProgressRequests { get; set; }

        /// <summary>
        /// Tamamlanan talep sayısı
        /// </summary>
        public int CompletedRequests { get; set; }

        /// <summary>
        /// Son talepler
        /// </summary>
        public List<ServiceRequestResponseDto> RecentRequests { get; set; } = new List<ServiceRequestResponseDto>();

        /// <summary>
        /// Acil talepler
        /// </summary>
        public List<ServiceRequestResponseDto> UrgentRequests { get; set; } = new List<ServiceRequestResponseDto>();

        /// <summary>
        /// Yazılım kategorisi talep sayısı
        /// </summary>
        public int SoftwareRequests { get; set; }

        /// <summary>
        /// Donanım kategorisi talep sayısı
        /// </summary>
        public int HardwareRequests { get; set; }

        /// <summary>
        /// Ağ kategorisi talep sayısı
        /// </summary>
        public int NetworkRequests { get; set; }

        /// <summary>
        /// Güvenlik kategorisi talep sayısı
        /// </summary>
        public int SecurityRequests { get; set; }

        /// <summary>
        /// Bakım kategorisi talep sayısı
        /// </summary>
        public int MaintenanceRequests { get; set; }
    }
}