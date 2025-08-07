using Microsoft.AspNetCore.Http;
using AiTeknikServis.Entities.Models;
using AiTeknikServis.Repositories.Contracts;
using AiTeknikServis.Services.Contracts;
using System.Text.RegularExpressions;

namespace AiTeknikServis.Services
{
    public class FileService : IFileService
    {
        private readonly IServiceRequestFileRepository _fileRepository;
        private readonly IServiceRequestRepository _serviceRequestRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FileService> _logger;

        // Güvenlik ayarları
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".txt", ".xlsx", ".ppt", ".pptx", ".zip", ".rar" };
        private readonly string[] _allowedMimeTypes = { 
            "image/jpeg", "image/png", "image/gif", "application/pdf", 
            "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "text/plain", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/vnd.ms-powerpoint", "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            "application/zip", "application/x-rar-compressed"
        };
        private readonly long _maxFileSize = 10 * 1024 * 1024; // 10 MB
        private readonly int _maxFilesPerRequest = 5;

        /// <summary>
        /// FileService constructor - Gerekli servisleri enjekte eder
        /// </summary>
        public FileService(
            IServiceRequestFileRepository fileRepository,
            IServiceRequestRepository serviceRequestRepository,
            IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration,
            ILogger<FileService> logger)
        {
            _fileRepository = fileRepository;
            _serviceRequestRepository = serviceRequestRepository;
            _webHostEnvironment = webHostEnvironment;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Servis talebi için dosya yükler
        /// </summary>
        public async Task<List<ServiceRequestFile>> UploadFilesAsync(int serviceRequestId, IFormFileCollection files, int uploadedBy)
        {
            try
            {
                _logger.LogInformation("Dosya yükleme başlatılıyor: ServiceRequestID {ServiceRequestId}, Dosya sayısı: {FileCount}", 
                    serviceRequestId, files.Count);

                // Temel validasyonlar
                if (files.Count > _maxFilesPerRequest)
                {
                    throw new InvalidOperationException($"Maksimum {_maxFilesPerRequest} dosya yükleyebilirsiniz");
                }

                // Servis talebi kontrolü
                var serviceRequest = await _serviceRequestRepository.GetByIdAsync(serviceRequestId);
                if (serviceRequest == null)
                {
                    throw new NotFoundException($"ID {serviceRequestId} ile servis talebi bulunamadı");
                }

                var uploadedFiles = new List<ServiceRequestFile>();

                foreach (var file in files)
                {
                    var uploadedFile = await UploadFileAsync(serviceRequestId, file, uploadedBy);
                    uploadedFiles.Add(uploadedFile);
                }

                _logger.LogInformation("Dosya yükleme tamamlandı: {UploadedCount} dosya başarıyla yüklendi", uploadedFiles.Count);
                return uploadedFiles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dosya yükleme işlemi başarısız: ServiceRequestID {ServiceRequestId}", serviceRequestId);
                throw new ServiceException("Dosyalar yüklenemedi", ex);
            }
        }

        /// <summary>
        /// Tek dosya yükler
        /// </summary>
        public async Task<ServiceRequestFile> UploadFileAsync(int serviceRequestId, IFormFile file, int uploadedBy)
        {
            try
            {
                _logger.LogInformation("Tek dosya yükleme başlatılıyor: {FileName}", file.FileName);

                // Dosya validasyonu
                var validationResult = await ValidateFileAsync(file);
                if (!validationResult.IsValid)
                {
                    throw new InvalidOperationException($"Dosya validasyonu başarısız: {string.Join(", ", validationResult.ErrorMessages)}");
                }

                // Güvenli dosya adı oluştur
                var safeFileName = GenerateSafeFileName(file.FileName);
                var filePath = GenerateFilePath(serviceRequestId, safeFileName);
                var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, filePath);

                // Klasörü oluştur
                var directory = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory!);
                }

                // Dosyayı diske kaydet
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Veritabanına kaydet
                var serviceRequestFile = new ServiceRequestFile
                {
                    ServiceRequestId = serviceRequestId,
                    FileName = safeFileName,
                    OriginalFileName = file.FileName,
                    FilePath = filePath,
                    FileSize = file.Length,
                    ContentType = file.ContentType,
                    UploadedDate = DateTime.UtcNow
                };

                var createdFile = await _fileRepository.CreateAsync(serviceRequestFile);

                _logger.LogInformation("Dosya başarıyla yüklendi: {FileName}, Boyut: {FileSize} bytes", 
                    safeFileName, file.Length);

                return createdFile;
            }
            catch (Exception ex) when (!(ex is InvalidOperationException || ex is NotFoundException))
            {
                _logger.LogError(ex, "Tek dosya yükleme başarısız: {FileName}", file.FileName);
                throw new ServiceException("Dosya yüklenemedi", ex);
            }
        }

        /// <summary>
        /// Dosyayı indirir
        /// </summary>
        public async Task<(Stream stream, string fileName, string contentType)> DownloadFileAsync(int fileId)
        {
            try
            {
                var file = await _fileRepository.GetByIdAsync(fileId);
                if (file == null)
                {
                    throw new NotFoundException($"ID {fileId} ile dosya bulunamadı");
                }

                var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, file.FilePath);
                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException($"Dosya bulunamadı: {file.FileName}");
                }

                var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
                
                _logger.LogInformation("Dosya indirme başlatıldı: {FileName}", file.FileName);
                
                return (stream, file.OriginalFileName, file.ContentType);
            }
            catch (Exception ex) when (!(ex is NotFoundException || ex is FileNotFoundException))
            {
                _logger.LogError(ex, "Dosya indirme başarısız: FileID {FileId}", fileId);
                throw new ServiceException("Dosya indirilemedi", ex);
            }
        }

        /// <summary>
        /// Dosyayı siler
        /// </summary>
        public async Task<bool> DeleteFileAsync(int fileId)
        {
            try
            {
                _logger.LogInformation("Dosya silme başlatılıyor: FileID {FileId}", fileId);

                var file = await _fileRepository.GetByIdAsync(fileId);
                if (file == null)
                {
                    _logger.LogWarning("Silinecek dosya bulunamadı: FileID {FileId}", fileId);
                    return false;
                }

                // Fiziksel dosyayı sil
                var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, file.FilePath);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    _logger.LogDebug("Fiziksel dosya silindi: {FilePath}", fullPath);
                }

                // Veritabanından sil
                var result = await _fileRepository.DeleteAsync(fileId);
                
                if (result)
                {
                    _logger.LogInformation("Dosya başarıyla silindi: {FileName}", file.FileName);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dosya silme başarısız: FileID {FileId}", fileId);
                throw new ServiceException("Dosya silinemedi", ex);
            }
        }

        /// <summary>
        /// Servis talebine ait dosyaları getirir
        /// </summary>
        public async Task<List<ServiceRequestFile>> GetFilesByServiceRequestIdAsync(int serviceRequestId)
        {
            try
            {
                return await _fileRepository.GetByServiceRequestIdAsync(serviceRequestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Servis talebi dosyaları getirilemedi: ServiceRequestID {ServiceRequestId}", serviceRequestId);
                throw new ServiceException("Servis talebi dosyaları getirilemedi", ex);
            }
        }

        /// <summary>
        /// Dosya ID'sine göre dosya bilgisini getirir
        /// </summary>
        public async Task<ServiceRequestFile?> GetFileByIdAsync(int fileId)
        {
            try
            {
                return await _fileRepository.GetByIdAsync(fileId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dosya bilgisi getirilemedi: FileID {FileId}", fileId);
                throw new ServiceException("Dosya bilgisi getirilemedi", ex);
            }
        }

        /// <summary>
        /// Dosya validasyonu yapar
        /// </summary>
        public async Task<FileValidationResult> ValidateFileAsync(IFormFile file)
        {
            var result = new FileValidationResult
            {
                FileSize = file.Length,
                FileExtension = Path.GetExtension(file.FileName).ToLowerInvariant(),
                ContentType = file.ContentType
            };

            // Dosya boş kontrolü
            if (file.Length == 0)
            {
                result.ErrorMessages.Add("Dosya boş olamaz");
            }

            // Boyut kontrolü
            if (!IsFileSizeValid(file.Length))
            {
                result.ErrorMessages.Add($"Dosya boyutu maksimum {FormatFileSize(_maxFileSize)} olabilir");
            }

            // Uzantı kontrolü
            if (!IsFileExtensionValid(file.FileName))
            {
                result.ErrorMessages.Add($"Dosya uzantısı desteklenmiyor. İzin verilen uzantılar: {string.Join(", ", _allowedExtensions)}");
            }

            // İçerik kontrolü
            if (!await IsFileContentValidAsync(file))
            {
                result.ErrorMessages.Add("Dosya içeriği güvenli değil veya MIME type uyumsuz");
            }

            // Dosya adı kontrolü
            if (string.IsNullOrWhiteSpace(file.FileName) || file.FileName.Length > 255)
            {
                result.ErrorMessages.Add("Dosya adı geçersiz veya çok uzun");
            }

            // Güvenlik kontrolleri
            if (file.FileName.Contains("..") || file.FileName.Contains("/") || file.FileName.Contains("\\"))
            {
                result.ErrorMessages.Add("Dosya adı güvenlik açısından uygun değil");
            }

            result.IsValid = !result.ErrorMessages.Any();
            return result;
        }

        /// <summary>
        /// Dosya boyutu limitini kontrol eder
        /// </summary>
        public bool IsFileSizeValid(long fileSize)
        {
            return fileSize > 0 && fileSize <= _maxFileSize;
        }

        /// <summary>
        /// Dosya uzantısını kontrol eder
        /// </summary>
        public bool IsFileExtensionValid(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return _allowedExtensions.Contains(extension);
        }

        /// <summary>
        /// Dosya içeriğini kontrol eder (MIME type validation)
        /// </summary>
        public async Task<bool> IsFileContentValidAsync(IFormFile file)
        {
            try
            {
                // MIME type kontrolü
                if (!_allowedMimeTypes.Contains(file.ContentType))
                {
                    return false;
                }

                // Dosya başlığı kontrolü (magic number validation)
                using var stream = file.OpenReadStream();
                var buffer = new byte[8];
                await stream.ReadAsync(buffer, 0, 8);

                // Basit magic number kontrolleri
                var header = BitConverter.ToString(buffer).Replace("-", "");
                
                // JPEG
                if (header.StartsWith("FFD8FF"))
                    return file.ContentType.StartsWith("image/jpeg");
                    
                // PNG
                if (header.StartsWith("89504E47"))
                    return file.ContentType.StartsWith("image/png");
                    
                // PDF
                if (header.StartsWith("255044462D"))
                    return file.ContentType == "application/pdf";

                // Diğer dosya tipleri için basic validation
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dosya içerik kontrolü başarısız: {FileName}", file.FileName);
                return false;
            }
        }

        /// <summary>
        /// Güvenli dosya adı oluşturur
        /// </summary>
        public string GenerateSafeFileName(string originalFileName)
        {
            if (string.IsNullOrEmpty(originalFileName))
                return $"file_{DateTime.UtcNow:yyyyMMddHHmmss}";

            // Zararlı karakterleri temizle
            var safeName = Regex.Replace(originalFileName, @"[^\w\.-]", "_");
            
            // Dosya adını sınırla
            var nameWithoutExt = Path.GetFileNameWithoutExtension(safeName);
            var extension = Path.GetExtension(safeName);
            
            if (nameWithoutExt.Length > 50)
            {
                nameWithoutExt = nameWithoutExt.Substring(0, 50);
            }

            // Benzersizlik için timestamp ekle
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            return $"{nameWithoutExt}_{timestamp}{extension}";
        }

        /// <summary>
        /// Dosya storage yolunu oluşturur
        /// </summary>
        public string GenerateFilePath(int serviceRequestId, string fileName)
        {
            var year = DateTime.UtcNow.Year;
            var month = DateTime.UtcNow.Month;
            
            return Path.Combine("uploads", "service-requests", year.ToString(), month.ToString("00"), serviceRequestId.ToString(), fileName);
        }

        /// <summary>
        /// Dosya istatistiklerini getirir
        /// </summary>
        public async Task<FileStatistics> GetFileStatisticsAsync()
        {
            try
            {
                var (totalFiles, totalSize, averageSize, maxSize, minSize) = await _fileRepository.GetFileStatisticsAsync();
                var extensionDistribution = await _fileRepository.GetExtensionDistributionAsync();

                // Aylık dağılım (basit versiyon)
                var monthlyDistribution = new Dictionary<string, int>();
                var currentDate = DateTime.UtcNow;
                for (int i = 0; i < 12; i++)
                {
                    var date = currentDate.AddMonths(-i);
                    var startOfMonth = new DateTime(date.Year, date.Month, 1);
                    var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
                    
                    var monthlyFiles = await _fileRepository.GetByDateRangeAsync(startOfMonth, endOfMonth);
                    monthlyDistribution[$"{date:yyyy-MM}"] = monthlyFiles.Count;
                }

                return new FileStatistics
                {
                    TotalFiles = totalFiles,
                    TotalFileSize = totalSize,
                    AverageFileSize = averageSize,
                    LargestFileSize = maxSize,
                    SmallestFileSize = minSize,
                    ExtensionDistribution = extensionDistribution,
                    MonthlyUploadDistribution = monthlyDistribution
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dosya istatistikleri hesaplanamadı");
                throw new ServiceException("Dosya istatistikleri hesaplanamadı", ex);
            }
        }

        /// <summary>
        /// Eski dosyaları temizler
        /// </summary>
        public async Task<int> CleanupOldFilesAsync(DateTime cutoffDate)
        {
            try
            {
                _logger.LogInformation("Eski dosya temizleme başlatılıyor: Kesim tarihi {CutoffDate}", cutoffDate);

                var oldFiles = await _fileRepository.GetByDateRangeAsync(DateTime.MinValue, cutoffDate);
                int deletedCount = 0;

                foreach (var file in oldFiles)
                {
                    try
                    {
                        // Fiziksel dosyayı sil
                        var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, file.FilePath);
                        if (File.Exists(fullPath))
                        {
                            File.Delete(fullPath);
                        }

                        // Veritabanından sil
                        await _fileRepository.DeleteAsync(file.Id);
                        deletedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Eski dosya silinirken hata: {FileName}", file.FileName);
                    }
                }

                _logger.LogInformation("Eski dosya temizleme tamamlandı: {DeletedCount} dosya silindi", deletedCount);
                return deletedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Eski dosya temizleme başarısız");
                return 0;
            }
        }

        /// <summary>
        /// Dosya boyutu formatlar (KB, MB, GB)
        /// </summary>
        public string FormatFileSize(long bytes)
        {
            const int scale = 1024;
            string[] orders = { "GB", "MB", "KB", "Bytes" };
            long max = (long)Math.Pow(scale, orders.Length - 1);

            foreach (string order in orders)
            {
                if (bytes > max)
                    return string.Format("{0:##.##} {1}", decimal.Divide(bytes, max), order);

                max /= scale;
            }
            return "0 Bytes";
        }

        #region Private Helper Methods

        /// <summary>
        /// Custom exception sınıfları (ServiceException, NotFoundException'ı kullanıyoruz)
        /// </summary>
        public class ServiceException : Exception
        {
            public ServiceException(string message) : base(message) { }
            public ServiceException(string message, Exception innerException) : base(message, innerException) { }
        }

        public class NotFoundException : Exception
        {
            public NotFoundException(string message) : base(message) { }
        }

        #endregion
    }
}