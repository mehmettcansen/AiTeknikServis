using Microsoft.EntityFrameworkCore;
using AiTeknikServis.Data;
using AiTeknikServis.Entities.Models;
using AiTeknikServis.Repositories.Contracts;
using AiTeknikServis.Services.Contracts;

namespace AiTeknikServis.Repositories
{
    /// <summary>
    /// Email doğrulama repository implementasyonu
    /// </summary>
    public class EmailVerificationRepository : IEmailVerificationRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EmailVerificationRepository> _logger;

        public EmailVerificationRepository(ApplicationDbContext context, ILogger<EmailVerificationRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Yeni email doğrulama kaydı oluşturur
        /// </summary>
        public async Task<EmailVerification> CreateAsync(EmailVerification emailVerification)
        {
            try
            {
                _context.EmailVerifications.Add(emailVerification);
                await _context.SaveChangesAsync();
                
                _logger.LogDebug("Email doğrulama kaydı oluşturuldu: ID {Id}, Email: {Email}", 
                    emailVerification.Id, emailVerification.Email);
                
                return emailVerification;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email doğrulama kaydı oluşturulurken hata: {Email}", emailVerification.Email);
                throw;
            }
        }

        /// <summary>
        /// ID'ye göre email doğrulama kaydını getirir
        /// </summary>
        public async Task<EmailVerification?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.EmailVerifications
                    .FirstOrDefaultAsync(ev => ev.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email doğrulama kaydı getirilirken hata: ID {Id}", id);
                return null;
            }
        }

        /// <summary>
        /// Belirli email ve tür için aktif doğrulama kodunu getirir
        /// </summary>
        public async Task<EmailVerification?> GetActiveVerificationAsync(string email, EmailVerificationType type)
        {
            try
            {
                return await _context.EmailVerifications
                    .Where(ev => ev.Email == email.ToLowerInvariant() && 
                                ev.Type == type && 
                                !ev.IsUsed && 
                                ev.ExpiryDate > DateTime.UtcNow &&
                                ev.RetryCount < ev.MaxRetries)
                    .OrderByDescending(ev => ev.CreatedDate)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Aktif doğrulama kodu getirilirken hata: {Email}, Tür: {Type}", email, type);
                return null;
            }
        }

        /// <summary>
        /// Doğrulama kodunu kullanılmış olarak işaretler
        /// </summary>
        public async Task<bool> MarkAsUsedAsync(int id)
        {
            try
            {
                var verification = await _context.EmailVerifications.FindAsync(id);
                if (verification == null)
                    return false;

                verification.IsUsed = true;
                verification.UsedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                
                _logger.LogDebug("Doğrulama kodu kullanılmış olarak işaretlendi: ID {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Doğrulama kodu işaretlenirken hata: ID {Id}", id);
                return false;
            }
        }

        /// <summary>
        /// Deneme sayısını artırır
        /// </summary>
        public async Task<bool> IncrementRetryCountAsync(int id)
        {
            try
            {
                var verification = await _context.EmailVerifications.FindAsync(id);
                if (verification == null)
                    return false;

                verification.RetryCount++;

                await _context.SaveChangesAsync();
                
                _logger.LogDebug("Deneme sayısı artırıldı: ID {Id}, Yeni sayı: {Count}", id, verification.RetryCount);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Deneme sayısı artırılırken hata: ID {Id}", id);
                return false;
            }
        }

        /// <summary>
        /// Belirli email için aktif kodları iptal eder
        /// </summary>
        public async Task<int> CancelActiveCodesAsync(string email, EmailVerificationType? type = null)
        {
            try
            {
                var query = _context.EmailVerifications
                    .Where(ev => ev.Email == email.ToLowerInvariant() && 
                                !ev.IsUsed && 
                                ev.ExpiryDate > DateTime.UtcNow);

                if (type.HasValue)
                {
                    query = query.Where(ev => ev.Type == type.Value);
                }

                var activeCodes = await query.ToListAsync();

                foreach (var code in activeCodes)
                {
                    code.IsUsed = true;
                    code.UsedDate = DateTime.UtcNow;
                }

                if (activeCodes.Any())
                {
                    await _context.SaveChangesAsync();
                    _logger.LogDebug("Aktif kodlar iptal edildi: {Email}, Sayı: {Count}", email, activeCodes.Count);
                }

                return activeCodes.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Aktif kodlar iptal edilirken hata: {Email}", email);
                return 0;
            }
        }

        /// <summary>
        /// Süresi dolmuş kodları siler
        /// </summary>
        public async Task<int> DeleteExpiredCodesAsync()
        {
            try
            {
                var expiredCodes = await _context.EmailVerifications
                    .Where(ev => ev.ExpiryDate <= DateTime.UtcNow)
                    .ToListAsync();

                if (expiredCodes.Any())
                {
                    _context.EmailVerifications.RemoveRange(expiredCodes);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogDebug("Süresi dolmuş kodlar silindi: {Count} adet", expiredCodes.Count);
                }

                return expiredCodes.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Süresi dolmuş kodlar silinirken hata");
                return 0;
            }
        }

        /// <summary>
        /// Belirli email için günlük doğrulama sayısını getirir
        /// </summary>
        public async Task<int> GetDailyVerificationCountAsync(string email, DateTime date)
        {
            try
            {
                var startOfDay = date.Date;
                var endOfDay = startOfDay.AddDays(1);

                return await _context.EmailVerifications
                    .CountAsync(ev => ev.Email == email.ToLowerInvariant() && 
                                     ev.CreatedDate >= startOfDay && 
                                     ev.CreatedDate < endOfDay);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Günlük doğrulama sayısı getirilirken hata: {Email}, Tarih: {Date}", email, date);
                return 0;
            }
        }

        /// <summary>
        /// Email doğrulama geçmişini getirir
        /// </summary>
        public async Task<List<EmailVerification>> GetVerificationHistoryAsync(string email, int limit = 10)
        {
            try
            {
                return await _context.EmailVerifications
                    .Where(ev => ev.Email == email.ToLowerInvariant())
                    .OrderByDescending(ev => ev.CreatedDate)
                    .Take(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Doğrulama geçmişi getirilirken hata: {Email}", email);
                return new List<EmailVerification>();
            }
        }

        /// <summary>
        /// Email doğrulama istatistiklerini getirir
        /// </summary>
        public async Task<EmailVerificationStatistics> GetStatisticsAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var verifications = await _context.EmailVerifications
                    .Where(ev => ev.CreatedDate >= startDate && ev.CreatedDate <= endDate)
                    .ToListAsync();

                var statistics = new EmailVerificationStatistics
                {
                    TotalCodesGenerated = verifications.Count,
                    SuccessfulVerifications = verifications.Count(v => v.IsUsed && v.UsedDate.HasValue),
                    ExpiredCodes = verifications.Count(v => v.IsExpired && !v.IsUsed),
                    GeneratedDate = DateTime.UtcNow
                };

                statistics.FailedVerifications = statistics.TotalCodesGenerated - statistics.SuccessfulVerifications;
                statistics.SuccessRate = statistics.TotalCodesGenerated > 0 
                    ? (double)statistics.SuccessfulVerifications / statistics.TotalCodesGenerated * 100 
                    : 0;

                // Tür dağılımı
                statistics.TypeDistribution = verifications
                    .GroupBy(v => v.Type)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Saatlik dağılım
                statistics.HourlyDistribution = verifications
                    .GroupBy(v => v.CreatedDate.Hour.ToString("D2") + ":00")
                    .ToDictionary(g => g.Key, g => g.Count());

                // Ortalama doğrulama süresi
                var successfulVerifications = verifications
                    .Where(v => v.IsUsed && v.UsedDate.HasValue)
                    .ToList();

                if (successfulVerifications.Any())
                {
                    var totalMinutes = successfulVerifications
                        .Sum(v => (v.UsedDate!.Value - v.CreatedDate).TotalMinutes);
                    statistics.AverageVerificationTime = totalMinutes / successfulVerifications.Count;
                }

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İstatistikler hesaplanırken hata: {StartDate} - {EndDate}", startDate, endDate);
                return new EmailVerificationStatistics();
            }
        }

        /// <summary>
        /// Belirli tarih aralığındaki doğrulama kayıtlarını getirir
        /// </summary>
        public async Task<List<EmailVerification>> GetVerificationsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                return await _context.EmailVerifications
                    .Where(ev => ev.CreatedDate >= startDate && ev.CreatedDate <= endDate)
                    .OrderByDescending(ev => ev.CreatedDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tarih aralığındaki kayıtlar getirilirken hata: {StartDate} - {EndDate}", startDate, endDate);
                return new List<EmailVerification>();
            }
        }

        /// <summary>
        /// Belirli türdeki doğrulama kayıtlarını getirir
        /// </summary>
        public async Task<List<EmailVerification>> GetVerificationsByTypeAsync(EmailVerificationType type, int limit = 100)
        {
            try
            {
                return await _context.EmailVerifications
                    .Where(ev => ev.Type == type)
                    .OrderByDescending(ev => ev.CreatedDate)
                    .Take(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Türe göre kayıtlar getirilirken hata: {Type}", type);
                return new List<EmailVerification>();
            }
        }

        /// <summary>
        /// Başarısız doğrulama kayıtlarını getirir
        /// </summary>
        public async Task<List<EmailVerification>> GetFailedVerificationsAsync(int limit = 100)
        {
            try
            {
                return await _context.EmailVerifications
                    .Where(ev => !ev.IsUsed && (ev.IsExpired || ev.RetryCount >= ev.MaxRetries))
                    .OrderByDescending(ev => ev.CreatedDate)
                    .Take(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Başarısız doğrulamalar getirilirken hata");
                return new List<EmailVerification>();
            }
        }

        /// <summary>
        /// Eski kayıtları temizler
        /// </summary>
        public async Task<int> CleanupOldRecordsAsync(DateTime cutoffDate)
        {
            try
            {
                var oldRecords = await _context.EmailVerifications
                    .Where(ev => ev.CreatedDate < cutoffDate)
                    .ToListAsync();

                if (oldRecords.Any())
                {
                    _context.EmailVerifications.RemoveRange(oldRecords);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogDebug("Eski kayıtlar temizlendi: {Count} adet, Kesim tarihi: {CutoffDate}", 
                        oldRecords.Count, cutoffDate);
                }

                return oldRecords.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Eski kayıtlar temizlenirken hata: {CutoffDate}", cutoffDate);
                return 0;
            }
        }
    }
}