using AiTeknikServis.Entities.Dtos.ServiceRequest;
using AiTeknikServis.Entities.Models;

namespace AiTeknikServis.Services.Contracts
{
    public interface IServiceRequestService
    {
        /// <summary>
        /// Yeni bir servis talebi oluşturur ve AI analizi yapar
        /// </summary>
        /// <param name="dto">Servis talebi oluşturma DTO'su</param>
        /// <returns>Oluşturulan servis talebi</returns>
        Task<ServiceRequestResponseDto> CreateAsync(ServiceRequestCreateDto dto);

        /// <summary>
        /// Mevcut servis talebini günceller
        /// </summary>
        /// <param name="id">Servis talebi ID'si</param>
        /// <param name="dto">Güncelleme DTO'su</param>
        /// <returns>Güncellenmiş servis talebi</returns>
        Task<ServiceRequestResponseDto> UpdateAsync(int id, ServiceRequestUpdateDto dto);

        /// <summary>
        /// ID'ye göre servis talebini getirir
        /// </summary>
        /// <param name="id">Servis talebi ID'si</param>
        /// <returns>Servis talebi detayları</returns>
        Task<ServiceRequestResponseDto> GetByIdAsync(int id);

        /// <summary>
        /// Belirli bir müşteriye ait servis taleplerini getirir
        /// </summary>
        /// <param name="customerId">Müşteri ID'si</param>
        /// <returns>Müşterinin servis talepleri</returns>
        Task<List<ServiceRequestResponseDto>> GetByCustomerAsync(int customerId);

        /// <summary>
        /// Belirli bir teknisyene atanmış servis taleplerini getirir
        /// </summary>
        /// <param name="technicianId">Teknisyen ID'si</param>
        /// <returns>Teknisyenin servis talepleri</returns>
        Task<List<ServiceRequestResponseDto>> GetByTechnicianAsync(int technicianId);

        /// <summary>
        /// Tüm servis taleplerini getirir
        /// </summary>
        /// <returns>Tüm servis talepleri</returns>
        Task<List<ServiceRequestResponseDto>> GetAllAsync();

        /// <summary>
        /// Servis talebini siler
        /// </summary>
        /// <param name="id">Servis talebi ID'si</param>
        /// <returns>Silme işlemi başarılı mı?</returns>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Belirli duruma göre servis taleplerini getirir
        /// </summary>
        /// <param name="status">Servis durumu</param>
        /// <returns>Belirtilen durumdaki servis talepleri</returns>
        Task<List<ServiceRequestResponseDto>> GetByStatusAsync(ServiceStatus status);

        /// <summary>
        /// Bekleyen servis taleplerini getirir
        /// </summary>
        /// <returns>Bekleyen servis talepleri</returns>
        Task<List<ServiceRequestResponseDto>> GetPendingRequestsAsync();

        /// <summary>
        /// Süresi geçmiş servis taleplerini getirir
        /// </summary>
        /// <returns>Süresi geçmiş servis talepleri</returns>
        Task<List<ServiceRequestResponseDto>> GetOverdueRequestsAsync();

        /// <summary>
        /// Henüz teknisyen atanmamış servis taleplerini getirir
        /// </summary>
        /// <returns>Atanmamış servis talepleri</returns>
        Task<List<ServiceRequestResponseDto>> GetUnassignedRequestsAsync();

        /// <summary>
        /// Servis talebine teknisyen atar
        /// </summary>
        /// <param name="serviceRequestId">Servis talebi ID'si</param>
        /// <param name="technicianId">Teknisyen ID'si</param>
        /// <returns>Güncellenmiş servis talebi</returns>
        Task<ServiceRequestResponseDto> AssignTechnicianAsync(int serviceRequestId, int technicianId);

        /// <summary>
        /// Servis talebinin durumunu değiştirir
        /// </summary>
        /// <param name="serviceRequestId">Servis talebi ID'si</param>
        /// <param name="newStatus">Yeni durum</param>
        /// <param name="notes">Durum değişikliği notları</param>
        /// <returns>Güncellenmiş servis talebi</returns>
        Task<ServiceRequestResponseDto> ChangeStatusAsync(int serviceRequestId, ServiceStatus newStatus, string? notes = null);

        /// <summary>
        /// Servis talebini tamamlar
        /// </summary>
        /// <param name="serviceRequestId">Servis talebi ID'si</param>
        /// <param name="resolution">Çözüm açıklaması</param>
        /// <param name="actualCost">Gerçek maliyet</param>
        /// <param name="actualHours">Gerçek çalışma saati</param>
        /// <returns>Tamamlanmış servis talebi</returns>
        Task<ServiceRequestResponseDto> CompleteServiceRequestAsync(int serviceRequestId, string resolution, decimal? actualCost = null, int? actualHours = null);

        /// <summary>
        /// Servis talebinde arama yapar
        /// </summary>
        /// <param name="searchTerm">Arama terimi</param>
        /// <returns>Arama sonuçları</returns>
        Task<List<ServiceRequestResponseDto>> SearchAsync(string searchTerm);

        /// <summary>
        /// Filtrelenmiş servis taleplerini getirir
        /// </summary>
        /// <param name="status">Durum filtresi</param>
        /// <param name="priority">Öncelik filtresi</param>
        /// <param name="category">Kategori filtresi</param>
        /// <param name="customerId">Müşteri filtresi</param>
        /// <param name="technicianId">Teknisyen filtresi</param>
        /// <param name="startDate">Başlangıç tarihi filtresi</param>
        /// <param name="endDate">Bitiş tarihi filtresi</param>
        /// <returns>Filtrelenmiş servis talepleri</returns>
        Task<List<ServiceRequestResponseDto>> GetFilteredAsync(
            ServiceStatus? status = null,
            Priority? priority = null,
            ServiceCategory? category = null,
            int? customerId = null,
            int? technicianId = null,
            DateTime? startDate = null,
            DateTime? endDate = null);

        /// <summary>
        /// Servis talebi istatistiklerini getirir
        /// </summary>
        /// <returns>İstatistik bilgileri</returns>
        Task<ServiceRequestStatistics> GetStatisticsAsync();
    }

    /// <summary>
    /// Servis talebi istatistiklerini içeren model
    /// </summary>
    public class ServiceRequestStatistics
    {
        /// <summary>
        /// Toplam servis talebi sayısı
        /// </summary>
        public int TotalRequests { get; set; }

        /// <summary>
        /// Bekleyen talep sayısı
        /// </summary>
        public int PendingRequests { get; set; }

        /// <summary>
        /// İşlemde olan talep sayısı
        /// </summary>
        public int InProgressRequests { get; set; }

        /// <summary>
        /// Tamamlanan talep sayısı
        /// </summary>
        public int CompletedRequests { get; set; }

        /// <summary>
        /// İptal edilen talep sayısı
        /// </summary>
        public int CancelledRequests { get; set; }

        /// <summary>
        /// Süresi geçmiş talep sayısı
        /// </summary>
        public int OverdueRequests { get; set; }

        /// <summary>
        /// Atanmamış talep sayısı
        /// </summary>
        public int UnassignedRequests { get; set; }

        /// <summary>
        /// Ortalama çözüm süresi (saat)
        /// </summary>
        public double AverageResolutionTime { get; set; }

        /// <summary>
        /// Kategori bazında dağılım
        /// </summary>
        public Dictionary<ServiceCategory, int> CategoryDistribution { get; set; } = new Dictionary<ServiceCategory, int>();

        /// <summary>
        /// Öncelik bazında dağılım
        /// </summary>
        public Dictionary<Priority, int> PriorityDistribution { get; set; } = new Dictionary<Priority, int>();

        /// <summary>
        /// İstatistik tarihi
        /// </summary>
        public DateTime GeneratedDate { get; set; } = DateTime.UtcNow;
    }
}