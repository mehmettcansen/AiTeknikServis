using System.Security.Cryptography;
using System.Text.Json;
using AutoMapper;
using AiTeknikServis.Entities.Dtos.EmailVerification;
using AiTeknikServis.Entities.Models;
using AiTeknikServis.Repositories.Contracts;
using AiTeknikServis.Services.Contracts;

namespace AiTeknikServis.Services
{
    /// <summary>
    /// Email doğrulama servisi implementasyonu
    /// </summary>
    public class EmailVerificationService : IEmailVerificationService
    {
        private readonly IEmailVerificationRepository _emailVerificationRepository;
        private readonly IEmailService _emailService;
        private readonly IMapper _mapper;
        private readonly ILogger<EmailVerificationService> _logger;
        private readonly IConfiguration _configuration;

        // Rate limiting için basit in-memory cache
        private static readonly Dictionary<string, List<DateTime>> _rateLimitCache = new();
        private static readonly object _rateLimitLock = new();

        public EmailVerificationService(
            IEmailVerificationRepository emailVerificationRepository,
            IEmailService emailService,
            IMapper mapper,
            ILogger<EmailVerificationService> logger,
            IConfiguration configuration)
        {
            _emailVerificationRepository = emailVerificationRepository;
            _emailService = emailService;
            _mapper = mapper;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Email doğrulama kodu oluşturur ve gönderir
        /// </summary>
        public async Task<EmailVerificationResultDto> CreateVerificationCodeAsync(EmailVerificationCreateDto dto)
        {
            try
            {
                _logger.LogInformation("Email doğrulama kodu oluşturuluyor: {Email}, Tür: {Type}", dto.Email, dto.Type);

                // Email uygunluk kontrolü
                if (!await IsEmailEligibleForVerificationAsync(dto.Email))
                {
                    return new EmailVerificationResultDto
                    {
                        IsSuccess = false,
                        Message = "Bu email adresi için doğrulama kodu oluşturulamaz",
                        ErrorCode = "EMAIL_NOT_ELIGIBLE",
                        CanRetry = false
                    };
                }

                // Mevcut aktif kodları iptal et
                await CancelVerificationCodesAsync(dto.Email, dto.Type);

                // Yeni doğrulama kodu oluştur
                var verificationCode = GenerateVerificationCode();
                var expiryDate = DateTime.UtcNow.AddMinutes(dto.ExpiryMinutes);

                var emailVerification = new EmailVerification
                {
                    Email = dto.Email.ToLowerInvariant(),
                    VerificationCode = verificationCode,
                    Type = dto.Type,
                    CreatedDate = DateTime.UtcNow,
                    ExpiryDate = expiryDate,
                    Purpose = dto.Purpose,
                    AdditionalData = dto.AdditionalData,
                    MaxRetries = dto.MaxRetries
                };

                var createdVerification = await _emailVerificationRepository.CreateAsync(emailVerification);

                // Email gönder
                _logger.LogInformation("Email gönderimi başlatılıyor: {Email}, Kod: {Code}", dto.Email, verificationCode);
                var emailSent = await SendVerificationEmailAsync(createdVerification);

                if (!emailSent)
                {
                    _logger.LogWarning("Doğrulama kodu email'i gönderilemedi: {Email}", dto.Email);
                    return new EmailVerificationResultDto
                    {
                        IsSuccess = false,
                        Message = "Doğrulama kodu email'i gönderilemedi. Lütfen tekrar deneyin.",
                        ErrorCode = "EMAIL_SEND_FAILED",
                        CanRetry = true
                    };
                }

                _logger.LogInformation("Email doğrulama kodu başarıyla oluşturuldu ve gönderildi: {Email}", dto.Email);

                return new EmailVerificationResultDto
                {
                    IsSuccess = true,
                    Message = "Doğrulama kodu email adresinize gönderildi",
                    ExpiryDate = expiryDate,
                    CanRetry = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email doğrulama kodu oluşturulurken hata: {Email}", dto.Email);
                return new EmailVerificationResultDto
                {
                    IsSuccess = false,
                    Message = "Doğrulama kodu oluşturulurken bir hata oluştu",
                    ErrorCode = "INTERNAL_ERROR",
                    CanRetry = true
                };
            }
        }

        /// <summary>
        /// Email doğrulama kodunu doğrular
        /// </summary>
        public async Task<EmailVerificationResultDto> VerifyCodeAsync(EmailVerificationVerifyDto dto)
        {
            try
            {
                _logger.LogInformation("Email doğrulama kodu doğrulanıyor: {Email}, Tür: {Type}", dto.Email, dto.Type);

                var verification = await _emailVerificationRepository.GetActiveVerificationAsync(
                    dto.Email.ToLowerInvariant(), dto.Type);

                if (verification == null)
                {
                    _logger.LogWarning("Aktif doğrulama kodu bulunamadı: {Email}, Tür: {Type}", dto.Email, dto.Type);
                    return new EmailVerificationResultDto
                    {
                        IsSuccess = false,
                        Message = "Geçerli bir doğrulama kodu bulunamadı",
                        ErrorCode = "CODE_NOT_FOUND",
                        CanRetry = false
                    };
                }

                // Kod süresi dolmuş mu?
                if (verification.IsExpired)
                {
                    _logger.LogWarning("Doğrulama kodunun süresi dolmuş: {Email}", dto.Email);
                    return new EmailVerificationResultDto
                    {
                        IsSuccess = false,
                        Message = "Doğrulama kodunun süresi dolmuş. Yeni kod talep edin.",
                        ErrorCode = "CODE_EXPIRED",
                        CanRetry = true
                    };
                }

                // Maksimum deneme sayısı aşılmış mı?
                if (verification.RetryCount >= verification.MaxRetries)
                {
                    _logger.LogWarning("Maksimum deneme sayısı aşıldı: {Email}", dto.Email);
                    await _emailVerificationRepository.MarkAsUsedAsync(verification.Id);
                    return new EmailVerificationResultDto
                    {
                        IsSuccess = false,
                        Message = "Maksimum deneme sayısı aşıldı. Yeni kod talep edin.",
                        ErrorCode = "MAX_RETRIES_EXCEEDED",
                        CanRetry = true
                    };
                }

                // Kod doğru mu?
                if (verification.VerificationCode != dto.VerificationCode)
                {
                    // Deneme sayısını artır
                    await _emailVerificationRepository.IncrementRetryCountAsync(verification.Id);
                    
                    var remainingAttempts = verification.MaxRetries - verification.RetryCount - 1;
                    
                    _logger.LogWarning("Yanlış doğrulama kodu: {Email}, Kalan deneme: {Remaining}", 
                        dto.Email, remainingAttempts);

                    return new EmailVerificationResultDto
                    {
                        IsSuccess = false,
                        Message = $"Doğrulama kodu yanlış. Kalan deneme hakkı: {remainingAttempts}",
                        ErrorCode = "INVALID_CODE",
                        RemainingAttempts = remainingAttempts,
                        CanRetry = remainingAttempts > 0
                    };
                }

                // Doğrulama başarılı - kodu kullanılmış olarak işaretle
                await _emailVerificationRepository.MarkAsUsedAsync(verification.Id);

                _logger.LogInformation("Email doğrulama başarılı: {Email}, Tür: {Type}", dto.Email, dto.Type);

                return new EmailVerificationResultDto
                {
                    IsSuccess = true,
                    Message = "Email doğrulama başarılı",
                    CanRetry = false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email doğrulama kodunu doğrularken hata: {Email}", dto.Email);
                return new EmailVerificationResultDto
                {
                    IsSuccess = false,
                    Message = "Doğrulama sırasında bir hata oluştu",
                    ErrorCode = "INTERNAL_ERROR",
                    CanRetry = true
                };
            }
        }

        /// <summary>
        /// Belirli email ve tür için aktif doğrulama kodunu getirir
        /// </summary>
        public async Task<EmailVerificationResponseDto?> GetActiveVerificationAsync(string email, EmailVerificationType type)
        {
            try
            {
                var verification = await _emailVerificationRepository.GetActiveVerificationAsync(
                    email.ToLowerInvariant(), type);

                if (verification == null)
                    return null;

                var dto = _mapper.Map<EmailVerificationResponseDto>(verification);
                dto.RemainingMinutes = Math.Max(0, (int)(verification.ExpiryDate - DateTime.UtcNow).TotalMinutes);
                dto.TypeDisplayName = GetTypeDisplayName(verification.Type);

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Aktif doğrulama kodu getirilirken hata: {Email}, Tür: {Type}", email, type);
                return null;
            }
        }

        /// <summary>
        /// Doğrulama kodunu yeniden gönderir
        /// </summary>
        public async Task<EmailVerificationResultDto> ResendVerificationCodeAsync(string email, EmailVerificationType type)
        {
            try
            {
                _logger.LogInformation("Doğrulama kodu yeniden gönderiliyor: {Email}, Tür: {Type}", email, type);

                var activeVerification = await _emailVerificationRepository.GetActiveVerificationAsync(
                    email.ToLowerInvariant(), type);

                if (activeVerification == null)
                {
                    return new EmailVerificationResultDto
                    {
                        IsSuccess = false,
                        Message = "Yeniden gönderilebilecek aktif doğrulama kodu bulunamadı",
                        ErrorCode = "NO_ACTIVE_CODE",
                        CanRetry = false
                    };
                }

                // Son gönderimden en az 1 dakika geçmiş olmalı (spam önleme)
                var timeSinceCreation = DateTime.UtcNow - activeVerification.CreatedDate;
                if (timeSinceCreation.TotalMinutes < 1)
                {
                    var waitTime = 60 - (int)timeSinceCreation.TotalSeconds;
                    return new EmailVerificationResultDto
                    {
                        IsSuccess = false,
                        Message = $"Yeniden gönderim için {waitTime} saniye bekleyin",
                        ErrorCode = "RATE_LIMITED",
                        CanRetry = true
                    };
                }

                // Email'i yeniden gönder
                var emailSent = await SendVerificationEmailAsync(activeVerification);

                if (!emailSent)
                {
                    return new EmailVerificationResultDto
                    {
                        IsSuccess = false,
                        Message = "Email gönderilemedi. Lütfen tekrar deneyin.",
                        ErrorCode = "EMAIL_SEND_FAILED",
                        CanRetry = true
                    };
                }

                _logger.LogInformation("Doğrulama kodu yeniden gönderildi: {Email}", email);

                return new EmailVerificationResultDto
                {
                    IsSuccess = true,
                    Message = "Doğrulama kodu yeniden gönderildi",
                    ExpiryDate = activeVerification.ExpiryDate,
                    CanRetry = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Doğrulama kodu yeniden gönderilirken hata: {Email}", email);
                return new EmailVerificationResultDto
                {
                    IsSuccess = false,
                    Message = "Yeniden gönderim sırasında bir hata oluştu",
                    ErrorCode = "INTERNAL_ERROR",
                    CanRetry = true
                };
            }
        }

        /// <summary>
        /// Belirli email için tüm aktif doğrulama kodlarını iptal eder
        /// </summary>
        public async Task<int> CancelVerificationCodesAsync(string email, EmailVerificationType? type = null)
        {
            try
            {
                var cancelledCount = await _emailVerificationRepository.CancelActiveCodesAsync(
                    email.ToLowerInvariant(), type);

                if (cancelledCount > 0)
                {
                    _logger.LogInformation("Doğrulama kodları iptal edildi: {Email}, Sayı: {Count}, Tür: {Type}", 
                        email, cancelledCount, type?.ToString() ?? "Tümü");
                }

                return cancelledCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Doğrulama kodları iptal edilirken hata: {Email}", email);
                return 0;
            }
        }

        /// <summary>
        /// Süresi dolmuş doğrulama kodlarını temizler
        /// </summary>
        public async Task<int> CleanupExpiredCodesAsync()
        {
            try
            {
                var cleanedCount = await _emailVerificationRepository.DeleteExpiredCodesAsync();

                if (cleanedCount > 0)
                {
                    _logger.LogInformation("Süresi dolmuş doğrulama kodları temizlendi: {Count} adet", cleanedCount);
                }

                return cleanedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Süresi dolmuş kodlar temizlenirken hata");
                return 0;
            }
        }

        /// <summary>
        /// Email doğrulama istatistiklerini getirir
        /// </summary>
        public async Task<EmailVerificationStatistics> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var statistics = await _emailVerificationRepository.GetStatisticsAsync(
                    startDate ?? DateTime.UtcNow.AddDays(-30),
                    endDate ?? DateTime.UtcNow);

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email doğrulama istatistikleri getirilirken hata");
                return new EmailVerificationStatistics();
            }
        }

        /// <summary>
        /// Belirli email adresinin doğrulama geçmişini getirir
        /// </summary>
        public async Task<List<EmailVerificationResponseDto>> GetVerificationHistoryAsync(string email, int limit = 10)
        {
            try
            {
                var verifications = await _emailVerificationRepository.GetVerificationHistoryAsync(
                    email.ToLowerInvariant(), limit);

                var dtos = _mapper.Map<List<EmailVerificationResponseDto>>(verifications);
                
                foreach (var dto in dtos)
                {
                    dto.TypeDisplayName = GetTypeDisplayName((EmailVerificationType)dto.Type);
                    dto.RemainingMinutes = Math.Max(0, (int)(dto.ExpiryDate - DateTime.UtcNow).TotalMinutes);
                }

                return dtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Doğrulama geçmişi getirilirken hata: {Email}", email);
                return new List<EmailVerificationResponseDto>();
            }
        }

        /// <summary>
        /// Doğrulama kodu formatını kontrol eder
        /// </summary>
        public bool IsValidCodeFormat(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            return code.Length == 6 && code.All(char.IsDigit);
        }

        /// <summary>
        /// Email adresinin doğrulama için uygun olup olmadığını kontrol eder
        /// </summary>
        public async Task<bool> IsEmailEligibleForVerificationAsync(string email)
        {
            try
            {
                // Email format kontrolü
                if (!_emailService.IsValidEmailAddress(email))
                    return false;

                // Blacklist kontrolü
                if (await _emailService.IsEmailBlacklistedAsync(email))
                    return false;

                // Günlük limit kontrolü (örneğin günde en fazla 10 doğrulama kodu)
                var dailyCount = await _emailVerificationRepository.GetDailyVerificationCountAsync(
                    email.ToLowerInvariant(), DateTime.UtcNow.Date);

                var dailyLimit = _configuration.GetValue<int>("EmailVerification:DailyLimit", 10);
                
                return dailyCount < dailyLimit;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email uygunluk kontrolü yapılırken hata: {Email}", email);
                return false;
            }
        }

        /// <summary>
        /// Belirli IP adresinden gelen doğrulama isteklerini sınırlar (rate limiting)
        /// </summary>
        public async Task<bool> CheckRateLimitAsync(string ipAddress, string email)
        {
            try
            {
                var key = $"{ipAddress}:{email}";
                var now = DateTime.UtcNow;
                var windowMinutes = _configuration.GetValue<int>("EmailVerification:RateLimitWindowMinutes", 60);
                var maxRequests = _configuration.GetValue<int>("EmailVerification:MaxRequestsPerWindow", 5);

                lock (_rateLimitLock)
                {
                    if (!_rateLimitCache.ContainsKey(key))
                    {
                        _rateLimitCache[key] = new List<DateTime>();
                    }

                    var requests = _rateLimitCache[key];
                    
                    // Eski istekleri temizle
                    requests.RemoveAll(r => (now - r).TotalMinutes > windowMinutes);

                    // Limit kontrolü
                    if (requests.Count >= maxRequests)
                    {
                        _logger.LogWarning("Rate limit aşıldı: {IP}, {Email}", ipAddress, email);
                        return false;
                    }

                    // Yeni isteği ekle
                    requests.Add(now);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rate limit kontrolü yapılırken hata: {IP}, {Email}", ipAddress, email);
                return true; // Hata durumunda izin ver
            }
        }

        #region Private Methods

        /// <summary>
        /// 6 haneli rastgele doğrulama kodu oluşturur
        /// </summary>
        private string GenerateVerificationCode()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            var randomNumber = Math.Abs(BitConverter.ToInt32(bytes, 0));
            return (randomNumber % 1000000).ToString("D6");
        }

        /// <summary>
        /// Doğrulama email'ini gönderir
        /// </summary>
        private async Task<bool> SendVerificationEmailAsync(EmailVerification verification)
        {
            try
            {
                var subject = GetEmailSubject(verification.Type);
                var body = GetEmailBody(verification);

                _logger.LogInformation("Email gönderiliyor - To: {Email}, Subject: {Subject}", verification.Email, subject);
                await _emailService.SendHtmlEmailAsync(verification.Email, subject, body);
                _logger.LogInformation("Email başarıyla gönderildi: {Email}", verification.Email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Doğrulama email'i gönderilirken hata: {Email}", verification.Email);
                return false;
            }
        }

        /// <summary>
        /// Email konusunu döndürür
        /// </summary>
        private string GetEmailSubject(EmailVerificationType type)
        {
            return type switch
            {
                EmailVerificationType.CustomerRegistration => "AI Teknik Servis - Kayıt Doğrulama",
                EmailVerificationType.UserCreation => "AI Teknik Servis - Hesap Oluşturma Doğrulama",
                EmailVerificationType.PasswordReset => "AI Teknik Servis - Şifre Sıfırlama",
                EmailVerificationType.EmailChange => "AI Teknik Servis - Email Değişikliği Doğrulama",
                EmailVerificationType.AccountActivation => "AI Teknik Servis - Hesap Aktivasyonu",
                _ => "AI Teknik Servis - Email Doğrulama"
            };
        }

        /// <summary>
        /// Email içeriğini oluşturur
        /// </summary>
        private string GetEmailBody(EmailVerification verification)
        {
            var typeText = GetTypeDisplayName(verification.Type);
            var expiryMinutes = (int)(verification.ExpiryDate - DateTime.UtcNow).TotalMinutes;

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Email Doğrulama</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f8f9fa; }}
        .code {{ font-size: 32px; font-weight: bold; color: #007bff; text-align: center; 
                 padding: 20px; background-color: white; border: 2px dashed #007bff; 
                 margin: 20px 0; letter-spacing: 5px; }}
        .footer {{ padding: 20px; text-align: center; color: #666; font-size: 12px; }}
        .warning {{ color: #dc3545; font-weight: bold; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>AI Teknik Servis</h1>
            <h2>Email Doğrulama</h2>
        </div>
        <div class='content'>
            <h3>Merhaba,</h3>
            <p><strong>{typeText}</strong> işlemi için email doğrulama kodunuz:</p>
            
            <div class='code'>{verification.VerificationCode}</div>
            
            <p><strong>Önemli Bilgiler:</strong></p>
            <ul>
                <li>Bu kod <strong>{expiryMinutes} dakika</strong> geçerlidir</li>
                <li>Maksimum <strong>{verification.MaxRetries} kez</strong> deneyebilirsiniz</li>
                <li>Bu kodu kimseyle paylaşmayın</li>
            </ul>
            
            <p class='warning'>
                Eğer bu işlemi siz yapmadıysanız, bu email'i dikkate almayın ve 
                güvenliğiniz için şifrenizi değiştirin.
            </p>
        </div>
        <div class='footer'>
            <p>Bu email otomatik olarak gönderilmiştir. Lütfen yanıtlamayın.</p>
            <p>&copy; 2024 AI Teknik Servis. Tüm hakları saklıdır.</p>
        </div>
    </div>
</body>
</html>";
        }

        /// <summary>
        /// Doğrulama türünün görünen adını döndürür
        /// </summary>
        private string GetTypeDisplayName(EmailVerificationType type)
        {
            return type switch
            {
                EmailVerificationType.CustomerRegistration => "Müşteri Kaydı",
                EmailVerificationType.UserCreation => "Kullanıcı Oluşturma",
                EmailVerificationType.PasswordReset => "Şifre Sıfırlama",
                EmailVerificationType.EmailChange => "Email Değişikliği",
                EmailVerificationType.AccountActivation => "Hesap Aktivasyonu",
                _ => type.ToString()
            };
        }

        #endregion
    }
}