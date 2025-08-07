namespace AiTeknikServis.Services.Contracts
{
    /// <summary>
    /// Kullanıcı validasyon servisi interface'i
    /// </summary>
    public interface IUserValidationService
    {
        /// <summary>
        /// Email adresinin sistemde kayıtlı olup olmadığını kontrol eder
        /// </summary>
        /// <param name="email">Kontrol edilecek email adresi</param>
        /// <returns>Email kayıtlı mı?</returns>
        Task<bool> IsEmailRegisteredAsync(string email);
        
        /// <summary>
        /// Email adresinin hangi kullanıcı türü için kayıtlı olduğunu döndürür
        /// </summary>
        /// <param name="email">Kontrol edilecek email adresi</param>
        /// <returns>Kullanıcı türü bilgisi</returns>
        Task<EmailRegistrationInfo?> GetEmailRegistrationInfoAsync(string email);
        
        /// <summary>
        /// Müşteri kaydı için email validasyonu
        /// </summary>
        /// <param name="email">Email adresi</param>
        /// <returns>Validasyon sonucu</returns>
        Task<ValidationResult> ValidateEmailForCustomerRegistrationAsync(string email);
        
        /// <summary>
        /// Admin/Manager/Teknisyen oluşturma için email validasyonu
        /// </summary>
        /// <param name="email">Email adresi</param>
        /// <param name="userType">Oluşturulacak kullanıcı türü</param>
        /// <returns>Validasyon sonucu</returns>
        Task<ValidationResult> ValidateEmailForUserCreationAsync(string email, string userType);
    }
    
    /// <summary>
    /// Email kayıt bilgisi
    /// </summary>
    public class EmailRegistrationInfo
    {
        public string Email { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime RegisteredDate { get; set; }
    }
    
    /// <summary>
    /// Validasyon sonucu
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
        public EmailRegistrationInfo? ExistingUser { get; set; }
    }
}