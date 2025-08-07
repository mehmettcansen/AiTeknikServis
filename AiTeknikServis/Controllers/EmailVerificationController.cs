using Microsoft.AspNetCore.Mvc;
using AiTeknikServis.Entities.Dtos.EmailVerification;
using AiTeknikServis.Entities.Models;
using AiTeknikServis.Services.Contracts;

namespace AiTeknikServis.Controllers
{
    /// <summary>
    /// Email doğrulama controller'ı
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class EmailVerificationController : ControllerBase
    {
        private readonly IEmailVerificationService _emailVerificationService;
        private readonly ILogger<EmailVerificationController> _logger;

        public EmailVerificationController(
            IEmailVerificationService emailVerificationService,
            ILogger<EmailVerificationController> logger)
        {
            _emailVerificationService = emailVerificationService;
            _logger = logger;
        }

        /// <summary>
        /// Email doğrulama kodu gönderir
        /// </summary>
        /// <param name="dto">Email doğrulama oluşturma DTO'su</param>
        /// <returns>Doğrulama sonucu</returns>
        [HttpPost("send-code")]
        public async Task<IActionResult> SendVerificationCode([FromBody] EmailVerificationCreateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Rate limiting kontrolü
                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var canProceed = await _emailVerificationService.CheckRateLimitAsync(clientIp, dto.Email);
                
                if (!canProceed)
                {
                    return StatusCode(429, new { 
                        message = "Çok fazla istek gönderdiniz. Lütfen daha sonra tekrar deneyin.",
                        errorCode = "RATE_LIMITED"
                    });
                }

                var result = await _emailVerificationService.CreateVerificationCodeAsync(dto);

                if (result.IsSuccess)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email doğrulama kodu gönderilirken hata: {Email}", dto.Email);
                return StatusCode(500, new { 
                    message = "Sunucu hatası oluştu",
                    errorCode = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Email doğrulama kodunu doğrular
        /// </summary>
        /// <param name="dto">Email doğrulama DTO'su</param>
        /// <returns>Doğrulama sonucu</returns>
        [HttpPost("verify-code")]
        public async Task<IActionResult> VerifyCode([FromBody] EmailVerificationVerifyDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _emailVerificationService.VerifyCodeAsync(dto);

                if (result.IsSuccess)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email doğrulama kodunu doğrularken hata: {Email}", dto.Email);
                return StatusCode(500, new { 
                    message = "Sunucu hatası oluştu",
                    errorCode = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Doğrulama kodunu yeniden gönderir
        /// </summary>
        /// <param name="email">Email adresi</param>
        /// <param name="type">Doğrulama türü</param>
        /// <returns>Yeniden gönderim sonucu</returns>
        [HttpPost("resend-code")]
        public async Task<IActionResult> ResendCode([FromQuery] string email, [FromQuery] EmailVerificationType type)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    return BadRequest(new { message = "Email adresi gereklidir" });
                }

                // Rate limiting kontrolü
                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var canProceed = await _emailVerificationService.CheckRateLimitAsync(clientIp, email);
                
                if (!canProceed)
                {
                    return StatusCode(429, new { 
                        message = "Çok fazla istek gönderdiniz. Lütfen daha sonra tekrar deneyin.",
                        errorCode = "RATE_LIMITED"
                    });
                }

                var result = await _emailVerificationService.ResendVerificationCodeAsync(email, type);

                if (result.IsSuccess)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Doğrulama kodu yeniden gönderilirken hata: {Email}", email);
                return StatusCode(500, new { 
                    message = "Sunucu hatası oluştu",
                    errorCode = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Aktif doğrulama kodunu getirir
        /// </summary>
        /// <param name="email">Email adresi</param>
        /// <param name="type">Doğrulama türü</param>
        /// <returns>Aktif doğrulama kodu bilgisi</returns>
        [HttpGet("active-verification")]
        public async Task<IActionResult> GetActiveVerification([FromQuery] string email, [FromQuery] EmailVerificationType type)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    return BadRequest(new { message = "Email adresi gereklidir" });
                }

                var verification = await _emailVerificationService.GetActiveVerificationAsync(email, type);

                if (verification == null)
                {
                    return NotFound(new { message = "Aktif doğrulama kodu bulunamadı" });
                }

                // Güvenlik için doğrulama kodunu gizle
                verification.VerificationCode = "******";

                return Ok(verification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Aktif doğrulama kodu getirilirken hata: {Email}", email);
                return StatusCode(500, new { 
                    message = "Sunucu hatası oluştu",
                    errorCode = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Email doğrulama geçmişini getirir
        /// </summary>
        /// <param name="email">Email adresi</param>
        /// <param name="limit">Maksimum kayıt sayısı</param>
        /// <returns>Doğrulama geçmişi</returns>
        [HttpGet("history")]
        public async Task<IActionResult> GetVerificationHistory([FromQuery] string email, [FromQuery] int limit = 10)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    return BadRequest(new { message = "Email adresi gereklidir" });
                }

                if (limit <= 0 || limit > 50)
                {
                    limit = 10;
                }

                var history = await _emailVerificationService.GetVerificationHistoryAsync(email, limit);

                // Güvenlik için doğrulama kodlarını gizle
                foreach (var item in history)
                {
                    item.VerificationCode = "******";
                }

                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Doğrulama geçmişi getirilirken hata: {Email}", email);
                return StatusCode(500, new { 
                    message = "Sunucu hatası oluştu",
                    errorCode = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Email doğrulama istatistiklerini getirir (Admin only)
        /// </summary>
        /// <param name="startDate">Başlangıç tarihi</param>
        /// <param name="endDate">Bitiş tarihi</param>
        /// <returns>İstatistikler</returns>
        [HttpGet("statistics")]
        // [Authorize(Roles = "Admin")] // Uncomment when authentication is implemented
        public async Task<IActionResult> GetStatistics([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                var statistics = await _emailVerificationService.GetStatisticsAsync(startDate, endDate);
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email doğrulama istatistikleri getirilirken hata");
                return StatusCode(500, new { 
                    message = "Sunucu hatası oluştu",
                    errorCode = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Doğrulama kodu formatını kontrol eder
        /// </summary>
        /// <param name="code">Doğrulama kodu</param>
        /// <returns>Format geçerli mi?</returns>
        [HttpGet("validate-code-format")]
        public IActionResult ValidateCodeFormat([FromQuery] string code)
        {
            try
            {
                var isValid = _emailVerificationService.IsValidCodeFormat(code);
                return Ok(new { isValid, message = isValid ? "Kod formatı geçerli" : "Kod formatı geçersiz" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kod formatı kontrol edilirken hata: {Code}", code);
                return StatusCode(500, new { 
                    message = "Sunucu hatası oluştu",
                    errorCode = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Email adresinin doğrulama için uygun olup olmadığını kontrol eder
        /// </summary>
        /// <param name="email">Email adresi</param>
        /// <returns>Email uygun mu?</returns>
        [HttpGet("check-email-eligibility")]
        public async Task<IActionResult> CheckEmailEligibility([FromQuery] string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    return BadRequest(new { message = "Email adresi gereklidir" });
                }

                var isEligible = await _emailVerificationService.IsEmailEligibleForVerificationAsync(email);
                return Ok(new { 
                    isEligible, 
                    message = isEligible ? "Email doğrulama için uygun" : "Email doğrulama için uygun değil" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email uygunluk kontrolü yapılırken hata: {Email}", email);
                return StatusCode(500, new { 
                    message = "Sunucu hatası oluştu",
                    errorCode = "INTERNAL_ERROR"
                });
            }
        }
    }
}