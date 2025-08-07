using Microsoft.EntityFrameworkCore;
using AiTeknikServis.Data;
using AiTeknikServis.Entities.Models;
using AiTeknikServis.Repositories.Contracts;

namespace AiTeknikServis.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// NotificationRepository constructor - Veritabanı bağlamını enjekte eder
        /// </summary>
        public NotificationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// ID'ye göre bildirimi servis talebi ve kullanıcı bilgileriyle birlikte getirir
        /// </summary>
        public async Task<Notification?> GetByIdAsync(int id)
        {
            return await _context.Notifications
                .Include(n => n.ServiceRequest)
                .Include(n => n.User)
                .FirstOrDefaultAsync(n => n.Id == id);
        }

        /// <summary>
        /// Tüm bildirimleri servis talebi ve kullanıcı bilgileriyle birlikte getirir
        /// </summary>
        public async Task<List<Notification>> GetAllAsync()
        {
            return await _context.Notifications
                .Include(n => n.ServiceRequest)
                .Include(n => n.User)
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();
        }

        /// <summary>
        /// Yeni bir bildirim oluşturur
        /// </summary>
        public async Task<Notification> CreateAsync(Notification notification)
        {
            try
            {
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
                return notification;
            }
            catch (Exception ex)
            {
                throw new Exception($"Bildirim oluşturulurken hata: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Mevcut bildirimi günceller
        /// </summary>
        public async Task<Notification> UpdateAsync(Notification notification)
        {
            try
            {
                _context.Notifications.Update(notification);
                await _context.SaveChangesAsync();
                return notification;
            }
            catch (Exception ex)
            {
                throw new Exception($"Bildirim güncellenirken hata: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var notification = await _context.Notifications.FindAsync(id);
                if (notification == null)
                    return false;

                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting notification: {ex.Message}", ex);
            }
        }

        public async Task<List<Notification>> GetByUserIdAsync(int userId)
        {
            return await _context.Notifications
                .Include(n => n.ServiceRequest)
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<Notification>> GetByServiceRequestIdAsync(int serviceRequestId)
        {
            return await _context.Notifications
                .Include(n => n.User)
                .Where(n => n.ServiceRequestId == serviceRequestId)
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<Notification>> GetByTypeAsync(NotificationType type)
        {
            return await _context.Notifications
                .Include(n => n.ServiceRequest)
                .Include(n => n.User)
                .Where(n => n.Type == type)
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();
        }

        /// <summary>
        /// Belirli bir kullanıcının okunmamış bildirimlerini getirir
        /// </summary>
        public async Task<List<Notification>> GetUnreadNotificationsAsync(int userId)
        {
            return await _context.Notifications
                .Include(n => n.ServiceRequest)
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();
        }

        /// <summary>
        /// Belirli bir kullanıcının son bildirimlerini getirir (varsayılan 10 adet)
        /// </summary>
        public async Task<List<Notification>> GetRecentNotificationsAsync(int userId, int count = 10)
        {
            return await _context.Notifications
                .Include(n => n.ServiceRequest)
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedDate)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<Notification>> GetPendingEmailNotificationsAsync()
        {
            return await _context.Notifications
                .Include(n => n.ServiceRequest)
                .Include(n => n.User)
                .Where(n => !n.IsEmailSent && !string.IsNullOrEmpty(n.RecipientEmail))
                .OrderBy(n => n.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<Notification>> GetPendingSmsNotificationsAsync()
        {
            return await _context.Notifications
                .Include(n => n.ServiceRequest)
                .Include(n => n.User)
                .Where(n => !n.IsSmsSent && !string.IsNullOrEmpty(n.RecipientPhone))
                .OrderBy(n => n.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<Notification>> GetFailedNotificationsAsync()
        {
            var cutoffDate = DateTime.UtcNow.AddHours(-24); // 24 saat öncesi
            return await _context.Notifications
                .Include(n => n.ServiceRequest)
                .Include(n => n.User)
                .Where(n => n.CreatedDate < cutoffDate && 
                           ((!string.IsNullOrEmpty(n.RecipientEmail) && !n.IsEmailSent) ||
                            (!string.IsNullOrEmpty(n.RecipientPhone) && !n.IsSmsSent)))
                .OrderBy(n => n.CreatedDate)
                .ToListAsync();
        }

        public async Task<bool> MarkEmailAsSentAsync(int notificationId)
        {
            try
            {
                var notification = await _context.Notifications.FindAsync(notificationId);
                if (notification == null)
                    return false;

                notification.IsEmailSent = true;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error marking email as sent: {ex.Message}", ex);
            }
        }

        public async Task<bool> MarkSmsAsSentAsync(int notificationId)
        {
            try
            {
                var notification = await _context.Notifications.FindAsync(notificationId);
                if (notification == null)
                    return false;

                notification.IsSmsSent = true;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error marking SMS as sent: {ex.Message}", ex);
            }
        }

        public async Task<bool> MarkAsReadAsync(int notificationId)
        {
            try
            {
                var notification = await _context.Notifications.FindAsync(notificationId);
                if (notification == null)
                    return false;

                notification.IsRead = true;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error marking notification as read: {ex.Message}", ex);
            }
        }

        public async Task<bool> MarkAsUnreadAsync(int notificationId)
        {
            try
            {
                var notification = await _context.Notifications.FindAsync(notificationId);
                if (notification == null)
                    return false;

                notification.IsRead = false;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error marking notification as unread: {ex.Message}", ex);
            }
        }

        public async Task<bool> MarkAllAsReadAsync(int userId)
        {
            try
            {
                var notifications = await _context.Notifications
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .ToListAsync();

                foreach (var notification in notifications)
                {
                    notification.IsRead = true;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error marking all notifications as read: {ex.Message}", ex);
            }
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task<bool> DeleteOldNotificationsAsync(DateTime cutoffDate)
        {
            try
            {
                var oldNotifications = await _context.Notifications
                    .Where(n => n.CreatedDate < cutoffDate)
                    .ToListAsync();

                _context.Notifications.RemoveRange(oldNotifications);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting old notifications: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteReadNotificationsAsync(int userId, DateTime cutoffDate)
        {
            try
            {
                var readNotifications = await _context.Notifications
                    .Where(n => n.UserId == userId && n.IsRead && n.CreatedDate < cutoffDate)
                    .ToListAsync();

                _context.Notifications.RemoveRange(readNotifications);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting read notifications: {ex.Message}", ex);
            }
        }

        public async Task<int> GetTotalNotificationCountAsync()
        {
            return await _context.Notifications.CountAsync();
        }

        public async Task<int> GetNotificationCountByTypeAsync(NotificationType type)
        {
            return await _context.Notifications
                .CountAsync(n => n.Type == type);
        }

        public async Task<int> GetNotificationCountByUserAsync(int userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId);
        }

        public async Task<Dictionary<NotificationType, int>> GetNotificationStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Notifications.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(n => n.CreatedDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(n => n.CreatedDate <= endDate.Value);

            return await query
                .GroupBy(n => n.Type)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        public async Task<List<Notification>> GetNotificationsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Notifications
                .Include(n => n.ServiceRequest)
                .Include(n => n.User)
                .Where(n => n.CreatedDate >= startDate && n.CreatedDate <= endDate)
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<Notification>> SearchNotificationsAsync(string searchTerm)
        {
            return await _context.Notifications
                .Include(n => n.ServiceRequest)
                .Include(n => n.User)
                .Where(n => n.Title.Contains(searchTerm) || n.Message.Contains(searchTerm))
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<Notification>> GetFilteredNotificationsAsync(
            int? userId = null,
            NotificationType? type = null,
            bool? isRead = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var query = _context.Notifications
                .Include(n => n.ServiceRequest)
                .Include(n => n.User)
                .AsQueryable();

            if (userId.HasValue)
                query = query.Where(n => n.UserId == userId.Value);

            if (type.HasValue)
                query = query.Where(n => n.Type == type.Value);

            if (isRead.HasValue)
                query = query.Where(n => n.IsRead == isRead.Value);

            if (startDate.HasValue)
                query = query.Where(n => n.CreatedDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(n => n.CreatedDate <= endDate.Value);

            return await query
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();
        }
    }
}