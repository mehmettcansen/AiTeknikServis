using AiTeknikServis.Entities.Dtos.Notification;
using AiTeknikServis.Entities.Models;

namespace AiTeknikServis.Services.Contracts
{
    public interface INotificationService
    {
        /// <summary>
        /// Yeni bir bildirim oluşturur
        /// </summary>
        /// <param name="dto">Bildirim oluşturma DTO'su</param>
        /// <returns>Oluşturulan bildirim</returns>
        Task<NotificationResponseDto> CreateAsync(NotificationCreateDto dto);

        /// <summary>
        /// ID'ye göre bildirimi getirir
        /// </summary>
        /// <param name="id">Bildirim ID'si</param>
        /// <returns>Bildirim detayları</returns>
        Task<NotificationResponseDto> GetByIdAsync(int id);

        /// <summary>
        /// Belirli bir kullanıcının bildirimlerini getirir
        /// </summary>
        /// <param name="userId">Kullanıcı ID'si</param>
        /// <returns>Kullanıcının bildirimleri</returns>
        Task<List<NotificationResponseDto>> GetByUserAsync(int userId);

        /// <summary>
        /// Kullanıcının okunmamış bildirimlerini getirir
        /// </summary>
        /// <param name="userId">Kullanıcı ID'si</param>
        /// <returns>Okunmamış bildirimler</returns>
        Task<List<NotificationResponseDto>> GetUnreadNotificationsAsync(int userId);

        /// <summary>
        /// Kullanıcının son bildirimlerini getirir
        /// </summary>
        /// <param name="userId">Kullanıcı ID'si</param>
        /// <param name="count">Getirilecek bildirim sayısı</param>
        /// <returns>Son bildirimler</returns>
        Task<List<NotificationResponseDto>> GetRecentNotificationsAsync(int userId, int count = 10);

        /// <summary>
        /// Bildirimi okundu olarak işaretler
        /// </summary>
        /// <param name="notificationId">Bildirim ID'si</param>
        /// <returns>İşlem başarılı mı?</returns>
        Task<bool> MarkAsReadAsync(int notificationId);

        /// <summary>
        /// Kullanıcının tüm bildirimlerini okundu olarak işaretler
        /// </summary>
        /// <param name="userId">Kullanıcı ID'si</param>
        /// <returns>İşlem başarılı mı?</returns>
        Task<bool> MarkAllAsReadAsync(int userId);

        /// <summary>
        /// Kullanıcının okunmamış bildirim sayısını getirir
        /// </summary>
        /// <param name="userId">Kullanıcı ID'si</param>
        /// <returns>Okunmamış bildirim sayısı</returns>
        Task<int> GetUnreadCountAsync(int userId);

        /// <summary>
        /// Servis talebi oluşturulduğunda bildirim gönderir
        /// </summary>
        /// <param name="serviceRequestId">Servis talebi ID'si</param>
        /// <returns>Task</returns>
        Task SendServiceRequestCreatedNotificationAsync(int serviceRequestId);

        /// <summary>
        /// Teknisyen atandığında bildirim gönderir
        /// </summary>
        /// <param name="serviceRequestId">Servis talebi ID'si</param>
        /// <param name="technicianId">Teknisyen ID'si</param>
        /// <returns>Task</returns>
        Task SendTechnicianAssignedNotificationAsync(int serviceRequestId, int technicianId);

        /// <summary>
        /// Servis talebi durumu değiştiğinde bildirim gönderir
        /// </summary>
        /// <param name="serviceRequestId">Servis talebi ID'si</param>
        /// <param name="newStatus">Yeni durum</param>
        /// <returns>Task</returns>
        Task SendStatusChangeNotificationAsync(int serviceRequestId, ServiceStatus newStatus);

        /// <summary>
        /// Servis tamamlandığında bildirim gönderir
        /// </summary>
        /// <param name="serviceRequestId">Servis talebi ID'si</param>
        /// <returns>Task</returns>
        Task SendServiceCompletedNotificationAsync(int serviceRequestId);

        /// <summary>
        /// Acil öncelikli talep için yöneticilere bildirim gönderir
        /// </summary>
        /// <param name="serviceRequestId">Servis talebi ID'si</param>
        /// <returns>Task</returns>
        Task SendUrgentRequestNotificationAsync(int serviceRequestId);

        /// <summary>
        /// Bekleyen email bildirimlerini işler
        /// </summary>
        /// <returns>İşlenen bildirim sayısı</returns>
        Task<int> ProcessPendingEmailNotificationsAsync();

        /// <summary>
        /// Eski bildirimleri temizler
        /// </summary>
        /// <param name="cutoffDate">Kesim tarihi</param>
        /// <returns>Silinen bildirim sayısı</returns>
        Task<int> CleanupOldNotificationsAsync(DateTime cutoffDate);

        /// <summary>
        /// Bildirim istatistiklerini getirir
        /// </summary>
        /// <param name="startDate">Başlangıç tarihi</param>
        /// <param name="endDate">Bitiş tarihi</param>
        /// <returns>Bildirim istatistikleri</returns>
        Task<NotificationStatistics> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Toplu bildirim gönderir
        /// </summary>
        /// <param name="userIds">Kullanıcı ID'leri</param>
        /// <param name="title">Bildirim başlığı</param>
        /// <param name="message">Bildirim mesajı</param>
        /// <param name="type">Bildirim türü</param>
        /// <returns>Gönderilen bildirim sayısı</returns>
        Task<int> SendBulkNotificationAsync(List<int> userIds, string title, string message, NotificationType type);
    }

    /// <summary>
    /// Bildirim istatistiklerini içeren model
    /// </summary>
    public class NotificationStatistics
    {
        /// <summary>
        /// Toplam bildirim sayısı
        /// </summary>
        public int TotalNotifications { get; set; }

        /// <summary>
        /// Gönderilen email sayısı
        /// </summary>
        public int EmailsSent { get; set; }

        /// <summary>
        /// Başarısız email sayısı
        /// </summary>
        public int FailedEmails { get; set; }

        /// <summary>
        /// Okunmamış bildirim sayısı
        /// </summary>
        public int UnreadNotifications { get; set; }

        /// <summary>
        /// Bildirim türü bazında dağılım
        /// </summary>
        public Dictionary<NotificationType, int> TypeDistribution { get; set; } = new Dictionary<NotificationType, int>();

        /// <summary>
        /// Email başarı oranı (%)
        /// </summary>
        public double EmailSuccessRate { get; set; }

        /// <summary>
        /// Ortalama okunma süresi (saat)
        /// </summary>
        public double AverageReadTime { get; set; }

        /// <summary>
        /// İstatistik hesaplama tarihi
        /// </summary>
        public DateTime GeneratedDate { get; set; } = DateTime.UtcNow;
    }
}