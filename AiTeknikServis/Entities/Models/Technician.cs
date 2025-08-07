using System.ComponentModel.DataAnnotations;

namespace AiTeknikServis.Entities.Models
{
    public class Technician : User
    {
        [MaxLength(500)]
        public string? Specializations { get; set; }
        
        [MaxLength(1000)]
        public string? Notes { get; set; }
        
        // Navigation Properties
        public virtual ICollection<WorkAssignment> Assignments { get; set; } = new List<WorkAssignment>();
        public virtual ICollection<ServiceRequest> AssignedRequests { get; set; } = new List<ServiceRequest>();
        
        public Technician()
        {
            Role = UserRole.Technician;
        }
        
        // Helper method to get specializations as list
        public List<string> GetSpecializationsList()
        {
            if (string.IsNullOrEmpty(Specializations))
                return new List<string>();
                
            return Specializations.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                 .Select(s => s.Trim())
                                 .ToList();
        }
        
        // Helper method to set specializations from list
        public void SetSpecializationsList(List<string> specializations)
        {
            Specializations = string.Join(",", specializations);
        }
    }
}