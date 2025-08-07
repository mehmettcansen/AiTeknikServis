using AiTeknikServis.Entities.Models;
using AiTeknikServis.Entities.Dtos.WorkAssignment;

namespace AiTeknikServis.Entities.Dtos.ServiceRequest
{
    public class ServiceRequestResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ServiceCategory Category { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public Priority Priority { get; set; }
        public string PriorityName { get; set; } = string.Empty;
        public ServiceStatus Status { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public DateTime? ScheduledDate { get; set; }
        public string? ProductInfo { get; set; }
        public string? Phone { get; set; }
        public ContactPreference ContactPreference { get; set; }
        public string? Resolution { get; set; }
        public decimal? EstimatedCost { get; set; }
        public decimal? ActualCost { get; set; }
        public int? EstimatedHours { get; set; }
        public int? ActualHours { get; set; }

        // Customer Information
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;

        // Technician Information
        public int? AssignedTechnicianId { get; set; }
        public string? TechnicianName { get; set; }

        // Related Data
        public List<ServiceRequestFileDto> Files { get; set; } = new List<ServiceRequestFileDto>();
        public List<AiPredictionResponseDto> AiPredictions { get; set; } = new List<AiPredictionResponseDto>();
        public List<WorkAssignmentResponseDto> WorkAssignments { get; set; } = new List<WorkAssignmentResponseDto>();
    }

    public class AiPredictionResponseDto
    {
        public int Id { get; set; }
        public ServiceCategory PredictedCategory { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public Priority PredictedPriority { get; set; }
        public string PriorityName { get; set; } = string.Empty;
        public string? Recommendation { get; set; }
        public float ConfidenceScore { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? SuggestedTechnician { get; set; }
    }
}
