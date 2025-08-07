using System.ComponentModel.DataAnnotations;
using AiTeknikServis.Entities.Models;

namespace AiTeknikServis.Entities.Dtos.WorkAssignment
{
    public class WorkAssignmentCreateDto
    {
        [Required(ErrorMessage = "Servis talebi ID zorunludur")]
        public int ServiceRequestId { get; set; }

        [Required(ErrorMessage = "Teknisyen ID zorunludur")]
        public int TechnicianId { get; set; }

        public DateTime? ScheduledDate { get; set; }

        [StringLength(1000, ErrorMessage = "Notlar en fazla 1000 karakter olabilir")]
        public string? Notes { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Tahmini saat negatif olamaz")]
        public int? EstimatedHours { get; set; }

        public WorkAssignmentStatus Status { get; set; } = WorkAssignmentStatus.Assigned;
    }
}