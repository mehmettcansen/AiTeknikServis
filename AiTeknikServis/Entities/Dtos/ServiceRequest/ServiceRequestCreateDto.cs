using System.ComponentModel.DataAnnotations;
using AiTeknikServis.Entities.Models;

namespace AiTeknikServis.Entities.Dtos.ServiceRequest
{
    public class ServiceRequestCreateDto
    {
        [Required(ErrorMessage = "Başlık alanı zorunludur")]
        [StringLength(200, ErrorMessage = "Başlık en fazla 200 karakter olabilir")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Açıklama alanı zorunludur")]
        [StringLength(2000, ErrorMessage = "Açıklama en fazla 2000 karakter olabilir")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Müşteri ID zorunludur")]
        public int CustomerId { get; set; }

        [StringLength(500, ErrorMessage = "Ürün bilgisi en fazla 500 karakter olabilir")]
        public string? ProductInfo { get; set; }

        [Required(ErrorMessage = "Telefon numarası zorunludur")]
        [StringLength(20, ErrorMessage = "Telefon numarası en fazla 20 karakter olabilir")]
        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
        public string Phone { get; set; } = string.Empty;

        public List<IFormFile>? Files { get; set; }

        public DateTime? ScheduledDate { get; set; }


        [Range(0, int.MaxValue, ErrorMessage = "Tahmini saat negatif olamaz")]
        public int? EstimatedHours { get; set; }
    }
}