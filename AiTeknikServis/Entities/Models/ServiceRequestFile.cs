using System.ComponentModel.DataAnnotations;

namespace AiTeknikServis.Entities.Models
{
    public class ServiceRequestFile
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;
        
        [MaxLength(255)]
        public string? OriginalFileName { get; set; }
        
        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string? ContentType { get; set; }
        
        public long FileSize { get; set; }
        
        public DateTime UploadedDate { get; set; } = DateTime.UtcNow;
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        // Foreign Key
        public int ServiceRequestId { get; set; }
        
        // Navigation Property
        public virtual ServiceRequest ServiceRequest { get; set; } = null!;
    }
}