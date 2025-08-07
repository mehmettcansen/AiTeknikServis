using System.ComponentModel.DataAnnotations;
using AiTeknikServis.Entities.Models;

namespace AiTeknikServis.Entities.Dtos.User
{
    public class CustomerUpdateDto
    {
        [StringLength(50, ErrorMessage = "Ad en fazla 50 karakter olabilir.")]
        public string? FirstName { get; set; }

        [StringLength(50, ErrorMessage = "Soyad en fazla 50 karakter olabilir.")]
        public string? LastName { get; set; }

        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [StringLength(100, ErrorMessage = "E-posta en fazla 100 karakter olabilir.")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
        [StringLength(20, ErrorMessage = "Telefon en fazla 20 karakter olabilir.")]
        public string? Phone { get; set; }

        [StringLength(200, ErrorMessage = "Şirket adı en fazla 200 karakter olabilir.")]
        public string? CompanyName { get; set; }

        [StringLength(500, ErrorMessage = "Adres en fazla 500 karakter olabilir.")]
        public string? Address { get; set; }

        [StringLength(100, ErrorMessage = "İletişim kişisi en fazla 100 karakter olabilir.")]
        public string? ContactPerson { get; set; }

        public ContactPreference? ContactPreference { get; set; }

        public bool? IsActive { get; set; }
    }
}