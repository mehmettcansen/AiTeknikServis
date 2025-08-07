using System.ComponentModel.DataAnnotations;

namespace AiTeknikServis.Entities.Models
{
    public class WorkAssignment
    {
        public int Id { get; set; }
        
        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
        
        public DateTime? ScheduledDate { get; set; }
        
        public DateTime? StartedDate { get; set; }
        
        public DateTime? CompletedDate { get; set; }
        
        public WorkAssignmentStatus Status { get; set; } = WorkAssignmentStatus.Assigned;
        
        [MaxLength(1000)]
        public string? Notes { get; set; }
        
        [MaxLength(1000)]
        public string? CompletionNotes { get; set; }
        
        public int? EstimatedHours { get; set; }
        
        public int? ActualHours { get; set; }
        
        // Foreign Keys
        public int ServiceRequestId { get; set; }
        public int TechnicianId { get; set; }
        
        // Navigation Properties
        public virtual ServiceRequest ServiceRequest { get; set; } = null!;
        public virtual Technician Technician { get; set; } = null!;
    }
}