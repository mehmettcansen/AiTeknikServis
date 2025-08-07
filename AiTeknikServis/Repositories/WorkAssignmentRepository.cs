using Microsoft.EntityFrameworkCore;
using AiTeknikServis.Data;
using AiTeknikServis.Entities.Models;
using AiTeknikServis.Repositories.Contracts;

namespace AiTeknikServis.Repositories
{
    public class WorkAssignmentRepository : IWorkAssignmentRepository
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// WorkAssignmentRepository constructor - Veritabanı bağlamını enjekte eder
        /// </summary>
        public WorkAssignmentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// ID'ye göre iş atamasını getirir (basit versiyon)
        /// </summary>
        public async Task<WorkAssignment?> GetByIdAsync(int id)
        {
            return await _context.WorkAssignments
                .FirstOrDefaultAsync(wa => wa.Id == id);
        }

        /// <summary>
        /// ID'ye göre iş atamasını tüm ilişkili verilerle birlikte getirir
        /// </summary>
        public async Task<WorkAssignment?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.WorkAssignments
                .Include(wa => wa.ServiceRequest)
                    .ThenInclude(sr => sr.Customer)
                .Include(wa => wa.Technician)
                .FirstOrDefaultAsync(wa => wa.Id == id);
        }

        /// <summary>
        /// Tüm iş atamalarını servis talebi ve teknisyen bilgileriyle birlikte getirir
        /// </summary>
        public async Task<List<WorkAssignment>> GetAllAsync()
        {
            return await _context.WorkAssignments
                .Include(wa => wa.ServiceRequest)
                .Include(wa => wa.Technician)
                .OrderByDescending(wa => wa.AssignedDate)
                .ToListAsync();
        }

        /// <summary>
        /// Yeni bir iş ataması oluşturur
        /// </summary>
        public async Task<WorkAssignment> CreateAsync(WorkAssignment workAssignment)
        {
            try
            {
                _context.WorkAssignments.Add(workAssignment);
                await _context.SaveChangesAsync();
                return workAssignment;
            }
            catch (Exception ex)
            {
                throw new Exception($"İş ataması oluşturulurken hata: {ex.Message}", ex);
            }
        }

        public async Task<WorkAssignment> UpdateAsync(WorkAssignment workAssignment)
        {
            try
            {
                _context.WorkAssignments.Update(workAssignment);
                await _context.SaveChangesAsync();
                return workAssignment;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating work assignment: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var workAssignment = await GetByIdAsync(id);
                if (workAssignment == null)
                    return false;

                _context.WorkAssignments.Remove(workAssignment);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting work assignment: {ex.Message}", ex);
            }
        }

        public async Task<List<WorkAssignment>> GetByServiceRequestIdAsync(int serviceRequestId)
        {
            return await _context.WorkAssignments
                .Include(wa => wa.Technician)
                .Where(wa => wa.ServiceRequestId == serviceRequestId)
                .OrderByDescending(wa => wa.AssignedDate)
                .ToListAsync();
        }

        public async Task<List<WorkAssignment>> GetByTechnicianIdAsync(int technicianId)
        {
            return await _context.WorkAssignments
                .Include(wa => wa.ServiceRequest)
                    .ThenInclude(sr => sr.Customer)
                .Where(wa => wa.TechnicianId == technicianId)
                .OrderByDescending(wa => wa.AssignedDate)
                .ToListAsync();
        }

        public async Task<List<WorkAssignment>> GetByStatusAsync(WorkAssignmentStatus status)
        {
            return await _context.WorkAssignments
                .Include(wa => wa.ServiceRequest)
                .Include(wa => wa.Technician)
                .Where(wa => wa.Status == status)
                .OrderByDescending(wa => wa.AssignedDate)
                .ToListAsync();
        }

        public async Task<List<WorkAssignment>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.WorkAssignments
                .Include(wa => wa.ServiceRequest)
                .Include(wa => wa.Technician)
                .Where(wa => wa.AssignedDate >= startDate && wa.AssignedDate <= endDate)
                .OrderByDescending(wa => wa.AssignedDate)
                .ToListAsync();
        }

        public async Task<List<WorkAssignment>> GetActiveTechnicianAssignmentsAsync(int technicianId)
        {
            return await _context.WorkAssignments
                .Include(wa => wa.ServiceRequest)
                .Where(wa => wa.TechnicianId == technicianId && 
                           (wa.Status == WorkAssignmentStatus.Assigned || wa.Status == WorkAssignmentStatus.InProgress))
                .OrderBy(wa => wa.ScheduledDate ?? wa.AssignedDate)
                .ToListAsync();
        }

        public async Task<int> GetTechnicianActiveAssignmentCountAsync(int technicianId)
        {
            return await _context.WorkAssignments
                .CountAsync(wa => wa.TechnicianId == technicianId && 
                                (wa.Status == WorkAssignmentStatus.Assigned || wa.Status == WorkAssignmentStatus.InProgress));
        }

        public async Task<List<WorkAssignment>> GetTechnicianAssignmentsByDateAsync(int technicianId, DateTime date)
        {
            var startOfDay = date.Date;
            var endOfDay = startOfDay.AddDays(1).AddTicks(-1);

            return await _context.WorkAssignments
                .Include(wa => wa.ServiceRequest)
                .Where(wa => wa.TechnicianId == technicianId &&
                           wa.ScheduledDate.HasValue &&
                           wa.ScheduledDate >= startOfDay &&
                           wa.ScheduledDate <= endOfDay)
                .OrderBy(wa => wa.ScheduledDate)
                .ToListAsync();
        }

        public async Task<List<WorkAssignment>> GetOverdueAssignmentsAsync()
        {
            var currentDate = DateTime.UtcNow;
            return await _context.WorkAssignments
                .Include(wa => wa.ServiceRequest)
                .Include(wa => wa.Technician)
                .Where(wa => wa.ScheduledDate.HasValue &&
                           wa.ScheduledDate < currentDate &&
                           wa.Status != WorkAssignmentStatus.Completed &&
                           wa.Status != WorkAssignmentStatus.Cancelled)
                .OrderBy(wa => wa.ScheduledDate)
                .ToListAsync();
        }

        public async Task<List<WorkAssignment>> GetScheduledAssignmentsAsync(DateTime date)
        {
            var startOfDay = date.Date;
            var endOfDay = startOfDay.AddDays(1).AddTicks(-1);

            return await _context.WorkAssignments
                .Include(wa => wa.ServiceRequest)
                .Include(wa => wa.Technician)
                .Where(wa => wa.ScheduledDate.HasValue &&
                           wa.ScheduledDate >= startOfDay &&
                           wa.ScheduledDate <= endOfDay)
                .OrderBy(wa => wa.ScheduledDate)
                .ToListAsync();
        }

        public async Task<List<WorkAssignment>> GetAssignmentsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.WorkAssignments
                .Include(wa => wa.ServiceRequest)
                .Include(wa => wa.Technician)
                .Where(wa => wa.ScheduledDate.HasValue &&
                           wa.ScheduledDate >= startDate &&
                           wa.ScheduledDate <= endDate)
                .OrderBy(wa => wa.ScheduledDate)
                .ToListAsync();
        }

        public async Task<bool> IsTechnicianAvailableAsync(int technicianId, DateTime scheduledDate)
        {
            var startOfDay = scheduledDate.Date;
            var endOfDay = startOfDay.AddDays(1).AddTicks(-1);

            var conflictingAssignments = await _context.WorkAssignments
                .CountAsync(wa => wa.TechnicianId == technicianId &&
                                wa.ScheduledDate.HasValue &&
                                wa.ScheduledDate >= startOfDay &&
                                wa.ScheduledDate <= endOfDay &&
                                wa.Status != WorkAssignmentStatus.Cancelled &&
                                wa.Status != WorkAssignmentStatus.Completed);

            var technician = await _context.Technicians.FindAsync(technicianId);
            if (technician == null) return false;

            // Varsayılan maksimum eş zamanlı atama sayısı: 5
            return conflictingAssignments < 5;
        }

        public async Task<int> GetTotalAssignmentCountAsync()
        {
            return await _context.WorkAssignments.CountAsync();
        }

        public async Task<int> GetAssignmentCountByStatusAsync(WorkAssignmentStatus status)
        {
            return await _context.WorkAssignments
                .CountAsync(wa => wa.Status == status);
        }

        public async Task<int> GetAssignmentCountByTechnicianAsync(int technicianId)
        {
            return await _context.WorkAssignments
                .CountAsync(wa => wa.TechnicianId == technicianId);
        }

        public async Task<double> GetAverageCompletionTimeAsync()
        {
            var completedAssignments = await _context.WorkAssignments
                .Where(wa => wa.Status == WorkAssignmentStatus.Completed &&
                           wa.StartedDate.HasValue &&
                           wa.CompletedDate.HasValue)
                .Select(wa => new { wa.StartedDate, wa.CompletedDate })
                .ToListAsync();

            if (!completedAssignments.Any())
                return 0;

            var totalHours = completedAssignments
                .Sum(wa => (wa.CompletedDate!.Value - wa.StartedDate!.Value).TotalHours);

            return totalHours / completedAssignments.Count;
        }

        public async Task<double> GetTechnicianAverageCompletionTimeAsync(int technicianId)
        {
            var completedAssignments = await _context.WorkAssignments
                .Where(wa => wa.TechnicianId == technicianId &&
                           wa.Status == WorkAssignmentStatus.Completed &&
                           wa.StartedDate.HasValue &&
                           wa.CompletedDate.HasValue)
                .Select(wa => new { wa.StartedDate, wa.CompletedDate })
                .ToListAsync();

            if (!completedAssignments.Any())
                return 0;

            var totalHours = completedAssignments
                .Sum(wa => (wa.CompletedDate!.Value - wa.StartedDate!.Value).TotalHours);

            return totalHours / completedAssignments.Count;
        }

        public async Task<List<WorkAssignment>> GetCompletedAssignmentsByTechnicianAsync(int technicianId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.WorkAssignments
                .Include(wa => wa.ServiceRequest)
                .Where(wa => wa.TechnicianId == technicianId && wa.Status == WorkAssignmentStatus.Completed);

            if (startDate.HasValue)
                query = query.Where(wa => wa.CompletedDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(wa => wa.CompletedDate <= endDate.Value);

            return await query
                .OrderByDescending(wa => wa.CompletedDate)
                .ToListAsync();
        }

        public async Task<Dictionary<int, int>> GetTechnicianWorkloadSummaryAsync()
        {
            return await _context.WorkAssignments
                .Where(wa => wa.Status == WorkAssignmentStatus.Assigned || wa.Status == WorkAssignmentStatus.InProgress)
                .GroupBy(wa => wa.TechnicianId)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        public async Task<List<WorkAssignment>> GetAssignmentsByPriorityAsync(Priority priority)
        {
            return await _context.WorkAssignments
                .Include(wa => wa.ServiceRequest)
                .Include(wa => wa.Technician)
                .Where(wa => wa.ServiceRequest.Priority == priority)
                .OrderByDescending(wa => wa.AssignedDate)
                .ToListAsync();
        }
    }
}