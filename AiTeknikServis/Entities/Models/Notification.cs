using System.ComponentModel.DataAnnotations;

namespace AiTeknikServis.Entities.Models
{
    public class Notification
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(1000)]
        public string Message { get; set; } = string.Empty;
        
        public NotificationType Type { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        
        public bool IsRead { get; set; } = false;
        
        public bool IsEmailSent { get; set; } = false;
        
        public bool IsSmsSent { get; set; } = false;
        
        [MaxLength(100)]
        public string? RecipientEmail { get; set; }
        
        [MaxLength(20)]
        public string? RecipientPhone { get; set; }
        
        // Foreign Keys
        public int? ServiceRequestId { get; set; }
        public int? UserId { get; set; }
        
        // Navigation Properties
        public virtual ServiceRequest? ServiceRequest { get; set; }
        public virtual User? User { get; set; }
    }
}