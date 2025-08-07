using AiTeknikServis.Entities.Dtos.User;
using AiTeknikServis.Entities.Models;

namespace AiTeknikServis.Services.Contracts
{
    public interface ICustomerService
    {
        /// <summary>
        /// Yeni müşteri oluşturur
        /// </summary>
        /// <param name="dto">Müşteri oluşturma DTO'su</param>
        /// <returns>Oluşturulan müşteri</returns>
        Task<CustomerResponseDto> CreateAsync(CustomerCreateDto dto);

        /// <summary>
        /// Müşteriyi günceller
        /// </summary>
        /// <param name="id">Müşteri ID'si</param>
        /// <param name="dto">Güncelleme DTO'su</param>
        /// <returns>Güncellenmiş müşteri</returns>
        Task<CustomerResponseDto> UpdateAsync(int id, CustomerUpdateDto dto);

        /// <summary>
        /// ID'ye göre müşteri getirir
        /// </summary>
        /// <param name="id">Müşteri ID'si</param>
        /// <returns>Müşteri detayları</returns>
        Task<CustomerResponseDto> GetByIdAsync(int id);

        /// <summary>
        /// E-posta adresine göre müşteri getirir
        /// </summary>
        /// <param name="email">E-posta adresi</param>
        /// <returns>Müşteri detayları</returns>
        Task<CustomerResponseDto?> GetByEmailAsync(string email);

        /// <summary>
        /// Identity kullanıcı ID'sine göre müşteri getirir
        /// </summary>
        /// <param name="identityUserId">Identity kullanıcı ID'si</param>
        /// <returns>Müşteri detayları</returns>
        Task<CustomerResponseDto?> GetByIdentityUserIdAsync(string identityUserId);

        /// <summary>
        /// Tüm müşterileri getirir
        /// </summary>
        /// <returns>Müşteri listesi</returns>
        Task<List<CustomerResponseDto>> GetAllAsync();

        /// <summary>
        /// Müşteriyi siler
        /// </summary>
        /// <param name="id">Müşteri ID'si</param>
        /// <returns>Silme işlemi başarılı mı?</returns>
        Task<bool> DeleteAsync(int id);
    }
}