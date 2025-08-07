using AiTeknikServis.Entities.Dtos.EmailVerification;
using AiTeknikServis.Entities.Models;

namespace AiTeknikServis.Services.Contracts
{
    /// <summary>
    /// Email doğrulama servisi interface'i
    /// </summary>
    public interface IEmailVerificationService
    {
        /// <summary>
        /// Email doğrulama kodu oluşturur ve gönderir
        /// </summary>
        /// <param name="dto">Email doğrulama oluşturma DTO'su</param>
        /// <returns>Doğrulama sonucu</returns>
        Task<EmailVerificationResultDto> CreateVerificationCodeAsync(EmailVerificationCreateDto dto);
        
        /// <summary>
        /// Email doğrulama kodunu doğrular
        /// </summary>
        /// <param name="dto">Email doğrulama DTO'su</param>
        /// <returns>Doğrulama sonucu</returns>
        Task<EmailVerificationResultDto> VerifyCodeAsync(EmailVerificationVerifyDto dto);
        
        /// <summary>
        /// Belirli email ve tür için aktif doğrulama kodunu getirir
        /// </summary>
        /// <param name="email">Email adresi</param>
        /// <param name="type">Doğrulama türü</param>
        /// <returns>Doğrulama kodu bilgisi</returns>
        Task<EmailVerificationResponseDto?> GetActiveVerificationAsync(string email, EmailVerificationType type);
        
        /// <summary>
        /// Doğrulama kodunu yeniden gönderir
        /// </summary>
        /// <param name="email">Email adresi</param>
        /// <param name="type">Doğrulama türü</param>
        /// <returns>Yeniden gönderim sonucu</returns>
        Task<EmailVerificationResultDto> ResendVerificationCodeAsync(string email, EmailVerificationType type);
        
        /// <summary>
        /// Belirli email için tüm aktif doğrulama kodlarını iptal eder
        /// </summary>
        /// <param name="email">Email adresi</param>
        /// <param name="type">Doğrulama türü (opsiyonel, belirtilmezse tüm türler)</param>
        /// <returns>İptal edilen kod sayısı</returns>
        Task<int> CancelVerificationCodesAsync(string email, EmailVerificationType? type = null);
        
        /// <summary>
        /// Süresi dolmuş doğrulama kodlarını temizler
        /// </summary>
        /// <returns>Temizlenen kod sayısı</returns>
        Task<int> CleanupExpiredCodesAsync();
        
        /// <summary>
        /// Email doğrulama istatistiklerini getirir
        /// </summary>
        /// <param name="startDate">Başlangıç tarihi</param>
        /// <param name="endDate">Bitiş tarihi</param>
        /// <returns>İstatistikler</returns>
        Task<EmailVerificationStatistics> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);
        
        /// <summary>
        /// Belirli email adresinin doğrulama geçmişini getirir
        /// </summary>
        /// <param name="email">Email adresi</param>
        /// <param name="limit">Maksimum kayıt sayısı</param>
        /// <returns>Doğrulama geçmişi</returns>
        Task<List<EmailVerificationResponseDto>> GetVerificationHistoryAsync(string email, int limit = 10);
        
        /// <summary>
        /// Doğrulama kodu formatını kontrol eder
        /// </summary>
        /// <param name="code">Doğrulama kodu</param>
        /// <returns>Kod geçerli mi?</returns>
        bool IsValidCodeFormat(string code);
        
        /// <summary>
        /// Email adresinin doğrulama için uygun olup olmadığını kontrol eder
        /// </summary>
        /// <param name="email">Email adresi</param>
        /// <returns>Email uygun mu?</returns>
        Task<bool> IsEmailEligibleForVerificationAsync(string email);
        
        /// <summary>
        /// Belirli IP adresinden gelen doğrulama isteklerini sınırlar (rate limiting)
        /// </summary>
        /// <param name="ipAddress">IP adresi</param>
        /// <param name="email">Email adresi</param>
        /// <returns>İstek yapılabilir mi?</returns>
        Task<bool> CheckRateLimitAsync(string ipAddress, string email);
    }
    
    /// <summary>
    /// Email doğrulama istatistikleri
    /// </summary>
    public class EmailVerificationStatistics
    {
        public int TotalCodesGenerated { get; set; }
        public int SuccessfulVerifications { get; set; }
        public int FailedVerifications { get; set; }
        public int ExpiredCodes { get; set; }
        public double SuccessRate { get; set; }
        public double AverageVerificationTime { get; set; } // Dakika cinsinden
        public Dictionary<EmailVerificationType, int> TypeDistribution { get; set; } = new();
        public Dictionary<string, int> HourlyDistribution { get; set; } = new();
        public DateTime GeneratedDate { get; set; } = DateTime.UtcNow;
    }
}