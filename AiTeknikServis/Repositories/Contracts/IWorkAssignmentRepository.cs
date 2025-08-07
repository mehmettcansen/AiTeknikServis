using AiTeknikServis.Entities.Models;

namespace AiTeknikServis.Repositories.Contracts
{
    public interface IWorkAssignmentRepository
    {
        // Basic CRUD Operations
        Task<WorkAssignment?> GetByIdAsync(int id);
        Task<WorkAssignment?> GetByIdWithDetailsAsync(int id);
        Task<List<WorkAssignment>> GetAllAsync();
        Task<WorkAssignment> CreateAsync(WorkAssignment workAssignment);
        Task<WorkAssignment> UpdateAsync(WorkAssignment workAssignment);
        Task<bool> DeleteAsync(int id);

        // Query Operations
        Task<List<WorkAssignment>> GetByServiceRequestIdAsync(int serviceRequestId);
        Task<List<WorkAssignment>> GetByTechnicianIdAsync(int technicianId);
        Task<List<WorkAssignment>> GetByStatusAsync(WorkAssignmentStatus status);
        Task<List<WorkAssignment>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);

        // Technician Workload Operations
        Task<List<WorkAssignment>> GetActiveTechnicianAssignmentsAsync(int technicianId);
        Task<int> GetTechnicianActiveAssignmentCountAsync(int technicianId);
        Task<List<WorkAssignment>> GetTechnicianAssignmentsByDateAsync(int technicianId, DateTime date);
        Task<List<WorkAssignment>> GetOverdueAssignmentsAsync();

        // Scheduling Operations
        Task<List<WorkAssignment>> GetScheduledAssignmentsAsync(DateTime date);
        Task<List<WorkAssignment>> GetAssignmentsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<bool> IsTechnicianAvailableAsync(int technicianId, DateTime scheduledDate);

        // Statistics
        Task<int> GetTotalAssignmentCountAsync();
        Task<int> GetAssignmentCountByStatusAsync(WorkAssignmentStatus status);
        Task<int> GetAssignmentCountByTechnicianAsync(int technicianId);
        Task<double> GetAverageCompletionTimeAsync();
        Task<double> GetTechnicianAverageCompletionTimeAsync(int technicianId);

        // Performance Metrics
        Task<List<WorkAssignment>> GetCompletedAssignmentsByTechnicianAsync(int technicianId, DateTime? startDate = null, DateTime? endDate = null);
        Task<Dictionary<int, int>> GetTechnicianWorkloadSummaryAsync();
        Task<List<WorkAssignment>> GetAssignmentsByPriorityAsync(Priority priority);
    }
}