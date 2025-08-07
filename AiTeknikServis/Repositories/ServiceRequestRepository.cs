using Microsoft.EntityFrameworkCore;
using AiTeknikServis.Data;
using AiTeknikServis.Entities.Models;
using AiTeknikServis.Repositories.Contracts;

namespace AiTeknikServis.Repositories
{
    public class ServiceRequestRepository : IServiceRequestRepository
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// ServiceRequestRepository constructor - Veritabanı bağlamını enjekte eder
        /// </summary>
        public ServiceRequestRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// ID'ye göre servis talebini getirir (basit versiyon)
        /// </summary>
        public async Task<ServiceRequest?> GetByIdAsync(int id)
        {
            return await _context.ServiceRequests
                .FirstOrDefaultAsync(sr => sr.Id == id);
        }

        /// <summary>
        /// ID'ye göre servis talebini tüm ilişkili verilerle birlikte getirir
        /// </summary>
        public async Task<ServiceRequest?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.ServiceRequests
                .Include(sr => sr.Customer)
                .Include(sr => sr.AssignedTechnician)
                .Include(sr => sr.Files)
                .Include(sr => sr.AiPredictions)
                .Include(sr => sr.WorkAssignments)
                    .ThenInclude(wa => wa.Technician)
                .Include(sr => sr.Notifications)
                .FirstOrDefaultAsync(sr => sr.Id == id);
        }

        /// <summary>
        /// Tüm servis taleplerini müşteri ve teknisyen bilgileriyle birlikte getirir
        /// </summary>
        public async Task<List<ServiceRequest>> GetAllAsync()
        {
            return await _context.ServiceRequests
                .Include(sr => sr.Customer)
                .Include(sr => sr.AssignedTechnician)
                .OrderByDescending(sr => sr.CreatedDate)
                .ToListAsync();
        }

        /// <summary>
        /// Yeni bir servis talebi oluşturur
        /// </summary>
        public async Task<ServiceRequest> CreateAsync(ServiceRequest serviceRequest)
        {
            try
            {
                _context.ServiceRequests.Add(serviceRequest);
                await _context.SaveChangesAsync();
                return serviceRequest;
            }
            catch (Exception ex)
            {
                throw new Exception($"Servis talebi oluşturulurken hata: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Mevcut servis talebini günceller
        /// </summary>
        public async Task<ServiceRequest> UpdateAsync(ServiceRequest serviceRequest)
        {
            try
            {
                _context.ServiceRequests.Update(serviceRequest);
                await _context.SaveChangesAsync();
                return serviceRequest;
            }
            catch (Exception ex)
            {
                throw new Exception($"Servis talebi güncellenirken hata: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// ID'ye göre servis talebini siler
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var serviceRequest = await GetByIdAsync(id);
                if (serviceRequest == null)
                    return false;

                _context.ServiceRequests.Remove(serviceRequest);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Servis talebi silinirken hata: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Belirli bir müşteriye ait servis taleplerini getirir
        /// </summary>
        public async Task<List<ServiceRequest>> GetByCustomerIdAsync(int customerId)
        {
            return await _context.ServiceRequests
                .Include(sr => sr.AssignedTechnician)
                .Where(sr => sr.CustomerId == customerId)
                .OrderByDescending(sr => sr.CreatedDate)
                .ToListAsync();
        }

        /// <summary>
        /// Belirli bir teknisyene atanmış servis taleplerini getirir
        /// </summary>
        public async Task<List<ServiceRequest>> GetByTechnicianIdAsync(int technicianId)
        {
            return await _context.ServiceRequests
                .Include(sr => sr.Customer)
                .Where(sr => sr.AssignedTechnicianId == technicianId)
                .OrderByDescending(sr => sr.CreatedDate)
                .ToListAsync();
        }

        /// <summary>
        /// Belirli bir duruma göre servis taleplerini getirir
        /// </summary>
        public async Task<List<ServiceRequest>> GetByStatusAsync(ServiceStatus status)
        {
            return await _context.ServiceRequests
                .Include(sr => sr.Customer)
                .Include(sr => sr.AssignedTechnician)
                .Where(sr => sr.Status == status)
                .OrderByDescending(sr => sr.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<ServiceRequest>> GetByPriorityAsync(Priority priority)
        {
            return await _context.ServiceRequests
                .Include(sr => sr.Customer)
                .Include(sr => sr.AssignedTechnician)
                .Where(sr => sr.Priority == priority)
                .OrderByDescending(sr => sr.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<ServiceRequest>> GetByCategoryAsync(ServiceCategory category)
        {
            return await _context.ServiceRequests
                .Include(sr => sr.Customer)
                .Include(sr => sr.AssignedTechnician)
                .Where(sr => sr.Category == category)
                .OrderByDescending(sr => sr.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<ServiceRequest>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.ServiceRequests
                .Include(sr => sr.Customer)
                .Include(sr => sr.AssignedTechnician)
                .Where(sr => sr.CreatedDate >= startDate && sr.CreatedDate <= endDate)
                .OrderByDescending(sr => sr.CreatedDate)
                .ToListAsync();
        }

        /// <summary>
        /// Bekleyen servis taleplerini öncelik sırasına göre getirir
        /// </summary>
        public async Task<List<ServiceRequest>> GetPendingRequestsAsync()
        {
            return await _context.ServiceRequests
                .Include(sr => sr.Customer)
                .Where(sr => sr.Status == ServiceStatus.Pending)
                .OrderBy(sr => sr.Priority)
                .ThenBy(sr => sr.CreatedDate)
                .ToListAsync();
        }

        /// <summary>
        /// Süresi geçmiş servis taleplerini getirir
        /// </summary>
        public async Task<List<ServiceRequest>> GetOverdueRequestsAsync()
        {
            var currentDate = DateTime.UtcNow;
            return await _context.ServiceRequests
                .Include(sr => sr.Customer)
                .Include(sr => sr.AssignedTechnician)
                .Where(sr => sr.ScheduledDate.HasValue && 
                           sr.ScheduledDate < currentDate && 
                           sr.Status != ServiceStatus.Completed &&
                           sr.Status != ServiceStatus.Cancelled)
                .OrderBy(sr => sr.ScheduledDate)
                .ToListAsync();
        }

        /// <summary>
        /// Henüz teknisyen atanmamış servis taleplerini getirir
        /// </summary>
        public async Task<List<ServiceRequest>> GetUnassignedRequestsAsync()
        {
            return await _context.ServiceRequests
                .Include(sr => sr.Customer)
                .Where(sr => sr.AssignedTechnicianId == null && 
                           sr.Status == ServiceStatus.Pending)
                .OrderBy(sr => sr.Priority)
                .ThenBy(sr => sr.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<ServiceRequest>> GetRequestsByTechnicianAvailabilityAsync()
        {
            return await _context.ServiceRequests
                .Include(sr => sr.Customer)
                .Include(sr => sr.AssignedTechnician)
                .Where(sr => sr.AssignedTechnician != null)
                .OrderByDescending(sr => sr.CreatedDate)
                .ToListAsync();
        }

        public async Task<int> GetTotalCountAsync()
        {
            return await _context.ServiceRequests.CountAsync();
        }

        public async Task<int> GetCountByStatusAsync(ServiceStatus status)
        {
            return await _context.ServiceRequests
                .CountAsync(sr => sr.Status == status);
        }

        public async Task<int> GetCountByCustomerAsync(int customerId)
        {
            return await _context.ServiceRequests
                .CountAsync(sr => sr.CustomerId == customerId);
        }

        public async Task<int> GetCountByTechnicianAsync(int technicianId)
        {
            return await _context.ServiceRequests
                .CountAsync(sr => sr.AssignedTechnicianId == technicianId);
        }

        public async Task<List<ServiceRequest>> SearchAsync(string searchTerm)
        {
            return await _context.ServiceRequests
                .Include(sr => sr.Customer)
                .Include(sr => sr.AssignedTechnician)
                .Where(sr => sr.Title.Contains(searchTerm) || 
                           sr.Description.Contains(searchTerm) ||
                           sr.Customer.FirstName.Contains(searchTerm) ||
                           sr.Customer.LastName.Contains(searchTerm) ||
                           (sr.Customer.CompanyName != null && sr.Customer.CompanyName.Contains(searchTerm)))
                .OrderByDescending(sr => sr.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<ServiceRequest>> GetFilteredAsync(
            ServiceStatus? status = null,
            Priority? priority = null,
            ServiceCategory? category = null,
            int? customerId = null,
            int? technicianId = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var query = _context.ServiceRequests
                .Include(sr => sr.Customer)
                .Include(sr => sr.AssignedTechnician)
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(sr => sr.Status == status.Value);

            if (priority.HasValue)
                query = query.Where(sr => sr.Priority == priority.Value);

            if (category.HasValue)
                query = query.Where(sr => sr.Category == category.Value);

            if (customerId.HasValue)
                query = query.Where(sr => sr.CustomerId == customerId.Value);

            if (technicianId.HasValue)
                query = query.Where(sr => sr.AssignedTechnicianId == technicianId.Value);

            if (startDate.HasValue)
                query = query.Where(sr => sr.CreatedDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(sr => sr.CreatedDate <= endDate.Value);

            return await query
                .OrderByDescending(sr => sr.CreatedDate)
                .ToListAsync();
        }
    }
}