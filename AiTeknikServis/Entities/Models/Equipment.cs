using System.ComponentModel.DataAnnotations;

namespace AiTeknikServis.Entities.Models
{
    public class Equipment
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string? Brand { get; set; }
        
        [MaxLength(100)]
        public string? Model { get; set; }
        
        [MaxLength(100)]
        public string? SerialNumber { get; set; }
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        public DateTime? PurchaseDate { get; set; }
        
        public DateTime? WarrantyExpireDate { get; set; }
        
        public decimal? PurchasePrice { get; set; }
        
        [MaxLength(50)]
        public string? Status { get; set; } = "Active";
        
        [MaxLength(500)]
        public string? Location { get; set; }
        
        // Foreign Key
        public int? CustomerId { get; set; }
        
        // Navigation Property
        public virtual Customer? Customer { get; set; }
    }
}