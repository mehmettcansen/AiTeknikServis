using System.ComponentModel.DataAnnotations;

namespace AiTeknikServis.Entities.Dtos.ServiceRequest
{
    public class ServiceRequestFileDto
    {
        /// <summary>
        /// Dosya ID'si
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Servis talebi ID'si
        /// </summary>
        public int ServiceRequestId { get; set; }

        /// <summary>
        /// Dosya adı (güvenli)
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Orijinal dosya adı
        /// </summary>
        [MaxLength(255)]
        public string? OriginalFileName { get; set; }

        /// <summary>
        /// Dosya yolu
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Dosya boyutu (byte)
        /// </summary>
        public long? FileSize { get; set; }

        /// <summary>
        /// İçerik türü (MIME type)
        /// </summary>
        [MaxLength(100)]
        public string? ContentType { get; set; }

        /// <summary>
        /// Yüklenme tarihi
        /// </summary>
        public DateTime UploadedDate { get; set; }

        /// <summary>
        /// Yükleyen kullanıcı ID'si
        /// </summary>
        public int? UploadedBy { get; set; }

        /// <summary>
        /// Formatlanmış dosya boyutu (örn: "2.5 MB")
        /// </summary>
        public string FormattedFileSize 
        { 
            get 
            {
                if (!FileSize.HasValue || FileSize.Value == 0)
                    return "0 Bytes";

                const int scale = 1024;
                string[] orders = { "GB", "MB", "KB", "Bytes" };
                long max = (long)Math.Pow(scale, orders.Length - 1);

                foreach (string order in orders)
                {
                    if (FileSize.Value > max)
                        return string.Format("{0:##.##} {1}", decimal.Divide(FileSize.Value, max), order);

                    max /= scale;
                }
                return "0 Bytes";
            } 
        }

        /// <summary>
        /// Dosya uzantısı
        /// </summary>
        public string FileExtension 
        { 
            get 
            {
                return Path.GetExtension(FileName)?.ToLowerInvariant() ?? "";
            } 
        }

        /// <summary>
        /// Dosyanın görüntü olup olmadığı
        /// </summary>
        public bool IsImage 
        { 
            get 
            {
                var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
                return imageExtensions.Contains(FileExtension);
            } 
        }

        /// <summary>
        /// İndirme URL'si (relative)
        /// </summary>
        public string DownloadUrl => $"/api/files/download/{Id}";
    }

    /// <summary>
    /// Dosya yükleme için DTO
    /// </summary>
    public class FileUploadDto
    {
        /// <summary>
        /// Servis talebi ID'si
        /// </summary>
        [Required]
        public int ServiceRequestId { get; set; }

        /// <summary>
        /// Yüklenecek dosyalar
        /// </summary>
        [Required]
        public IFormFileCollection Files { get; set; } = null!;

        /// <summary>
        /// Açıklama (opsiyonel)
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }
    }
} 