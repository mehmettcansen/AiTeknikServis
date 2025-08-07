using AiTeknikServis.Entities.Models;

namespace AiTeknikServis.Repositories.Contracts
{
    public interface IUserRepository
    {
        // Generic User Operations
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIdentityUserIdAsync(string identityUserId);
        Task<List<User>> GetAllAsync();
        Task<List<User>> GetByRoleAsync(UserRole role);
        Task<User> CreateAsync(User user);
        Task<User> UpdateAsync(User user);
        Task<bool> DeleteAsync(int id);

        // Customer Operations
        Task<Customer?> GetCustomerByIdAsync(int id);
        Task<Customer?> GetCustomerWithRequestsAsync(int id);
        Task<List<Customer>> GetAllCustomersAsync();
        Task<Customer> CreateCustomerAsync(Customer customer);
        Task<Customer> UpdateCustomerAsync(Customer customer);
        Task<List<Customer>> GetCustomersByCompanyAsync(string companyName);

        // Technician Operations
        Task<Technician?> GetTechnicianByIdAsync(int id);
        Task<Technician?> GetTechnicianWithAssignmentsAsync(int id);
        Task<List<Technician>> GetAllTechniciansAsync();
        Task<Technician> CreateTechnicianAsync(Technician technician);
        Task<Technician> UpdateTechnicianAsync(Technician technician);
        Task<List<Technician>> GetAvailableTechniciansAsync();
        Task<List<Technician>> GetTechniciansBySpecializationAsync(string specialization);
        Task<List<Technician>> GetTechniciansWithLowWorkloadAsync(int maxAssignments);

        // Manager Operations
        Task<Manager?> GetManagerByIdAsync(int id);
        Task<List<Manager>> GetAllManagersAsync();
        Task<Manager> CreateManagerAsync(Manager manager);
        Task<Manager> UpdateManagerAsync(Manager manager);


        // Admin Operations
        Task<Admin?> GetAdminByIdAsync(int id);
        Task<List<Admin>> GetAllAdminsAsync();
        Task<Admin> CreateAdminAsync(Admin admin);
        Task<Admin> UpdateAdminAsync(Admin admin);

        // Statistics
        Task<int> GetTotalUserCountAsync();
        Task<int> GetUserCountByRoleAsync(UserRole role);
        Task<int> GetActiveUserCountAsync();
    }
}