using System.ComponentModel.DataAnnotations;

namespace AiTeknikServis.Entities.Models
{
    public class Admin : User
    {
        [MaxLength(100)]
        public string? AccessLevel { get; set; }
        
        public DateTime? LastLoginDate { get; set; }
        
        [MaxLength(500)]
        public string? Permissions { get; set; }
        
        public Admin()
        {
            Role = UserRole.Admin;
        }
        
        // Helper method to get permissions as list
        public List<string> GetPermissionsList()
        {
            if (string.IsNullOrEmpty(Permissions))
                return new List<string>();
                
            return Permissions.Split(',', StringSplitOptions.RemoveEmptyEntries)
                             .Select(p => p.Trim())
                             .ToList();
        }
        
        // Helper method to set permissions from list
        public void SetPermissionsList(List<string> permissions)
        {
            Permissions = string.Join(",", permissions);
        }
    }
}