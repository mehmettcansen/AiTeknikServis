using Microsoft.AspNetCore.Identity;
using AiTeknikServis.Repositories.Contracts;
using AiTeknikServis.Services.Contracts;

namespace AiTeknikServis.Services
{
    /// <summary>
    /// Kullanıcı validasyon servisi implementasyonu
    /// </summary>
    public class UserValidationService : IUserValidationService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UserValidationService> _logger;

        public UserValidationService(
            UserManager<IdentityUser> userManager,
            IUserRepository userRepository,
            ILogger<UserValidationService> logger)
        {
            _userManager = userManager;
            _userRepository = userRepository;
            _logger = logger;
        }

        /// <summary>
        /// Email adresinin sistemde kayıtlı olup olmadığını kontrol eder
        /// </summary>
        public async Task<bool> IsEmailRegisteredAsync(string email)
        {
            try
            {
                var identityUser = await _userManager.FindByEmailAsync(email);
                return identityUser != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email kayıt kontrolü yapılırken hata: {Email}", email);
                return false;
            }
        }

        /// <summary>
        /// Email adresinin hangi kullanıcı türü için kayıtlı olduğunu döndürür
        /// </summary>
        public async Task<EmailRegistrationInfo?> GetEmailRegistrationInfoAsync(string email)
        {
            try
            {
                var identityUser = await _userManager.FindByEmailAsync(email);
                if (identityUser == null)
                    return null;

                var roles = await _userManager.GetRolesAsync(identityUser);
                var primaryRole = roles.FirstOrDefault() ?? "Unknown";

                // Kullanıcı detaylarını al
                var user = await _userRepository.GetByEmailAsync(email);
                
                return new EmailRegistrationInfo
                {
                    Email = email,
                    UserType = primaryRole,
                    FullName = user != null ? $"{user.FirstName} {user.LastName}" : "Bilinmiyor",
                    IsActive = user?.IsActive ?? true,
                    RegisteredDate = user?.CreatedDate ?? DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email kayıt bilgisi alınırken hata: {Email}", email);
                return null;
            }
        }

        /// <summary>
        /// Müşteri kaydı için email validasyonu
        /// </summary>
        public async Task<ValidationResult> ValidateEmailForCustomerRegistrationAsync(string email)
        {
            try
            {
                var existingUser = await GetEmailRegistrationInfoAsync(email);
                
                if (existingUser == null)
                {
                    return new ValidationResult
                    {
                        IsValid = true,
                        Message = "Email adresi kullanılabilir"
                    };
                }

                // Email zaten kayıtlı
                var userTypeText = GetUserTypeDisplayName(existingUser.UserType);
                
                return new ValidationResult
                {
                    IsValid = false,
                    Message = $"Bu email adresi zaten sistemde {userTypeText} olarak kayıtlı. Lütfen farklı bir email adresi kullanın.",
                    ErrorCode = "EMAIL_ALREADY_REGISTERED",
                    ExistingUser = existingUser
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Müşteri email validasyonu yapılırken hata: {Email}", email);
                return new ValidationResult
                {
                    IsValid = false,
                    Message = "Email validasyonu yapılırken bir hata oluştu",
                    ErrorCode = "VALIDATION_ERROR"
                };
            }
        }

        /// <summary>
        /// Admin/Manager/Teknisyen oluşturma için email validasyonu
        /// </summary>
        public async Task<ValidationResult> ValidateEmailForUserCreationAsync(string email, string userType)
        {
            try
            {
                var existingUser = await GetEmailRegistrationInfoAsync(email);
                
                if (existingUser == null)
                {
                    return new ValidationResult
                    {
                        IsValid = true,
                        Message = "Email adresi kullanılabilir"
                    };
                }

                // Email zaten kayıtlı
                var existingUserTypeText = GetUserTypeDisplayName(existingUser.UserType);
                var newUserTypeText = GetUserTypeDisplayName(userType);
                
                return new ValidationResult
                {
                    IsValid = false,
                    Message = $"Bu email adresi zaten sistemde {existingUserTypeText} olarak kayıtlı. {newUserTypeText} oluşturmak için farklı bir email adresi kullanın.",
                    ErrorCode = "EMAIL_ALREADY_REGISTERED",
                    ExistingUser = existingUser
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı oluşturma email validasyonu yapılırken hata: {Email}, UserType: {UserType}", email, userType);
                return new ValidationResult
                {
                    IsValid = false,
                    Message = "Email validasyonu yapılırken bir hata oluştu",
                    ErrorCode = "VALIDATION_ERROR"
                };
            }
        }

        /// <summary>
        /// Kullanıcı türünün görünen adını döndürür
        /// </summary>
        private string GetUserTypeDisplayName(string userType)
        {
            return userType.ToLower() switch
            {
                "admin" => "Yönetici",
                "manager" => "Manager",
                "technician" => "Teknisyen",
                "customer" => "Müşteri",
                _ => userType
            };
        }
    }
}