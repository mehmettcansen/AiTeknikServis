using Microsoft.AspNetCore.Mvc;
using AiTeknikServis.Services.Contracts;

namespace AiTeknikServis.Controllers.Api
{
    /// <summary>
    /// Kullanıcı validasyon API controller'ı
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UserValidationController : ControllerBase
    {
        private readonly IUserValidationService _userValidationService;
        private readonly ILogger<UserValidationController> _logger;

        public UserValidationController(
            IUserValidationService userValidationService,
            ILogger<UserValidationController> logger)
        {
            _userValidationService = userValidationService;
            _logger = logger;
        }

        /// <summary>
        /// Email adresinin kayıtlı olup olmadığını kontrol eder
        /// </summary>
        [HttpGet("check-email")]
        public async Task<IActionResult> CheckEmail([FromQuery] string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    return BadRequest(new { message = "Email adresi gereklidir" });
                }

                var isRegistered = await _userValidationService.IsEmailRegisteredAsync(email);
                var registrationInfo = await _userValidationService.GetEmailRegistrationInfoAsync(email);

                return Ok(new
                {
                    email,
                    isRegistered,
                    registrationInfo,
                    message = isRegistered ? "Email adresi sistemde kayıtlı" : "Email adresi kullanılabilir"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email kontrolü yapılırken hata: {Email}", email);
                return StatusCode(500, new { message = "Email kontrolü yapılamadı", error = ex.Message });
            }
        }

        /// <summary>
        /// Müşteri kaydı için email validasyonu
        /// </summary>
        [HttpPost("validate-customer-email")]
        public async Task<IActionResult> ValidateCustomerEmail([FromBody] EmailValidationRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Email))
                {
                    return BadRequest(new { message = "Email adresi gereklidir" });
                }

                var result = await _userValidationService.ValidateEmailForCustomerRegistrationAsync(request.Email);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Müşteri email validasyonu yapılırken hata: {Email}", request.Email);
                return StatusCode(500, new { message = "Email validasyonu yapılamadı", error = ex.Message });
            }
        }

        /// <summary>
        /// Kullanıcı oluşturma için email validasyonu
        /// </summary>
        [HttpPost("validate-user-email")]
        public async Task<IActionResult> ValidateUserEmail([FromBody] UserEmailValidationRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Email))
                {
                    return BadRequest(new { message = "Email adresi gereklidir" });
                }

                if (string.IsNullOrWhiteSpace(request.UserType))
                {
                    return BadRequest(new { message = "Kullanıcı türü gereklidir" });
                }

                var result = await _userValidationService.ValidateEmailForUserCreationAsync(request.Email, request.UserType);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı email validasyonu yapılırken hata: {Email}, UserType: {UserType}", 
                    request.Email, request.UserType);
                return StatusCode(500, new { message = "Email validasyonu yapılamadı", error = ex.Message });
            }
        }
    }

    /// <summary>
    /// Email validasyon isteği
    /// </summary>
    public class EmailValidationRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// Kullanıcı email validasyon isteği
    /// </summary>
    public class UserEmailValidationRequest
    {
        public string Email { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
    }
}