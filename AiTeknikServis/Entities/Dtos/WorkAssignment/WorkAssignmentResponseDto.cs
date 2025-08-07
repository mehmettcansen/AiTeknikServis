using AiTeknikServis.Entities.Models;

namespace AiTeknikServis.Entities.Dtos.WorkAssignment
{
    public class WorkAssignmentResponseDto
    {
        public int Id { get; set; }
        public DateTime AssignedDate { get; set; }
        public DateTime? ScheduledDate { get; set; }
        public DateTime? StartedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public WorkAssignmentStatus Status { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string? CompletionNotes { get; set; }
        public int? EstimatedHours { get; set; }
        public int? ActualHours { get; set; }

        // Service Request Information
        public int ServiceRequestId { get; set; }
        public string ServiceRequestTitle { get; set; } = string.Empty;
        public Priority ServiceRequestPriority { get; set; }
        
        // Customer Information
        public string CustomerCompanyName { get; set; } = string.Empty;

        // Technician Information
        public int TechnicianId { get; set; }
        public string TechnicianName { get; set; } = string.Empty;
    }
}