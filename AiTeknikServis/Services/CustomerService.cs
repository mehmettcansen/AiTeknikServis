using AutoMapper;
using AiTeknikServis.Entities.Dtos.User;
using AiTeknikServis.Entities.Models;
using AiTeknikServis.Repositories.Contracts;
using AiTeknikServis.Services.Contracts;

namespace AiTeknikServis.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<CustomerService> _logger;

        public CustomerService(
            IUserRepository userRepository,
            IMapper mapper,
            ILogger<CustomerService> logger)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<CustomerResponseDto> CreateAsync(CustomerCreateDto dto)
        {
            try
            {
                var customer = _mapper.Map<Customer>(dto);
                customer.Role = UserRole.Customer;
                customer.CreatedDate = DateTime.UtcNow;
                customer.IsActive = true;

                var createdCustomer = await _userRepository.CreateCustomerAsync(customer);
                return _mapper.Map<CustomerResponseDto>(createdCustomer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Müşteri oluşturulurken hata: {Email}", dto.Email);
                throw;
            }
        }

        public async Task<CustomerResponseDto> UpdateAsync(int id, CustomerUpdateDto dto)
        {
            try
            {
                var customer = await _userRepository.GetCustomerByIdAsync(id);
                if (customer == null)
                {
                    throw new ArgumentException($"ID {id} ile müşteri bulunamadı.");
                }

                _mapper.Map(dto, customer);
                var updatedCustomer = await _userRepository.UpdateCustomerAsync(customer);
                return _mapper.Map<CustomerResponseDto>(updatedCustomer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Müşteri güncellenirken hata: {Id}", id);
                throw;
            }
        }

        public async Task<CustomerResponseDto> GetByIdAsync(int id)
        {
            try
            {
                var customer = await _userRepository.GetCustomerWithRequestsAsync(id);
                if (customer == null)
                {
                    throw new ArgumentException($"ID {id} ile müşteri bulunamadı.");
                }

                return _mapper.Map<CustomerResponseDto>(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Müşteri getirilirken hata: {Id}", id);
                throw;
            }
        }

        public async Task<CustomerResponseDto?> GetByEmailAsync(string email)
        {
            try
            {
                var user = await _userRepository.GetByEmailAsync(email);
                if (user is Customer customer)
                {
                    return _mapper.Map<CustomerResponseDto>(customer);
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "E-posta ile müşteri getirilirken hata: {Email}", email);
                throw;
            }
        }

        public async Task<CustomerResponseDto?> GetByIdentityUserIdAsync(string identityUserId)
        {
            try
            {
                var user = await _userRepository.GetByIdentityUserIdAsync(identityUserId);
                if (user is Customer customer)
                {
                    return _mapper.Map<CustomerResponseDto>(customer);
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Identity kullanıcı ID ile müşteri getirilirken hata: {IdentityUserId}", identityUserId);
                throw;
            }
        }

        public async Task<List<CustomerResponseDto>> GetAllAsync()
        {
            try
            {
                var customers = await _userRepository.GetAllCustomersAsync();
                return _mapper.Map<List<CustomerResponseDto>>(customers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tüm müşteriler getirilirken hata");
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var customer = await _userRepository.GetCustomerByIdAsync(id);
                if (customer == null)
                {
                    return false;
                }

                return await _userRepository.DeleteAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Müşteri silinirken hata: {Id}", id);
                throw;
            }
        }
    }
}