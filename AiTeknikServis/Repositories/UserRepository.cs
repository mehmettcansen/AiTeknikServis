using Microsoft.EntityFrameworkCore;
using AiTeknikServis.Data;
using AiTeknikServis.Entities.Models;
using AiTeknikServis.Repositories.Contracts;

namespace AiTeknikServis.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// UserRepository constructor - Veritabanı bağlamını enjekte eder
        /// </summary>
        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// ID'ye göre kullanıcıyı getirir (tüm kullanıcı tiplerinde arar)
        /// </summary>
        public async Task<User?> GetByIdAsync(int id)
        {
            // Tüm kullanıcı tiplerinde sırayla arar
            var customer = await _context.Customers.FirstOrDefaultAsync(u => u.Id == id);
            if (customer != null) return customer;

            var technician = await _context.Technicians.FirstOrDefaultAsync(u => u.Id == id);
            if (technician != null) return technician;

            var manager = await _context.Managers.FirstOrDefaultAsync(u => u.Id == id);
            if (manager != null) return manager;

            var admin = await _context.Admins.FirstOrDefaultAsync(u => u.Id == id);
            return admin;
        }

        /// <summary>
        /// Email adresine göre kullanıcıyı getirir (tüm kullanıcı tiplerinde arar)
        /// </summary>
        public async Task<User?> GetByEmailAsync(string email)
        {
            // Tüm kullanıcı tiplerinde sırayla arar
            var customer = await _context.Customers.FirstOrDefaultAsync(u => u.Email == email);
            if (customer != null) return customer;

            var technician = await _context.Technicians.FirstOrDefaultAsync(u => u.Email == email);
            if (technician != null) return technician;

            var manager = await _context.Managers.FirstOrDefaultAsync(u => u.Email == email);
            if (manager != null) return manager;

            var admin = await _context.Admins.FirstOrDefaultAsync(u => u.Email == email);
            return admin;
        }

        /// <summary>
        /// Identity kullanıcı ID'sine göre kullanıcıyı getirir
        /// </summary>
        public async Task<User?> GetByIdentityUserIdAsync(string identityUserId)
        {
            // Tüm kullanıcı tiplerinde sırayla arar
            var customer = await _context.Customers.FirstOrDefaultAsync(u => u.IdentityUserId == identityUserId);
            if (customer != null) return customer;

            var technician = await _context.Technicians.FirstOrDefaultAsync(u => u.IdentityUserId == identityUserId);
            if (technician != null) return technician;

            var manager = await _context.Managers.FirstOrDefaultAsync(u => u.IdentityUserId == identityUserId);
            if (manager != null) return manager;

            var admin = await _context.Admins.FirstOrDefaultAsync(u => u.IdentityUserId == identityUserId);
            return admin;
        }

        public async Task<List<User>> GetAllAsync()
        {
            var users = new List<User>();
            users.AddRange(await _context.Customers.ToListAsync());
            users.AddRange(await _context.Technicians.ToListAsync());
            users.AddRange(await _context.Managers.ToListAsync());
            users.AddRange(await _context.Admins.ToListAsync());
            return users.OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToList();
        }

        public async Task<List<User>> GetByRoleAsync(UserRole role)
        {
            return role switch
            {
                UserRole.Customer => (await _context.Customers.ToListAsync()).Cast<User>().ToList(),
                UserRole.Technician => (await _context.Technicians.ToListAsync()).Cast<User>().ToList(),
                UserRole.Manager => (await _context.Managers.ToListAsync()).Cast<User>().ToList(),
                UserRole.Admin => (await _context.Admins.ToListAsync()).Cast<User>().ToList(),
                _ => new List<User>()
            };
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var user = await GetByIdAsync(id);
                if (user == null) return false;

                switch (user.Role)
                {
                    case UserRole.Customer:
                        _context.Customers.Remove((Customer)user);
                        break;
                    case UserRole.Technician:
                        _context.Technicians.Remove((Technician)user);
                        break;
                    case UserRole.Manager:
                        _context.Managers.Remove((Manager)user);
                        break;
                    case UserRole.Admin:
                        _context.Admins.Remove((Admin)user);
                        break;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting user: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// ID'ye göre müşteri bilgilerini getirir
        /// </summary>
        public async Task<Customer?> GetCustomerByIdAsync(int id)
        {
            return await _context.Customers.FirstOrDefaultAsync(c => c.Id == id);
        }

        /// <summary>
        /// ID'ye göre müşteriyi servis talepleriyle birlikte getirir
        /// </summary>
        public async Task<Customer?> GetCustomerWithRequestsAsync(int id)
        {
            return await _context.Customers
                .Include(c => c.ServiceRequests)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        /// <summary>
        /// Tüm müşterileri alfabetik sıraya göre getirir
        /// </summary>
        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            return await _context.Customers
                .OrderBy(c => c.FirstName)
                .ThenBy(c => c.LastName)
                .ToListAsync();
        }

        public async Task<Customer> CreateCustomerAsync(Customer customer)
        {
            try
            {
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
                return customer;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating customer: {ex.Message}", ex);
            }
        }

        public async Task<Customer> UpdateCustomerAsync(Customer customer)
        {
            try
            {
                _context.Customers.Update(customer);
                await _context.SaveChangesAsync();
                return customer;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating customer: {ex.Message}", ex);
            }
        }

        public async Task<List<Customer>> GetCustomersByCompanyAsync(string companyName)
        {
            return await _context.Customers
                .Where(c => c.CompanyName != null && c.CompanyName.Contains(companyName))
                .OrderBy(c => c.FirstName)
                .ThenBy(c => c.LastName)
                .ToListAsync();
        }

        /// <summary>
        /// ID'ye göre teknisyen bilgilerini getirir
        /// </summary>
        public async Task<Technician?> GetTechnicianByIdAsync(int id)
        {
            return await _context.Technicians.FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<Technician?> GetTechnicianWithAssignmentsAsync(int id)
        {
            return await _context.Technicians
                .Include(t => t.Assignments)
                    .ThenInclude(a => a.ServiceRequest)
                .Include(t => t.AssignedRequests)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<List<Technician>> GetAllTechniciansAsync()
        {
            return await _context.Technicians
                .OrderBy(t => t.FirstName)
                .ThenBy(t => t.LastName)
                .ToListAsync();
        }

        public async Task<Technician> CreateTechnicianAsync(Technician technician)
        {
            try
            {
                _context.Technicians.Add(technician);
                await _context.SaveChangesAsync();
                return technician;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating technician: {ex.Message}", ex);
            }
        }

        public async Task<Technician> UpdateTechnicianAsync(Technician technician)
        {
            try
            {
                _context.Technicians.Update(technician);
                await _context.SaveChangesAsync();
                return technician;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating technician: {ex.Message}", ex);
            }
        }

        public async Task<List<Technician>> GetAvailableTechniciansAsync()
        {
            return await _context.Technicians
                .Where(t => t.IsActive)
                .OrderBy(t => t.FirstName)
                .ThenBy(t => t.LastName)
                .ToListAsync();
        }

        public async Task<List<Technician>> GetTechniciansBySpecializationAsync(string specialization)
        {
            return await _context.Technicians
                .Where(t => t.Specializations != null && t.Specializations.Contains(specialization))
                .Where(t => t.IsActive)
                .OrderBy(t => t.FirstName)
                .ThenBy(t => t.LastName)
                .ToListAsync();
        }

        public async Task<List<Technician>> GetTechniciansWithLowWorkloadAsync(int maxAssignments)
        {
            return await _context.Technicians
                .Include(t => t.Assignments)
                .Where(t => t.IsActive)
                .Where(t => t.Assignments.Count(a => a.Status == WorkAssignmentStatus.Assigned || a.Status == WorkAssignmentStatus.InProgress) < maxAssignments)
                .OrderBy(t => t.Assignments.Count(a => a.Status == WorkAssignmentStatus.Assigned || a.Status == WorkAssignmentStatus.InProgress))
                .ToListAsync();
        }

        // Manager Operations
        public async Task<Manager?> GetManagerByIdAsync(int id)
        {
            return await _context.Managers
                .Include(m => m.Supervisor)
                .Include(m => m.Subordinates)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<List<Manager>> GetAllManagersAsync()
        {
            return await _context.Managers
                .Include(m => m.Supervisor)
                .OrderBy(m => m.FirstName)
                .ThenBy(m => m.LastName)
                .ToListAsync();
        }

        public async Task<Manager> CreateManagerAsync(Manager manager)
        {
            try
            {
                _context.Managers.Add(manager);
                await _context.SaveChangesAsync();
                return manager;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating manager: {ex.Message}", ex);
            }
        }

        public async Task<Manager> UpdateManagerAsync(Manager manager)
        {
            try
            {
                _context.Managers.Update(manager);
                await _context.SaveChangesAsync();
                return manager;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating manager: {ex.Message}", ex);
            }
        }



        // Admin Operations
        public async Task<Admin?> GetAdminByIdAsync(int id)
        {
            return await _context.Admins.FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<List<Admin>> GetAllAdminsAsync()
        {
            return await _context.Admins
                .OrderBy(a => a.FirstName)
                .ThenBy(a => a.LastName)
                .ToListAsync();
        }

        public async Task<Admin> CreateAdminAsync(Admin admin)
        {
            try
            {
                _context.Admins.Add(admin);
                await _context.SaveChangesAsync();
                return admin;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating admin: {ex.Message}", ex);
            }
        }

        public async Task<Admin> UpdateAdminAsync(Admin admin)
        {
            try
            {
                _context.Admins.Update(admin);
                await _context.SaveChangesAsync();
                return admin;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating admin: {ex.Message}", ex);
            }
        }

        // Statistics
        public async Task<int> GetTotalUserCountAsync()
        {
            var customerCount = await _context.Customers.CountAsync();
            var technicianCount = await _context.Technicians.CountAsync();
            var managerCount = await _context.Managers.CountAsync();
            var adminCount = await _context.Admins.CountAsync();
            return customerCount + technicianCount + managerCount + adminCount;
        }

        public async Task<int> GetUserCountByRoleAsync(UserRole role)
        {
            return role switch
            {
                UserRole.Customer => await _context.Customers.CountAsync(),
                UserRole.Technician => await _context.Technicians.CountAsync(),
                UserRole.Manager => await _context.Managers.CountAsync(),
                UserRole.Admin => await _context.Admins.CountAsync(),
                _ => 0
            };
        }

        public async Task<int> GetActiveUserCountAsync()
        {
            var customerCount = await _context.Customers.CountAsync(c => c.IsActive);
            var technicianCount = await _context.Technicians.CountAsync(t => t.IsActive);
            var managerCount = await _context.Managers.CountAsync(m => m.IsActive);
            var adminCount = await _context.Admins.CountAsync(a => a.IsActive);
            return customerCount + technicianCount + managerCount + adminCount;
        }

        /// <summary>
        /// Generic user creation method
        /// </summary>
        public async Task<User> CreateAsync(User user)
        {
            user.CreatedDate = DateTime.UtcNow;
            
            switch (user.Role)
            {
                case UserRole.Customer:
                    var customer = user as Customer ?? throw new ArgumentException("Invalid user type for Customer role");
                    _context.Customers.Add(customer);
                    break;
                case UserRole.Technician:
                    var technician = user as Technician ?? throw new ArgumentException("Invalid user type for Technician role");
                    _context.Technicians.Add(technician);
                    break;
                case UserRole.Manager:
                    var manager = user as Manager ?? throw new ArgumentException("Invalid user type for Manager role");
                    _context.Managers.Add(manager);
                    break;
                case UserRole.Admin:
                    var admin = user as Admin ?? throw new ArgumentException("Invalid user type for Admin role");
                    _context.Admins.Add(admin);
                    break;
                default:
                    throw new ArgumentException($"Unsupported user role: {user.Role}");
            }
            
            await _context.SaveChangesAsync();
            return user;
        }

        /// <summary>
        /// Generic user update method
        /// </summary>
        public async Task<User> UpdateAsync(User user)
        {
            switch (user.Role)
            {
                case UserRole.Customer:
                    var customer = user as Customer ?? throw new ArgumentException("Invalid user type for Customer role");
                    _context.Customers.Update(customer);
                    break;
                case UserRole.Technician:
                    var technician = user as Technician ?? throw new ArgumentException("Invalid user type for Technician role");
                    _context.Technicians.Update(technician);
                    break;
                case UserRole.Manager:
                    var manager = user as Manager ?? throw new ArgumentException("Invalid user type for Manager role");
                    _context.Managers.Update(manager);
                    break;
                case UserRole.Admin:
                    var admin = user as Admin ?? throw new ArgumentException("Invalid user type for Admin role");
                    _context.Admins.Update(admin);
                    break;
                default:
                    throw new ArgumentException($"Unsupported user role: {user.Role}");
            }
            
            await _context.SaveChangesAsync();
            return user;
        }
    }
}