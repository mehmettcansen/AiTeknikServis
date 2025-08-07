using System.ComponentModel.DataAnnotations;

namespace AiTeknikServis.Entities.Models
{
    public class Customer : User
    {
        [MaxLength(200)]
        public string? CompanyName { get; set; }
        
        [MaxLength(500)]
        public string? Address { get; set; }
        
        [MaxLength(100)]
        public string? ContactPerson { get; set; }
        
        // Navigation Properties
        public virtual ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();
        
        public Customer()
        {
            Role = UserRole.Customer;
        }
    }
}