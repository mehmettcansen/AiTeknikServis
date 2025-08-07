using AiTeknikServis.Entities.Dtos.ServiceRequest;

namespace AiTeknikServis.Entities.Dtos.Dashboard
{
    public class TechnicianDashboardDto
    {
        /// <summary>
        /// Aktif servis talepleri
        /// </summary>
        public List<ServiceRequestResponseDto> ActiveServiceRequests { get; set; } = new List<ServiceRequestResponseDto>();

        /// <summary>
        /// Toplam atama sayısı
        /// </summary>
        public int TotalAssignments { get; set; }

        /// <summary>
        /// Bekleyen atama sayısı
        /// </summary>
        public int PendingAssignments { get; set; }

        /// <summary>
        /// Devam eden atama sayısı
        /// </summary>
        public int InProgressAssignments { get; set; }

        /// <summary>
        /// Bugün tamamlanan atama sayısı
        /// </summary>
        public int CompletedToday { get; set; }
    }
}