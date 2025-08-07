using Microsoft.EntityFrameworkCore;
using AiTeknikServis.Data;
using AiTeknikServis.Entities.Models;
using AiTeknikServis.Repositories.Contracts;

namespace AiTeknikServis.Repositories
{
    public class ServiceRequestFileRepository : IServiceRequestFileRepository
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// ServiceRequestFileRepository constructor - Veritabanı bağlamını enjekte eder
        /// </summary>
        public ServiceRequestFileRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// ID'ye göre dosyayı getirir
        /// </summary>
        public async Task<ServiceRequestFile?> GetByIdAsync(int id)
        {
            return await _context.ServiceRequestFiles
                .Include(f => f.ServiceRequest)
                .FirstOrDefaultAsync(f => f.Id == id);
        }

        /// <summary>
        /// Servis talebine ait dosyaları getirir
        /// </summary>
        public async Task<List<ServiceRequestFile>> GetByServiceRequestIdAsync(int serviceRequestId)
        {
            return await _context.ServiceRequestFiles
                .Where(f => f.ServiceRequestId == serviceRequestId)
                .OrderBy(f => f.UploadedDate)
                .ToListAsync();
        }

        /// <summary>
        /// Tüm dosyaları getirir
        /// </summary>
        public async Task<List<ServiceRequestFile>> GetAllAsync()
        {
            return await _context.ServiceRequestFiles
                .Include(f => f.ServiceRequest)
                .OrderByDescending(f => f.UploadedDate)
                .ToListAsync();
        }

        /// <summary>
        /// Yeni dosya kaydı oluşturur
        /// </summary>
        public async Task<ServiceRequestFile> CreateAsync(ServiceRequestFile file)
        {
            try
            {
                _context.ServiceRequestFiles.Add(file);
                await _context.SaveChangesAsync();
                return file;
            }
            catch (Exception ex)
            {
                throw new Exception($"Dosya kaydı oluşturulurken hata: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Dosya kaydını günceller
        /// </summary>
        public async Task<ServiceRequestFile> UpdateAsync(ServiceRequestFile file)
        {
            try
            {
                _context.ServiceRequestFiles.Update(file);
                await _context.SaveChangesAsync();
                return file;
            }
            catch (Exception ex)
            {
                throw new Exception($"Dosya kaydı güncellenirken hata: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Dosya kaydını siler
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var file = await GetByIdAsync(id);
                if (file == null)
                    return false;

                _context.ServiceRequestFiles.Remove(file);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Dosya kaydı silinirken hata: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Dosya uzantısına göre dosyaları getirir
        /// </summary>
        public async Task<List<ServiceRequestFile>> GetByExtensionAsync(string extension)
        {
            return await _context.ServiceRequestFiles
                .Where(f => f.FileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(f => f.UploadedDate)
                .ToListAsync();
        }

        /// <summary>
        /// Belirli tarih aralığındaki dosyaları getirir
        /// </summary>
        public async Task<List<ServiceRequestFile>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.ServiceRequestFiles
                .Where(f => f.UploadedDate >= startDate && f.UploadedDate <= endDate)
                .OrderByDescending(f => f.UploadedDate)
                .ToListAsync();
        }

        /// <summary>
        /// Toplam dosya sayısını getirir
        /// </summary>
        public async Task<int> GetTotalCountAsync()
        {
            return await _context.ServiceRequestFiles.CountAsync();
        }

        /// <summary>
        /// Toplam dosya boyutunu getirir
        /// </summary>
        public async Task<long> GetTotalFileSizeAsync()
        {
            return await _context.ServiceRequestFiles
                .SumAsync(f => f.FileSize);
        }

        /// <summary>
        /// Uzantı bazında dosya dağılımını getirir
        /// </summary>
        public async Task<Dictionary<string, int>> GetExtensionDistributionAsync()
        {
            var files = await _context.ServiceRequestFiles
                .Select(f => f.FileName)
                .ToListAsync();

            return files
                .Select(fileName => Path.GetExtension(fileName)?.ToLowerInvariant() ?? "")
                .Where(ext => !string.IsNullOrEmpty(ext))
                .GroupBy(ext => ext)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// Belirli boyuttan büyük dosyaları getirir
        /// </summary>
        public async Task<List<ServiceRequestFile>> GetFilesLargerThanAsync(long sizeInBytes)
        {
            return await _context.ServiceRequestFiles
                .Where(f => f.FileSize > sizeInBytes)
                .OrderByDescending(f => f.FileSize)
                .ToListAsync();
        }

        /// <summary>
        /// Belirli tarihten eski dosyaları siler
        /// </summary>
        public async Task<int> DeleteOldFilesAsync(DateTime cutoffDate)
        {
            try
            {
                var oldFiles = await _context.ServiceRequestFiles
                    .Where(f => f.UploadedDate < cutoffDate)
                    .ToListAsync();

                var deletedCount = oldFiles.Count;
                _context.ServiceRequestFiles.RemoveRange(oldFiles);
                await _context.SaveChangesAsync();

                return deletedCount;
            }
            catch (Exception ex)
            {
                throw new Exception($"Eski dosyalar silinirken hata: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Dosya istatistiklerini getirir
        /// </summary>
        public async Task<(int totalFiles, long totalSize, double averageSize, long maxSize, long minSize)> GetFileStatisticsAsync()
        {
            var files = await _context.ServiceRequestFiles
                .Select(f => f.FileSize)
                .ToListAsync();

            if (!files.Any())
            {
                return (0, 0, 0, 0, 0);
            }

            return (
                totalFiles: files.Count,
                totalSize: files.Sum(),
                averageSize: files.Average(),
                maxSize: files.Max(),
                minSize: files.Min()
            );
        }
    }
}