using AiTeknikServis.Entities.Models;

namespace AiTeknikServis.Entities.Dtos.Report
{
    public class ServiceRequestReportDto
    {
        public int ServiceRequestId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerPhone { get; set; }
        public string TechnicianName { get; set; } = string.Empty;
        public ServiceCategory Category { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public Priority Priority { get; set; }
        public string PriorityName { get; set; } = string.Empty;
        public ServiceStatus Status { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public int CompletionDays { get; set; }
        public string? ProductInfo { get; set; }
        public string? Resolution { get; set; }
        public decimal? EstimatedCost { get; set; }
        public decimal? ActualCost { get; set; }
        public int? EstimatedHours { get; set; }
        public int? ActualHours { get; set; }
        
        // Süreç hareketleri
        public List<ProcessMovementDto> ProcessMovements { get; set; } = new List<ProcessMovementDto>();
        
        // AI Analizi
        public List<AiAnalysisDto> AiAnalyses { get; set; } = new List<AiAnalysisDto>();
        
        // AI Rapor Yorumu
        public string? AiReportSummary { get; set; }
    }

    public class ProcessMovementDto
    {
        public DateTime Date { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? PerformedBy { get; set; }
    }

    public class AiAnalysisDto
    {
        public string Recommendation { get; set; } = string.Empty;
        public float ConfidenceScore { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? SuggestedTechnician { get; set; }
    }
}