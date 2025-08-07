using AiTeknikServis.Entities.Models;

namespace AiTeknikServis.Repositories.Contracts
{
    public interface IServiceRequestFileRepository
    {
        /// <summary>
        /// ID'ye göre dosyayı getirir
        /// </summary>
        /// <param name="id">Dosya ID'si</param>
        /// <returns>Dosya bilgisi</returns>
        Task<ServiceRequestFile?> GetByIdAsync(int id);

        /// <summary>
        /// Servis talebine ait dosyaları getirir
        /// </summary>
        /// <param name="serviceRequestId">Servis talebi ID'si</param>
        /// <returns>Dosya listesi</returns>
        Task<List<ServiceRequestFile>> GetByServiceRequestIdAsync(int serviceRequestId);

        /// <summary>
        /// Tüm dosyaları getirir
        /// </summary>
        /// <returns>Tüm dosyalar</returns>
        Task<List<ServiceRequestFile>> GetAllAsync();

        /// <summary>
        /// Yeni dosya kaydı oluşturur
        /// </summary>
        /// <param name="file">Dosya bilgisi</param>
        /// <returns>Oluşturulan dosya</returns>
        Task<ServiceRequestFile> CreateAsync(ServiceRequestFile file);

        /// <summary>
        /// Dosya kaydını günceller
        /// </summary>
        /// <param name="file">Güncellenecek dosya</param>
        /// <returns>Güncellenmiş dosya</returns>
        Task<ServiceRequestFile> UpdateAsync(ServiceRequestFile file);

        /// <summary>
        /// Dosya kaydını siler
        /// </summary>
        /// <param name="id">Dosya ID'si</param>
        /// <returns>Silme işlemi başarılı mı?</returns>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Dosya uzantısına göre dosyaları getirir
        /// </summary>
        /// <param name="extension">Dosya uzantısı</param>
        /// <returns>Eşleşen dosyalar</returns>
        Task<List<ServiceRequestFile>> GetByExtensionAsync(string extension);

        /// <summary>
        /// Belirli tarih aralığındaki dosyaları getirir
        /// </summary>
        /// <param name="startDate">Başlangıç tarihi</param>
        /// <param name="endDate">Bitiş tarihi</param>
        /// <returns>Tarih aralığındaki dosyalar</returns>
        Task<List<ServiceRequestFile>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Toplam dosya sayısını getirir
        /// </summary>
        /// <returns>Toplam dosya sayısı</returns>
        Task<int> GetTotalCountAsync();

        /// <summary>
        /// Toplam dosya boyutunu getirir
        /// </summary>
        /// <returns>Toplam boyut (byte)</returns>
        Task<long> GetTotalFileSizeAsync();

        /// <summary>
        /// Uzantı bazında dosya dağılımını getirir
        /// </summary>
        /// <returns>Uzantı-sayı haritası</returns>
        Task<Dictionary<string, int>> GetExtensionDistributionAsync();

        /// <summary>
        /// Belirli boyuttan büyük dosyaları getirir
        /// </summary>
        /// <param name="sizeInBytes">Boyut limiti (byte)</param>
        /// <returns>Büyük dosyalar</returns>
        Task<List<ServiceRequestFile>> GetFilesLargerThanAsync(long sizeInBytes);

        /// <summary>
        /// Belirli tarihten eski dosyaları siler
        /// </summary>
        /// <param name="cutoffDate">Kesim tarihi</param>
        /// <returns>Silinen dosya sayısı</returns>
        Task<int> DeleteOldFilesAsync(DateTime cutoffDate);

        /// <summary>
        /// Dosya istatistiklerini getirir
        /// </summary>
        /// <returns>İstatistik bilgileri</returns>
        Task<(int totalFiles, long totalSize, double averageSize, long maxSize, long minSize)> GetFileStatisticsAsync();
    }
} 