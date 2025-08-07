using System.ComponentModel.DataAnnotations;

namespace AiTeknikServis.Entities.Models
{
    public class Report
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [MaxLength(50)]
        public string ReportType { get; set; } = string.Empty; // Performance, Customer Satisfaction, etc.
        
        public DateTime GeneratedDate { get; set; } = DateTime.UtcNow;
        
        public DateTime? StartDate { get; set; }
        
        public DateTime? EndDate { get; set; }
        
        [MaxLength(5000)]
        public string? Content { get; set; }
        
        [MaxLength(2000)]
        public string? Summary { get; set; }
        
        public string? JsonData { get; set; } // For storing structured report data
        
        // Foreign Key
        public int? GeneratedByUserId { get; set; }
        
        // Navigation Property
        public virtual User? GeneratedByUser { get; set; }
    }
}