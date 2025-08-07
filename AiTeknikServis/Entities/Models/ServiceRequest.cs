using System.ComponentModel.DataAnnotations;

namespace AiTeknikServis.Entities.Models
{
    public class ServiceRequest
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;
        
        public ServiceCategory Category { get; set; }
        
        public Priority Priority { get; set; }
        
        public ServiceStatus Status { get; set; } = ServiceStatus.Pending;
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        
        public DateTime? CompletedDate { get; set; }
        
        public DateTime? ScheduledDate { get; set; }
        
        [MaxLength(500)]
        public string? ProductInfo { get; set; }

        
        [Required]
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? Resolution { get; set; }
        
        public decimal? EstimatedCost { get; set; }
        
        public decimal? ActualCost { get; set; }
        
        public int? EstimatedHours { get; set; }
        
        public int? ActualHours { get; set; }
        
        [MaxLength(5000)]
        public string? AiReportAnalysis { get; set; }
        
        public DateTime? AiAnalysisDate { get; set; }
        
        // Foreign Keys
        public int CustomerId { get; set; }
        public int? AssignedTechnicianId { get; set; }
        
        // Navigation Properties
        public virtual Customer Customer { get; set; } = null!;
        public virtual Technician? AssignedTechnician { get; set; }
        public virtual ICollection<ServiceRequestFile> Files { get; set; } = new List<ServiceRequestFile>();
        public virtual ICollection<AiPrediction> AiPredictions { get; set; } = new List<AiPrediction>();
        public virtual ICollection<WorkAssignment> WorkAssignments { get; set; } = new List<WorkAssignment>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}