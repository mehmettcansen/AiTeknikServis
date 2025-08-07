using AiTeknikServis.Entities.Models;

namespace AiTeknikServis.Entities.Dtos.User
{
    public class CustomerResponseDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public UserRole Role { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string? Address { get; set; }
        public string? ContactPerson { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
        public int TotalServiceRequests { get; set; }
        public int ActiveServiceRequests { get; set; }
    }
}