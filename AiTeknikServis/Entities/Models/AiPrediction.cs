using System.ComponentModel.DataAnnotations;

namespace AiTeknikServis.Entities.Models
{
    public class AiPrediction
    {
        public int Id { get; set; }
        
        public ServiceCategory PredictedCategory { get; set; }
        
        public Priority PredictedPriority { get; set; }
        
        [MaxLength(2000)]
        public string? Recommendation { get; set; }
        
        [MaxLength(200)]
        public string? SuggestedTechnician { get; set; }
        
        public float ConfidenceScore { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        
        [MaxLength(2000)]
        public string? InputText { get; set; }
        
        [MaxLength(5000)]
        public string? RawAiResponse { get; set; }
        
        public bool IsAccurate { get; set; } = true; // For feedback tracking
        
        // Foreign Key
        public int ServiceRequestId { get; set; }
        
        // Navigation Property
        public virtual ServiceRequest ServiceRequest { get; set; } = null!;
    }
}