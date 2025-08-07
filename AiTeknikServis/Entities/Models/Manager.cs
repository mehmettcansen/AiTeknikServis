using System.ComponentModel.DataAnnotations;

namespace AiTeknikServis.Entities.Models
{
    public class Manager : User
    {
        [MaxLength(100)]
        public string? Title { get; set; }
        
        public int? SupervisorId { get; set; }
        
        // Navigation Properties
        public virtual Manager? Supervisor { get; set; }
        public virtual ICollection<Manager> Subordinates { get; set; } = new List<Manager>();
        
        public Manager()
        {
            Role = UserRole.Manager;
        }
    }
}