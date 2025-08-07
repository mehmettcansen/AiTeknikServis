using System.ComponentModel.DataAnnotations;
using AiTeknikServis.Entities.Models;

namespace AiTeknikServis.Entities.Dtos.ServiceRequest
{
    public class ServiceRequestUpdateDto
    {
        [Required(ErrorMessage = "ID alanı zorunludur")]
        public int Id { get; set; }

        [StringLength(200, ErrorMessage = "Başlık en fazla 200 karakter olabilir")]
        public string? Title { get; set; }

        [StringLength(2000, ErrorMessage = "Açıklama en fazla 2000 karakter olabilir")]
        public string? Description { get; set; }

        public ServiceCategory? Category { get; set; }

        public Priority? Priority { get; set; }

        public ServiceStatus? Status { get; set; }

        public int? AssignedTechnicianId { get; set; }

        public DateTime? ScheduledDate { get; set; }

        public DateTime? CompletedDate { get; set; }

        [StringLength(500, ErrorMessage = "Ürün bilgisi en fazla 500 karakter olabilir")]
        public string? ProductInfo { get; set; }

        [StringLength(100, ErrorMessage = "İletişim tercihi en fazla 100 karakter olabilir")]
        public string? ContactPreference { get; set; }

        [StringLength(1000, ErrorMessage = "Çözüm açıklaması en fazla 1000 karakter olabilir")]
        public string? Resolution { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Tahmini maliyet negatif olamaz")]
        public decimal? EstimatedCost { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Gerçek maliyet negatif olamaz")]
        public decimal? ActualCost { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Tahmini saat negatif olamaz")]
        public int? EstimatedHours { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Gerçek saat negatif olamaz")]
        public int? ActualHours { get; set; }
    }
}