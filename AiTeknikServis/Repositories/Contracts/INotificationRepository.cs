using AiTeknikServis.Entities.Models;

namespace AiTeknikServis.Repositories.Contracts
{
    public interface INotificationRepository
    {
        // Basic CRUD Operations
        Task<Notification?> GetByIdAsync(int id);
        Task<List<Notification>> GetAllAsync();
        Task<Notification> CreateAsync(Notification notification);
        Task<Notification> UpdateAsync(Notification notification);
        Task<bool> DeleteAsync(int id);

        // Query Operations
        Task<List<Notification>> GetByUserIdAsync(int userId);
        Task<List<Notification>> GetByServiceRequestIdAsync(int serviceRequestId);
        Task<List<Notification>> GetByTypeAsync(NotificationType type);
        Task<List<Notification>> GetUnreadNotificationsAsync(int userId);
        Task<List<Notification>> GetRecentNotificationsAsync(int userId, int count = 10);

        // Email/SMS Operations
        Task<List<Notification>> GetPendingEmailNotificationsAsync();
        Task<List<Notification>> GetPendingSmsNotificationsAsync();
        Task<List<Notification>> GetFailedNotificationsAsync();
        Task<bool> MarkEmailAsSentAsync(int notificationId);
        Task<bool> MarkSmsAsSentAsync(int notificationId);

        // Read/Unread Operations
        Task<bool> MarkAsReadAsync(int notificationId);
        Task<bool> MarkAsUnreadAsync(int notificationId);
        Task<bool> MarkAllAsReadAsync(int userId);
        Task<int> GetUnreadCountAsync(int userId);

        // Cleanup Operations
        Task<bool> DeleteOldNotificationsAsync(DateTime cutoffDate);
        Task<bool> DeleteReadNotificationsAsync(int userId, DateTime cutoffDate);

        // Statistics
        Task<int> GetTotalNotificationCountAsync();
        Task<int> GetNotificationCountByTypeAsync(NotificationType type);
        Task<int> GetNotificationCountByUserAsync(int userId);
        Task<Dictionary<NotificationType, int>> GetNotificationStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);

        // Search and Filter
        Task<List<Notification>> GetNotificationsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<List<Notification>> SearchNotificationsAsync(string searchTerm);
        Task<List<Notification>> GetFilteredNotificationsAsync(
            int? userId = null,
            NotificationType? type = null,
            bool? isRead = null,
            DateTime? startDate = null,
            DateTime? endDate = null);
    }
}