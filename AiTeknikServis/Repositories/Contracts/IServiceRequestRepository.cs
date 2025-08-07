using AiTeknikServis.Entities.Models;

namespace AiTeknikServis.Repositories.Contracts
{
    public interface IServiceRequestRepository
    {
        // Basic CRUD Operations
        Task<ServiceRequest?> GetByIdAsync(int id);
        Task<ServiceRequest?> GetByIdWithDetailsAsync(int id);
        Task<List<ServiceRequest>> GetAllAsync();
        Task<ServiceRequest> CreateAsync(ServiceRequest serviceRequest);
        Task<ServiceRequest> UpdateAsync(ServiceRequest serviceRequest);
        Task<bool> DeleteAsync(int id);

        // Query Operations
        Task<List<ServiceRequest>> GetByCustomerIdAsync(int customerId);
        Task<List<ServiceRequest>> GetByTechnicianIdAsync(int technicianId);
        Task<List<ServiceRequest>> GetByStatusAsync(ServiceStatus status);
        Task<List<ServiceRequest>> GetByPriorityAsync(Priority priority);
        Task<List<ServiceRequest>> GetByCategoryAsync(ServiceCategory category);
        Task<List<ServiceRequest>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        
        // Advanced Queries
        Task<List<ServiceRequest>> GetPendingRequestsAsync();
        Task<List<ServiceRequest>> GetOverdueRequestsAsync();
        Task<List<ServiceRequest>> GetUnassignedRequestsAsync();
        Task<List<ServiceRequest>> GetRequestsByTechnicianAvailabilityAsync();
        
        // Statistics
        Task<int> GetTotalCountAsync();
        Task<int> GetCountByStatusAsync(ServiceStatus status);
        Task<int> GetCountByCustomerAsync(int customerId);
        Task<int> GetCountByTechnicianAsync(int technicianId);
        
        // Search
        Task<List<ServiceRequest>> SearchAsync(string searchTerm);
        Task<List<ServiceRequest>> GetFilteredAsync(
            ServiceStatus? status = null,
            Priority? priority = null,
            ServiceCategory? category = null,
            int? customerId = null,
            int? technicianId = null,
            DateTime? startDate = null,
            DateTime? endDate = null);
    }
}