using AutoMapper;
using AiTeknikServis.Entities.Dtos.Notification;
using AiTeknikServis.Entities.Models;
using AiTeknikServis.Repositories.Contracts;
using AiTeknikServis.Services.Contracts;

namespace AiTeknikServis.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IServiceRequestRepository _serviceRequestRepository;
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly IMapper _mapper;
        private readonly ILogger<NotificationService> _logger;

        /// <summary>
        /// NotificationService constructor - Gerekli servisleri enjekte eder
        /// </summary>
        public NotificationService(
            INotificationRepository notificationRepository,
            IServiceRequestRepository serviceRequestRepository,
            IUserRepository userRepository,
            IEmailService emailService,
            IMapper mapper,
            ILogger<NotificationService> logger)
        {
            _notificationRepository = notificationRepository;
            _serviceRequestRepository = serviceRequestRepository;
            _userRepository = userRepository;
            _emailService = emailService;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Yeni bir bildirim oluşturur
        /// </summary>
        public async Task<NotificationResponseDto> CreateAsync(NotificationCreateDto dto)
        {
            try
            {
                _logger.LogInformation("Yeni bildirim oluşturuluyor: {Title}", dto.Title);

                var notification = _mapper.Map<Notification>(dto);
                notification.CreatedDate = DateTime.UtcNow;

                var createdNotification = await _notificationRepository.CreateAsync(notification);

                // Email gönderimi
                if (!string.IsNullOrEmpty(dto.RecipientEmail))
                {
                    await SendEmailNotificationAsync(createdNotification);
                }

                _logger.LogInformation("Bildirim başarıyla oluşturuldu: ID {Id}", createdNotification.Id);

                return _mapper.Map<NotificationResponseDto>(createdNotification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bildirim oluşturulurken hata: {Title}", dto.Title);
                throw new ServiceException("Bildirim oluşturulamadı", ex);
            }
        }

        /// <summary>
        /// ID'ye göre bildirimi getirir
        /// </summary>
        public async Task<NotificationResponseDto> GetByIdAsync(int id)
        {
            try
            {
                var notification = await _notificationRepository.GetByIdAsync(id);
                if (notification == null)
                {
                    throw new NotFoundException($"ID {id} ile bildirim bulunamadı");
                }

                return _mapper.Map<NotificationResponseDto>(notification);
            }
            catch (Exception ex) when (!(ex is NotFoundException))
            {
                _logger.LogError(ex, "Bildirim getirilirken hata: ID {Id}", id);
                throw new ServiceException("Bildirim getirilemedi", ex);
            }
        }

        /// <summary>
        /// Belirli bir kullanıcının bildirimlerini getirir
        /// </summary>
        public async Task<List<NotificationResponseDto>> GetByUserAsync(int userId)
        {
            try
            {
                var notifications = await _notificationRepository.GetByUserIdAsync(userId);
                return _mapper.Map<List<NotificationResponseDto>>(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı bildirimleri getirilirken hata: UserID {UserId}", userId);
                throw new ServiceException("Kullanıcı bildirimleri getirilemedi", ex);
            }
        }

        /// <summary>
        /// Kullanıcının okunmamış bildirimlerini getirir
        /// </summary>
        public async Task<List<NotificationResponseDto>> GetUnreadNotificationsAsync(int userId)
        {
            try
            {
                var notifications = await _notificationRepository.GetUnreadNotificationsAsync(userId);
                return _mapper.Map<List<NotificationResponseDto>>(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Okunmamış bildirimler getirilirken hata: UserID {UserId}", userId);
                throw new ServiceException("Okunmamış bildirimler getirilemedi", ex);
            }
        }

        /// <summary>
        /// Kullanıcının son bildirimlerini getirir
        /// </summary>
        public async Task<List<NotificationResponseDto>> GetRecentNotificationsAsync(int userId, int count = 10)
        {
            try
            {
                var notifications = await _notificationRepository.GetRecentNotificationsAsync(userId, count);
                return _mapper.Map<List<NotificationResponseDto>>(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Son bildirimler getirilirken hata: UserID {UserId}", userId);
                throw new ServiceException("Son bildirimler getirilemedi", ex);
            }
        }

        /// <summary>
        /// Bildirimi okundu olarak işaretler
        /// </summary>
        public async Task<bool> MarkAsReadAsync(int notificationId)
        {
            try
            {
                var result = await _notificationRepository.MarkAsReadAsync(notificationId);
                if (result)
                {
                    _logger.LogInformation("Bildirim okundu olarak işaretlendi: ID {NotificationId}", notificationId);
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bildirim okundu işaretlenirken hata: ID {NotificationId}", notificationId);
                throw new ServiceException("Bildirim okundu işaretlenemedi", ex);
            }
        }

        /// <summary>
        /// Kullanıcının tüm bildirimlerini okundu olarak işaretler
        /// </summary>
        public async Task<bool> MarkAllAsReadAsync(int userId)
        {
            try
            {
                var result = await _notificationRepository.MarkAllAsReadAsync(userId);
                if (result)
                {
                    _logger.LogInformation("Kullanıcının tüm bildirimleri okundu olarak işaretlendi: UserID {UserId}", userId);
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tüm bildirimler okundu işaretlenirken hata: UserID {UserId}", userId);
                throw new ServiceException("Tüm bildirimler okundu işaretlenemedi", ex);
            }
        }

        /// <summary>
        /// Kullanıcının okunmamış bildirim sayısını getirir
        /// </summary>
        public async Task<int> GetUnreadCountAsync(int userId)
        {
            try
            {
                return await _notificationRepository.GetUnreadCountAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Okunmamış bildirim sayısı getirilirken hata: UserID {UserId}", userId);
                return 0;
            }
        }

        /// <summary>
        /// Servis talebi oluşturulduğunda bildirim gönderir
        /// </summary>
        public async Task SendServiceRequestCreatedNotificationAsync(int serviceRequestId)
        {
            try
            {
                var serviceRequest = await _serviceRequestRepository.GetByIdWithDetailsAsync(serviceRequestId);
                if (serviceRequest == null) return;

                // Müşteriye bildirim
                var customerNotification = new NotificationCreateDto
                {
                    Title = "Servis Talebiniz Alındı",
                    Message = $"'{serviceRequest.Title}' başlıklı servis talebiniz başarıyla alındı. Talep numaranız: #{serviceRequestId}. En kısa sürede size dönüş yapılacaktır.",
                    Type = NotificationType.ServiceRequestCreated,
                    UserId = serviceRequest.CustomerId,
                    ServiceRequestId = serviceRequestId,
                    RecipientEmail = serviceRequest.Customer?.Email
                };

                await CreateAsync(customerNotification);

                // Yöneticilere bildirim
                await NotifyManagersAsync("Yeni Servis Talebi", 
                    $"Yeni servis talebi alındı: '{serviceRequest.Title}' (#{serviceRequestId}). Kategori: {serviceRequest.Category}, Öncelik: {serviceRequest.Priority}",
                    NotificationType.ServiceRequestCreated, serviceRequestId);

                _logger.LogInformation("Servis talebi oluşturma bildirimi gönderildi: ID {ServiceRequestId}", serviceRequestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Servis talebi oluşturma bildirimi gönderilirken hata: ID {ServiceRequestId}", serviceRequestId);
            }
        }

        /// <summary>
        /// Teknisyen atandığında bildirim gönderir
        /// </summary>
        public async Task SendTechnicianAssignedNotificationAsync(int serviceRequestId, int technicianId)
        {
            try
            {
                var serviceRequest = await _serviceRequestRepository.GetByIdWithDetailsAsync(serviceRequestId);
                var technician = await _userRepository.GetTechnicianByIdAsync(technicianId);
                
                if (serviceRequest == null || technician == null) return;

                // Müşteriye bildirim
                var customerNotification = new NotificationCreateDto
                {
                    Title = "Teknisyen Atandı",
                    Message = $"'{serviceRequest.Title}' başlıklı servis talebinize {technician.FirstName} {technician.LastName} teknisyeni atandı. Yakında sizinle iletişime geçecektir.",
                    Type = NotificationType.TechnicianAssigned,
                    UserId = serviceRequest.CustomerId,
                    ServiceRequestId = serviceRequestId,
                    RecipientEmail = serviceRequest.Customer?.Email
                };

                await CreateAsync(customerNotification);

                // Teknisyene bildirim
                var technicianNotification = new NotificationCreateDto
                {
                    Title = "Yeni Görev Atandı",
                    Message = $"Size yeni bir görev atandı: '{serviceRequest.Title}' (#{serviceRequestId}). Müşteri: {serviceRequest.Customer?.FirstName} {serviceRequest.Customer?.LastName}",
                    Type = NotificationType.TechnicianAssigned,
                    UserId = technicianId,
                    ServiceRequestId = serviceRequestId,
                    RecipientEmail = technician.Email
                };

                await CreateAsync(technicianNotification);

                _logger.LogInformation("Teknisyen atama bildirimi gönderildi: ServiceRequestID {ServiceRequestId}, TechnicianID {TechnicianId}", 
                    serviceRequestId, technicianId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Teknisyen atama bildirimi gönderilirken hata: ServiceRequestID {ServiceRequestId}, TechnicianID {TechnicianId}", 
                    serviceRequestId, technicianId);
            }
        }

        /// <summary>
        /// Servis talebi durumu değiştiğinde bildirim gönderir
        /// </summary>
        public async Task SendStatusChangeNotificationAsync(int serviceRequestId, ServiceStatus newStatus)
        {
            try
            {
                var serviceRequest = await _serviceRequestRepository.GetByIdWithDetailsAsync(serviceRequestId);
                if (serviceRequest == null) return;

                var statusText = GetStatusText(newStatus);
                var customerNotification = new NotificationCreateDto
                {
                    Title = "Servis Durumu Güncellendi",
                    Message = $"'{serviceRequest.Title}' başlıklı servis talebinizin durumu '{statusText}' olarak güncellendi.",
                    Type = NotificationType.ServiceRequestStatusChanged,
                    UserId = serviceRequest.CustomerId,
                    ServiceRequestId = serviceRequestId,
                    RecipientEmail = serviceRequest.Customer?.Email
                };

                await CreateAsync(customerNotification);

                _logger.LogInformation("Durum değişikliği bildirimi gönderildi: ServiceRequestID {ServiceRequestId}, Yeni Durum: {NewStatus}", 
                    serviceRequestId, newStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Durum değişikliği bildirimi gönderilirken hata: ServiceRequestID {ServiceRequestId}", serviceRequestId);
            }
        }

        /// <summary>
        /// Servis tamamlandığında bildirim gönderir
        /// </summary>
        public async Task SendServiceCompletedNotificationAsync(int serviceRequestId)
        {
            try
            {
                var serviceRequest = await _serviceRequestRepository.GetByIdWithDetailsAsync(serviceRequestId);
                if (serviceRequest == null) return;

                var customerNotification = new NotificationCreateDto
                {
                    Title = "Servis Tamamlandı",
                    Message = $"'{serviceRequest.Title}' başlıklı servis talebiniz başarıyla tamamlandı. Hizmetimizden memnun kaldığınızı umuyoruz. Geri bildirimlerinizi bekliyoruz.",
                    Type = NotificationType.ServiceRequestCompleted,
                    UserId = serviceRequest.CustomerId,
                    ServiceRequestId = serviceRequestId,
                    RecipientEmail = serviceRequest.Customer?.Email
                };

                await CreateAsync(customerNotification);

                _logger.LogInformation("Servis tamamlama bildirimi gönderildi: ServiceRequestID {ServiceRequestId}", serviceRequestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Servis tamamlama bildirimi gönderilirken hata: ServiceRequestID {ServiceRequestId}", serviceRequestId);
            }
        }

        /// <summary>
        /// Acil öncelikli talep için yöneticilere bildirim gönderir
        /// </summary>
        public async Task SendUrgentRequestNotificationAsync(int serviceRequestId)
        {
            try
            {
                var serviceRequest = await _serviceRequestRepository.GetByIdWithDetailsAsync(serviceRequestId);
                if (serviceRequest == null) return;

                await NotifyManagersAsync("ACİL: Kritik Öncelikli Servis Talebi", 
                    $"Kritik öncelikli servis talebi alındı: '{serviceRequest.Title}' (#{serviceRequestId}). Müşteri: {serviceRequest.Customer?.FirstName} {serviceRequest.Customer?.LastName}. Acil müdahale gerekiyor!",
                    NotificationType.UrgentRequest, serviceRequestId);

                _logger.LogInformation("Acil talep bildirimi gönderildi: ServiceRequestID {ServiceRequestId}", serviceRequestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Acil talep bildirimi gönderilirken hata: ServiceRequestID {ServiceRequestId}", serviceRequestId);
            }
        }

        /// <summary>
        /// Bekleyen email bildirimlerini işler
        /// </summary>
        public async Task<int> ProcessPendingEmailNotificationsAsync()
        {
            try
            {
                var pendingNotifications = await _notificationRepository.GetPendingEmailNotificationsAsync();
                int processedCount = 0;

                foreach (var notification in pendingNotifications)
                {
                    try
                    {
                        await SendEmailNotificationAsync(notification);
                        await _notificationRepository.MarkEmailAsSentAsync(notification.Id);
                        processedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Email bildirimi gönderilirken hata: NotificationID {NotificationId}", notification.Id);
                    }
                }

                if (processedCount > 0)
                {
                    _logger.LogInformation("Bekleyen email bildirimleri işlendi: {ProcessedCount} adet", processedCount);
                }

                return processedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bekleyen email bildirimleri işlenirken hata");
                return 0;
            }
        }

        /// <summary>
        /// Eski bildirimleri temizler
        /// </summary>
        public async Task<int> CleanupOldNotificationsAsync(DateTime cutoffDate)
        {
            try
            {
                var result = await _notificationRepository.DeleteOldNotificationsAsync(cutoffDate);
                if (result)
                {
                    _logger.LogInformation("Eski bildirimler temizlendi: Kesim tarihi {CutoffDate}", cutoffDate);
                    return 1; // Repository'den silinen sayı dönmediği için basit değer
                }
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Eski bildirimler temizlenirken hata: Kesim tarihi {CutoffDate}", cutoffDate);
                return 0;
            }
        }

        /// <summary>
        /// Bildirim istatistiklerini getirir
        /// </summary>
        public async Task<NotificationStatistics> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var statistics = new NotificationStatistics();

                // Temel sayılar
                statistics.TotalNotifications = await _notificationRepository.GetTotalNotificationCountAsync();
                statistics.UnreadNotifications = await _notificationRepository.GetFilteredNotificationsAsync(isRead: false).ContinueWith(t => t.Result.Count);

                // Tür bazında dağılım
                statistics.TypeDistribution = await _notificationRepository.GetNotificationStatisticsAsync(startDate, endDate);

                // Email istatistikleri
                var allNotifications = await _notificationRepository.GetNotificationsByDateRangeAsync(
                    startDate ?? DateTime.UtcNow.AddMonths(-1), 
                    endDate ?? DateTime.UtcNow);

                var emailNotifications = allNotifications.Where(n => !string.IsNullOrEmpty(n.RecipientEmail)).ToList();
                statistics.EmailsSent = emailNotifications.Count(n => n.IsEmailSent);
                statistics.FailedEmails = emailNotifications.Count(n => !n.IsEmailSent);
                statistics.EmailSuccessRate = emailNotifications.Any() 
                    ? (double)statistics.EmailsSent / emailNotifications.Count * 100 
                    : 0;

                // Ortalama okunma süresi (basit hesaplama)
                var readNotifications = allNotifications.Where(n => n.IsRead).ToList();
                if (readNotifications.Any())
                {
                    // Basit hesaplama - gerçek implementasyonda read date tutulacak
                    statistics.AverageReadTime = 2.5; // Örnek değer
                }

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bildirim istatistikleri hesaplanırken hata");
                throw new ServiceException("Bildirim istatistikleri hesaplanamadı", ex);
            }
        }

        /// <summary>
        /// Toplu bildirim gönderir
        /// </summary>
        public async Task<int> SendBulkNotificationAsync(List<int> userIds, string title, string message, NotificationType type)
        {
            try
            {
                int sentCount = 0;

                foreach (var userId in userIds)
                {
                    try
                    {
                        var user = await _userRepository.GetByIdAsync(userId);
                        if (user == null) continue;

                        var notificationDto = new NotificationCreateDto
                        {
                            Title = title,
                            Message = message,
                            Type = type,
                            UserId = userId,
                            RecipientEmail = user.Email
                        };

                        await CreateAsync(notificationDto);
                        sentCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Toplu bildirim gönderilirken hata: UserID {UserId}", userId);
                    }
                }

                _logger.LogInformation("Toplu bildirim gönderildi: {SentCount}/{TotalCount}", sentCount, userIds.Count);
                return sentCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Toplu bildirim gönderilirken hata");
                return 0;
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Email bildirimi gönderir
        /// </summary>
        private async Task SendEmailNotificationAsync(Notification notification)
        {
            if (string.IsNullOrEmpty(notification.RecipientEmail))
                return;

            try
            {
                await _emailService.SendEmailAsync(
                    notification.RecipientEmail,
                    notification.Title,
                    notification.Message);

                _logger.LogDebug("Email bildirimi gönderildi: {Email}", notification.RecipientEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email bildirimi gönderilirken hata: {Email}", notification.RecipientEmail);
                throw;
            }
        }

        /// <summary>
        /// Yöneticilere bildirim gönderir
        /// </summary>
        private async Task NotifyManagersAsync(string title, string message, NotificationType type, int? serviceRequestId = null)
        {
            try
            {
                var managers = await _userRepository.GetAllManagersAsync();
                var admins = await _userRepository.GetAllAdminsAsync();
                
                var allManagers = managers.Cast<User>().Concat(admins.Cast<User>()).ToList();

                foreach (var manager in allManagers)
                {
                    var notificationDto = new NotificationCreateDto
                    {
                        Title = title,
                        Message = message,
                        Type = type,
                        UserId = manager.Id,
                        ServiceRequestId = serviceRequestId,
                        RecipientEmail = manager.Email
                    };

                    await CreateAsync(notificationDto);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Yöneticilere bildirim gönderilirken hata");
            }
        }

        /// <summary>
        /// Durum metnini döndürür
        /// </summary>
        private string GetStatusText(ServiceStatus status)
        {
            return status switch
            {
                ServiceStatus.Pending => "Beklemede",
                ServiceStatus.InProgress => "İşlemde",
                ServiceStatus.OnHold => "Bekletildi",
                ServiceStatus.Completed => "Tamamlandı",
                ServiceStatus.Cancelled => "İptal Edildi",
                ServiceStatus.Rejected => "Reddedildi",
                _ => status.ToString()
            };
        }

        #endregion
    }
}