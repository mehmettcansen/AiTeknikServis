using AiTeknikServis.Entities.Models;
using AiTeknikServis.Services.Contracts;

namespace AiTeknikServis.Repositories.Contracts
{
    /// <summary>
    /// Email doğrulama repository interface'i
    /// </summary>
    public interface IEmailVerificationRepository
    {
        /// <summary>
        /// Yeni email doğrulama kaydı oluşturur
        /// </summary>
        /// <param name="emailVerification">Email doğrulama entity'si</param>
        /// <returns>Oluşturulan kayıt</returns>
        Task<EmailVerification> CreateAsync(EmailVerification emailVerification);
        
        /// <summary>
        /// ID'ye göre email doğrulama kaydını getirir
        /// </summary>
        /// <param name="id">Kayıt ID'si</param>
        /// <returns>Email doğrulama kaydı</returns>
        Task<EmailVerification?> GetByIdAsync(int id);
        
        /// <summary>
        /// Belirli email ve tür için aktif doğrulama kodunu getirir
        /// </summary>
        /// <param name="email">Email adresi</param>
        /// <param name="type">Doğrulama türü</param>
        /// <returns>Aktif doğrulama kodu</returns>
        Task<EmailVerification?> GetActiveVerificationAsync(string email, EmailVerificationType type);
        
        /// <summary>
        /// Doğrulama kodunu kullanılmış olarak işaretler
        /// </summary>
        /// <param name="id">Doğrulama ID'si</param>
        /// <returns>İşlem başarılı mı?</returns>
        Task<bool> MarkAsUsedAsync(int id);
        
        /// <summary>
        /// Deneme sayısını artırır
        /// </summary>
        /// <param name="id">Doğrulama ID'si</param>
        /// <returns>İşlem başarılı mı?</returns>
        Task<bool> IncrementRetryCountAsync(int id);
        
        /// <summary>
        /// Belirli email için aktif kodları iptal eder
        /// </summary>
        /// <param name="email">Email adresi</param>
        /// <param name="type">Doğrulama türü (opsiyonel)</param>
        /// <returns>İptal edilen kod sayısı</returns>
        Task<int> CancelActiveCodesAsync(string email, EmailVerificationType? type = null);
        
        /// <summary>
        /// Süresi dolmuş kodları siler
        /// </summary>
        /// <returns>Silinen kod sayısı</returns>
        Task<int> DeleteExpiredCodesAsync();
        
        /// <summary>
        /// Belirli email için günlük doğrulama sayısını getirir
        /// </summary>
        /// <param name="email">Email adresi</param>
        /// <param name="date">Tarih</param>
        /// <returns>Günlük doğrulama sayısı</returns>
        Task<int> GetDailyVerificationCountAsync(string email, DateTime date);
        
        /// <summary>
        /// Email doğrulama geçmişini getirir
        /// </summary>
        /// <param name="email">Email adresi</param>
        /// <param name="limit">Maksimum kayıt sayısı</param>
        /// <returns>Doğrulama geçmişi</returns>
        Task<List<EmailVerification>> GetVerificationHistoryAsync(string email, int limit = 10);
        
        /// <summary>
        /// Email doğrulama istatistiklerini getirir
        /// </summary>
        /// <param name="startDate">Başlangıç tarihi</param>
        /// <param name="endDate">Bitiş tarihi</param>
        /// <returns>İstatistikler</returns>
        Task<EmailVerificationStatistics> GetStatisticsAsync(DateTime startDate, DateTime endDate);
        
        /// <summary>
        /// Belirli tarih aralığındaki doğrulama kayıtlarını getirir
        /// </summary>
        /// <param name="startDate">Başlangıç tarihi</param>
        /// <param name="endDate">Bitiş tarihi</param>
        /// <returns>Doğrulama kayıtları</returns>
        Task<List<EmailVerification>> GetVerificationsByDateRangeAsync(DateTime startDate, DateTime endDate);
        
        /// <summary>
        /// Belirli türdeki doğrulama kayıtlarını getirir
        /// </summary>
        /// <param name="type">Doğrulama türü</param>
        /// <param name="limit">Maksimum kayıt sayısı</param>
        /// <returns>Doğrulama kayıtları</returns>
        Task<List<EmailVerification>> GetVerificationsByTypeAsync(EmailVerificationType type, int limit = 100);
        
        /// <summary>
        /// Başarısız doğrulama kayıtlarını getirir
        /// </summary>
        /// <param name="limit">Maksimum kayıt sayısı</param>
        /// <returns>Başarısız doğrulama kayıtları</returns>
        Task<List<EmailVerification>> GetFailedVerificationsAsync(int limit = 100);
        
        /// <summary>
        /// Eski kayıtları temizler
        /// </summary>
        /// <param name="cutoffDate">Kesim tarihi</param>
        /// <returns>Silinen kayıt sayısı</returns>
        Task<int> CleanupOldRecordsAsync(DateTime cutoffDate);
    }
}