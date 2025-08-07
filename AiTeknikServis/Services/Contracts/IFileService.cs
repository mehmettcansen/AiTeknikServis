using Microsoft.AspNetCore.Http;
using AiTeknikServis.Entities.Models;

namespace AiTeknikServis.Services.Contracts
{
    public interface IFileService
    {
        /// <summary>
        /// Servis talebi için dosya yükler
        /// </summary>
        /// <param name="serviceRequestId">Servis talebi ID'si</param>
        /// <param name="files">Yüklenecek dosyalar</param>
        /// <param name="uploadedBy">Yükleyen kullanıcı ID'si</param>
        /// <returns>Yüklenen dosya bilgileri</returns>
        Task<List<ServiceRequestFile>> UploadFilesAsync(int serviceRequestId, IFormFileCollection files, int uploadedBy);

        /// <summary>
        /// Tek dosya yükler
        /// </summary>
        /// <param name="serviceRequestId">Servis talebi ID'si</param>
        /// <param name="file">Yüklenecek dosya</param>
        /// <param name="uploadedBy">Yükleyen kullanıcı ID'si</param>
        /// <returns>Yüklenen dosya bilgisi</returns>
        Task<ServiceRequestFile> UploadFileAsync(int serviceRequestId, IFormFile file, int uploadedBy);

        /// <summary>
        /// Dosyayı indirir
        /// </summary>
        /// <param name="fileId">Dosya ID'si</param>
        /// <returns>Dosya stream'i ve bilgileri</returns>
        Task<(Stream stream, string fileName, string contentType)> DownloadFileAsync(int fileId);

        /// <summary>
        /// Dosyayı siler
        /// </summary>
        /// <param name="fileId">Dosya ID'si</param>
        /// <returns>Silme işlemi başarılı mı?</returns>
        Task<bool> DeleteFileAsync(int fileId);

        /// <summary>
        /// Servis talebine ait dosyaları getirir
        /// </summary>
        /// <param name="serviceRequestId">Servis talebi ID'si</param>
        /// <returns>Dosya listesi</returns>
        Task<List<ServiceRequestFile>> GetFilesByServiceRequestIdAsync(int serviceRequestId);

        /// <summary>
        /// Dosya ID'sine göre dosya bilgisini getirir
        /// </summary>
        /// <param name="fileId">Dosya ID'si</param>
        /// <returns>Dosya bilgisi</returns>
        Task<ServiceRequestFile?> GetFileByIdAsync(int fileId);

        /// <summary>
        /// Dosya validasyonu yapar
        /// </summary>
        /// <param name="file">Validasyon yapılacak dosya</param>
        /// <returns>Validasyon sonucu</returns>
        Task<FileValidationResult> ValidateFileAsync(IFormFile file);

        /// <summary>
        /// Dosya boyutu limitini kontrol eder
        /// </summary>
        /// <param name="fileSize">Dosya boyutu (byte)</param>
        /// <returns>Boyut uygun mu?</returns>
        bool IsFileSizeValid(long fileSize);

        /// <summary>
        /// Dosya uzantısını kontrol eder
        /// </summary>
        /// <param name="fileName">Dosya adı</param>
        /// <returns>Uzantı uygun mu?</returns>
        bool IsFileExtensionValid(string fileName);

        /// <summary>
        /// Dosya içeriğini kontrol eder (MIME type validation)
        /// </summary>
        /// <param name="file">Kontrol edilecek dosya</param>
        /// <returns>İçerik uygun mu?</returns>
        Task<bool> IsFileContentValidAsync(IFormFile file);

        /// <summary>
        /// Güvenli dosya adı oluşturur
        /// </summary>
        /// <param name="originalFileName">Orijinal dosya adı</param>
        /// <returns>Güvenli dosya adı</returns>
        string GenerateSafeFileName(string originalFileName);

        /// <summary>
        /// Dosya storage yolunu oluşturur
        /// </summary>
        /// <param name="serviceRequestId">Servis talebi ID'si</param>
        /// <param name="fileName">Dosya adı</param>
        /// <returns>Dosya yolu</returns>
        string GenerateFilePath(int serviceRequestId, string fileName);

        /// <summary>
        /// Dosya istatistiklerini getirir
        /// </summary>
        /// <returns>Dosya istatistikleri</returns>
        Task<FileStatistics> GetFileStatisticsAsync();

        /// <summary>
        /// Eski dosyaları temizler
        /// </summary>
        /// <param name="cutoffDate">Kesim tarihi</param>
        /// <returns>Silinen dosya sayısı</returns>
        Task<int> CleanupOldFilesAsync(DateTime cutoffDate);

        /// <summary>
        /// Dosya boyutu formatlar (KB, MB, GB)
        /// </summary>
        /// <param name="bytes">Byte cinsinden boyut</param>
        /// <returns>Formatlanmış boyut</returns>
        string FormatFileSize(long bytes);
    }

    /// <summary>
    /// Dosya validasyon sonucunu içeren model
    /// </summary>
    public class FileValidationResult
    {
        /// <summary>
        /// Validasyon başarılı mı?
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Hata mesajları
        /// </summary>
        public List<string> ErrorMessages { get; set; } = new List<string>();

        /// <summary>
        /// Uyarı mesajları
        /// </summary>
        public List<string> WarningMessages { get; set; } = new List<string>();

        /// <summary>
        /// Dosya boyutu (byte)
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// Dosya uzantısı
        /// </summary>
        public string FileExtension { get; set; } = string.Empty;

        /// <summary>
        /// MIME type
        /// </summary>
        public string ContentType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Dosya istatistiklerini içeren model
    /// </summary>
    public class FileStatistics
    {
        /// <summary>
        /// Toplam dosya sayısı
        /// </summary>
        public int TotalFiles { get; set; }

        /// <summary>
        /// Toplam dosya boyutu (byte)
        /// </summary>
        public long TotalFileSize { get; set; }

        /// <summary>
        /// Ortalama dosya boyutu (byte)
        /// </summary>
        public double AverageFileSize { get; set; }

        /// <summary>
        /// En büyük dosya boyutu (byte)
        /// </summary>
        public long LargestFileSize { get; set; }

        /// <summary>
        /// En küçük dosya boyutu (byte)
        /// </summary>
        public long SmallestFileSize { get; set; }

        /// <summary>
        /// Uzantı bazında dağılım
        /// </summary>
        public Dictionary<string, int> ExtensionDistribution { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Aylık yükleme dağılımı
        /// </summary>
        public Dictionary<string, int> MonthlyUploadDistribution { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// İstatistik hesaplama tarihi
        /// </summary>
        public DateTime GeneratedDate { get; set; } = DateTime.UtcNow;
    }
} 