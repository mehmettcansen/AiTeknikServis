using AiTeknikServis.Entities.Models;

namespace AiTeknikServis.Entities.Dtos.User
{
    public class TechnicianResponseDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public UserRole Role { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public List<string> SpecializationList { get; set; } = new List<string>();
        public string? Notes { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
        public int ActiveAssignments { get; set; }
        public int CompletedAssignments { get; set; }
        public double AverageRating { get; set; }
    }
}