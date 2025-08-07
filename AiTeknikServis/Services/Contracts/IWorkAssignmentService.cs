using AiTeknikServis.Entities.Dtos.WorkAssignment;
using AiTeknikServis.Entities.Dtos.Dashboard;
using AiTeknikServis.Entities.Models;

namespace AiTeknikServis.Services.Contracts
{
    public interface IWorkAssignmentService
    {
        /// <summary>
        /// Yeni bir iş ataması oluşturur
        /// </summary>
        /// <param name="dto">İş ataması oluşturma DTO'su</param>
        /// <returns>Oluşturulan iş ataması</returns>
        Task<WorkAssignmentResponseDto> CreateAsync(WorkAssignmentCreateDto dto);

        /// <summary>
        /// Otomatik görev ataması yapar (AI destekli)
        /// </summary>
        /// <param name="serviceRequestId">Servis talebi ID'si</param>
        /// <returns>Oluşturulan iş ataması</returns>
        Task<WorkAssignmentResponseDto> AutoAssignAsync(int serviceRequestId);

        /// <summary>
        /// Manuel görev ataması yapar
        /// </summary>
        /// <param name="serviceRequestId">Servis talebi ID'si</param>
        /// <param name="technicianId">Teknisyen ID'si</param>
        /// <param name="scheduledDate">Planlanan tarih</param>
        /// <param name="notes">Notlar</param>
        /// <returns>Oluşturulan iş ataması</returns>
        Task<WorkAssignmentResponseDto> ManualAssignAsync(int serviceRequestId, int technicianId, DateTime? scheduledDate = null, string? notes = null);

        /// <summary>
        /// ID'ye göre iş atamasını getirir
        /// </summary>
        /// <param name="id">İş ataması ID'si</param>
        /// <returns>İş ataması detayları</returns>
        Task<WorkAssignmentResponseDto> GetByIdAsync(int id);

        /// <summary>
        /// Belirli bir teknisyenin iş atamalarını getirir
        /// </summary>
        /// <param name="technicianId">Teknisyen ID'si</param>
        /// <returns>Teknisyenin iş atamaları</returns>
        Task<List<WorkAssignmentResponseDto>> GetByTechnicianAsync(int technicianId);

        /// <summary>
        /// Belirli bir servis talebinin iş atamalarını getirir
        /// </summary>
        /// <param name="serviceRequestId">Servis talebi ID'si</param>
        /// <returns>Servis talebinin iş atamaları</returns>
        Task<List<WorkAssignmentResponseDto>> GetByServiceRequestAsync(int serviceRequestId);

        /// <summary>
        /// Teknisyenin aktif iş atamalarını getirir
        /// </summary>
        /// <param name="technicianId">Teknisyen ID'si</param>
        /// <returns>Aktif iş atamaları</returns>
        Task<List<WorkAssignmentResponseDto>> GetActiveTechnicianAssignmentsAsync(int technicianId);

        /// <summary>
        /// Belirli bir tarihteki iş atamalarını getirir
        /// </summary>
        /// <param name="date">Tarih</param>
        /// <returns>O tarihteki iş atamaları</returns>
        Task<List<WorkAssignmentResponseDto>> GetScheduledAssignmentsAsync(DateTime date);

        /// <summary>
        /// Süresi geçmiş iş atamalarını getirir
        /// </summary>
        /// <returns>Süresi geçmiş iş atamaları</returns>
        Task<List<WorkAssignmentResponseDto>> GetOverdueAssignmentsAsync();

        /// <summary>
        /// İş atamasını başlatır
        /// </summary>
        /// <param name="assignmentId">İş ataması ID'si</param>
        /// <returns>Güncellenmiş iş ataması</returns>
        Task<WorkAssignmentResponseDto> StartAssignmentAsync(int assignmentId);

        /// <summary>
        /// İş atamasını tamamlar
        /// </summary>
        /// <param name="assignmentId">İş ataması ID'si</param>
        /// <param name="completionNotes">Tamamlama notları</param>
        /// <param name="actualHours">Gerçek çalışma saati</param>
        /// <returns>Tamamlanmış iş ataması</returns>
        Task<WorkAssignmentResponseDto> CompleteAssignmentAsync(int assignmentId, string? completionNotes = null, int? actualHours = null);

        /// <summary>
        /// İş atamasını iptal eder
        /// </summary>
        /// <param name="assignmentId">İş ataması ID'si</param>
        /// <param name="reason">İptal nedeni</param>
        /// <returns>İptal edilmiş iş ataması</returns>
        Task<WorkAssignmentResponseDto> CancelAssignmentAsync(int assignmentId, string? reason = null);

        /// <summary>
        /// İş atamasını yeniden atar
        /// </summary>
        /// <param name="assignmentId">Mevcut iş ataması ID'si</param>
        /// <param name="newTechnicianId">Yeni teknisyen ID'si</param>
        /// <param name="reason">Yeniden atama nedeni</param>
        /// <returns>Yeni iş ataması</returns>
        Task<WorkAssignmentResponseDto> ReassignAsync(int assignmentId, int newTechnicianId, string? reason = null);

        /// <summary>
        /// Teknisyenin müsaitlik durumunu kontrol eder
        /// </summary>
        /// <param name="technicianId">Teknisyen ID'si</param>
        /// <param name="scheduledDate">Kontrol edilecek tarih</param>
        /// <returns>Teknisyen müsait mi?</returns>
        Task<bool> IsTechnicianAvailableAsync(int technicianId, DateTime scheduledDate);

        /// <summary>
        /// En uygun teknisyeni bulur
        /// </summary>
        /// <param name="category">Servis kategorisi</param>
        /// <param name="priority">Öncelik</param>
        /// <param name="scheduledDate">Planlanan tarih</param>
        /// <returns>En uygun teknisyen ID'si</returns>
        Task<int?> FindBestTechnicianAsync(ServiceCategory category, Priority priority, DateTime? scheduledDate = null);

        /// <summary>
        /// Teknisyen iş yükü dağılımını getirir
        /// </summary>
        /// <returns>Teknisyen iş yükü haritası</returns>
        Task<Dictionary<int, TechnicianWorkload>> GetTechnicianWorkloadAsync();
        
        /// <summary>
        /// Detaylı teknisyen iş yükü bilgilerini getirir
        /// </summary>
        /// <returns>Teknisyen iş yükü DTO listesi</returns>
        Task<List<TechnicianWorkloadDto>> GetDetailedTechnicianWorkloadAsync();

        /// <summary>
        /// Belirli bir teknisyenin detaylı iş yükü bilgilerini getirir
        /// </summary>
        /// <param name="technicianId">Teknisyen ID'si</param>
        /// <returns>Teknisyen iş yükü detayları</returns>
        Task<TechnicianWorkloadDto?> GetTechnicianWorkloadDetailsAsync(int technicianId);

        /// <summary>
        /// İş ataması performans metriklerini getirir
        /// </summary>
        /// <param name="technicianId">Teknisyen ID'si (opsiyonel)</param>
        /// <param name="startDate">Başlangıç tarihi</param>
        /// <param name="endDate">Bitiş tarihi</param>
        /// <returns>Performans metrikleri</returns>
        Task<WorkAssignmentPerformanceMetrics> GetPerformanceMetricsAsync(int? technicianId = null, DateTime? startDate = null, DateTime? endDate = null);
    }

    /// <summary>
    /// Teknisyen iş yükü bilgilerini içeren model
    /// </summary>
    public class TechnicianWorkload
    {
        /// <summary>
        /// Teknisyen ID'si
        /// </summary>
        public int TechnicianId { get; set; }

        /// <summary>
        /// Teknisyen adı
        /// </summary>
        public string TechnicianName { get; set; } = string.Empty;

        /// <summary>
        /// Aktif görev sayısı
        /// </summary>
        public int ActiveAssignments { get; set; }

        /// <summary>
        /// Maksimum eş zamanlı görev sayısı
        /// </summary>
        public int MaxConcurrentAssignments { get; set; }

        /// <summary>
        /// İş yükü yüzdesi
        /// </summary>
        public double WorkloadPercentage { get; set; }

        /// <summary>
        /// Müsaitlik durumu
        /// </summary>
        public bool IsAvailable { get; set; }

        /// <summary>
        /// Uzmanlık alanları
        /// </summary>
        public List<string> Specializations { get; set; } = new List<string>();

        /// <summary>
        /// Ortalama tamamlama süresi (saat)
        /// </summary>
        public double AverageCompletionTime { get; set; }
    }

    /// <summary>
    /// İş ataması performans metriklerini içeren model
    /// </summary>
    public class WorkAssignmentPerformanceMetrics
    {
        /// <summary>
        /// Toplam atama sayısı
        /// </summary>
        public int TotalAssignments { get; set; }

        /// <summary>
        /// Tamamlanan atama sayısı
        /// </summary>
        public int CompletedAssignments { get; set; }

        /// <summary>
        /// İptal edilen atama sayısı
        /// </summary>
        public int CancelledAssignments { get; set; }

        /// <summary>
        /// Süresi geçmiş atama sayısı
        /// </summary>
        public int OverdueAssignments { get; set; }

        /// <summary>
        /// Ortalama tamamlama süresi (saat)
        /// </summary>
        public double AverageCompletionTime { get; set; }

        /// <summary>
        /// Tamamlama oranı (%)
        /// </summary>
        public double CompletionRate { get; set; }

        /// <summary>
        /// Zamanında tamamlama oranı (%)
        /// </summary>
        public double OnTimeCompletionRate { get; set; }

        /// <summary>
        /// Teknisyen bazında performans
        /// </summary>
        public Dictionary<int, TechnicianPerformance> TechnicianPerformances { get; set; } = new Dictionary<int, TechnicianPerformance>();

        /// <summary>
        /// Metrik hesaplama tarihi
        /// </summary>
        public DateTime CalculatedDate { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Teknisyen performans bilgilerini içeren model
    /// </summary>
    public class TechnicianPerformance
    {
        /// <summary>
        /// Teknisyen ID'si
        /// </summary>
        public int TechnicianId { get; set; }

        /// <summary>
        /// Teknisyen adı
        /// </summary>
        public string TechnicianName { get; set; } = string.Empty;

        /// <summary>
        /// Tamamlanan görev sayısı
        /// </summary>
        public int CompletedTasks { get; set; }

        /// <summary>
        /// Ortalama tamamlama süresi (saat)
        /// </summary>
        public double AverageCompletionTime { get; set; }

        /// <summary>
        /// Zamanında tamamlama oranı (%)
        /// </summary>
        public double OnTimeRate { get; set; }

        /// <summary>
        /// Performans skoru (1-10 arası)
        /// </summary>
        public double PerformanceScore { get; set; }
    }
}