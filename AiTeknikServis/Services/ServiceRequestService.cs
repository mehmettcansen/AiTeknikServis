using AutoMapper;
using AiTeknikServis.Entities.Dtos.ServiceRequest;
using AiTeknikServis.Entities.Models;
using AiTeknikServis.Repositories.Contracts;
using AiTeknikServis.Services.Contracts;

namespace AiTeknikServis.Services
{
    public class ServiceRequestService : IServiceRequestService
    {
        private readonly IServiceRequestRepository _serviceRequestRepository;
        private readonly IUserRepository _userRepository;
        private readonly IAiPredictionService _aiPredictionService;
        private readonly INotificationService _notificationService;
        private readonly IReportService _reportService;
        private readonly IMapper _mapper;
        private readonly ILogger<ServiceRequestService> _logger;

        /// <summary>
        /// ServiceRequestService constructor - Gerekli servisleri enjekte eder
        /// </summary>
        public ServiceRequestService(
            IServiceRequestRepository serviceRequestRepository,
            IUserRepository userRepository,
            IAiPredictionService aiPredictionService,
            INotificationService notificationService,
            IReportService reportService,
            IMapper mapper,
            ILogger<ServiceRequestService> logger)
        {
            _serviceRequestRepository = serviceRequestRepository;
            _userRepository = userRepository;
            _aiPredictionService = aiPredictionService;
            _notificationService = notificationService;
            _reportService = reportService;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Yeni bir servis talebi oluşturur ve AI analizi yapar
        /// </summary>
        public async Task<ServiceRequestResponseDto> CreateAsync(ServiceRequestCreateDto dto)
        {
            try
            {
                _logger.LogInformation("Yeni servis talebi oluşturuluyor: {Title}", dto.Title);

                // DTO'yu entity'ye dönüştür
                var serviceRequest = _mapper.Map<ServiceRequest>(dto);
                serviceRequest.CreatedDate = DateTime.UtcNow;
                serviceRequest.Status = ServiceStatus.Pending;

                // AI analizi yap
                var aiAnalysis = await _aiPredictionService.AnalyzeServiceRequestAsync(
                    dto.Description, 
                    dto.Title, 
                    dto.ProductInfo);

                // AI sonuçlarını servis talebine uygula
                serviceRequest.Category = aiAnalysis.PredictedCategory;
                serviceRequest.Priority = aiAnalysis.PredictedPriority;

                // Servis talebini kaydet
                var createdServiceRequest = await _serviceRequestRepository.CreateAsync(serviceRequest);

                // AI tahminini kaydet
                await _aiPredictionService.SavePredictionAsync(createdServiceRequest.Id, aiAnalysis);

                // Bildirim gönder
                await _notificationService.SendServiceRequestCreatedNotificationAsync(createdServiceRequest.Id);

                // Acil öncelikli ise yöneticilere bildirim gönder
                if (aiAnalysis.PredictedPriority == Priority.Critical)
                {
                    await _notificationService.SendUrgentRequestNotificationAsync(createdServiceRequest.Id);
                }

                _logger.LogInformation("Servis talebi başarıyla oluşturuldu: ID {Id}, Kategori: {Category}, Öncelik: {Priority}", 
                    createdServiceRequest.Id, aiAnalysis.PredictedCategory, aiAnalysis.PredictedPriority);

                // Response DTO'ya dönüştür ve döndür
                var result = await GetByIdAsync(createdServiceRequest.Id);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Servis talebi oluşturulurken hata: {Title}", dto.Title);
                throw new ServiceException("Servis talebi oluşturulamadı", ex);
            }
        }

        /// <summary>
        /// Mevcut servis talebini günceller
        /// </summary>
        public async Task<ServiceRequestResponseDto> UpdateAsync(int id, ServiceRequestUpdateDto dto)
        {
            try
            {
                _logger.LogInformation("Servis talebi güncelleniyor: ID {Id}", id);

                var existingServiceRequest = await _serviceRequestRepository.GetByIdAsync(id);
                if (existingServiceRequest == null)
                {
                    throw new NotFoundException($"ID {id} ile servis talebi bulunamadı");
                }

                // Güncelleme verilerini uygula
                _mapper.Map(dto, existingServiceRequest);

                // Güncelle
                var updatedServiceRequest = await _serviceRequestRepository.UpdateAsync(existingServiceRequest);

                // Durum değişikliği varsa bildirim gönder
                if (dto.Status.HasValue && dto.Status.Value != existingServiceRequest.Status)
                {
                    await _notificationService.SendStatusChangeNotificationAsync(id, dto.Status.Value);
                }

                _logger.LogInformation("Servis talebi başarıyla güncellendi: ID {Id}", id);

                return await GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Servis talebi güncellenirken hata: ID {Id}", id);
                throw new ServiceException("Servis talebi güncellenemedi", ex);
            }
        }

        /// <summary>
        /// ID'ye göre servis talebini getirir
        /// </summary>
        public async Task<ServiceRequestResponseDto> GetByIdAsync(int id)
        {
            try
            {
                var serviceRequest = await _serviceRequestRepository.GetByIdWithDetailsAsync(id);
                if (serviceRequest == null)
                {
                    throw new NotFoundException($"ID {id} ile servis talebi bulunamadı");
                }

                return _mapper.Map<ServiceRequestResponseDto>(serviceRequest);
            }
            catch (Exception ex) when (!(ex is NotFoundException))
            {
                _logger.LogError(ex, "Servis talebi getirilirken hata: ID {Id}", id);
                throw new ServiceException("Servis talebi getirilemedi", ex);
            }
        }

        /// <summary>
        /// Belirli bir müşteriye ait servis taleplerini getirir
        /// </summary>
        public async Task<List<ServiceRequestResponseDto>> GetByCustomerAsync(int customerId)
        {
            try
            {
                var serviceRequests = await _serviceRequestRepository.GetByCustomerIdAsync(customerId);
                return _mapper.Map<List<ServiceRequestResponseDto>>(serviceRequests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Müşteri servis talepleri getirilirken hata: CustomerID {CustomerId}", customerId);
                throw new ServiceException("Müşteri servis talepleri getirilemedi", ex);
            }
        }

        /// <summary>
        /// Belirli bir teknisyene atanmış servis taleplerini getirir
        /// </summary>
        public async Task<List<ServiceRequestResponseDto>> GetByTechnicianAsync(int technicianId)
        {
            try
            {
                var serviceRequests = await _serviceRequestRepository.GetByTechnicianIdAsync(technicianId);
                return _mapper.Map<List<ServiceRequestResponseDto>>(serviceRequests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Teknisyen servis talepleri getirilirken hata: TechnicianID {TechnicianId}", technicianId);
                throw new ServiceException("Teknisyen servis talepleri getirilemedi", ex);
            }
        }

        /// <summary>
        /// Tüm servis taleplerini getirir
        /// </summary>
        public async Task<List<ServiceRequestResponseDto>> GetAllAsync()
        {
            try
            {
                var serviceRequests = await _serviceRequestRepository.GetAllAsync();
                return _mapper.Map<List<ServiceRequestResponseDto>>(serviceRequests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tüm servis talepleri getirilirken hata");
                throw new ServiceException("Servis talepleri getirilemedi", ex);
            }
        }

        /// <summary>
        /// Servis talebini siler
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                _logger.LogInformation("Servis talebi siliniyor: ID {Id}", id);

                var result = await _serviceRequestRepository.DeleteAsync(id);
                
                if (result)
                {
                    _logger.LogInformation("Servis talebi başarıyla silindi: ID {Id}", id);
                }
                else
                {
                    _logger.LogWarning("Silinecek servis talebi bulunamadı: ID {Id}", id);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Servis talebi silinirken hata: ID {Id}", id);
                throw new ServiceException("Servis talebi silinemedi", ex);
            }
        }

        /// <summary>
        /// Belirli duruma göre servis taleplerini getirir
        /// </summary>
        public async Task<List<ServiceRequestResponseDto>> GetByStatusAsync(ServiceStatus status)
        {
            try
            {
                var serviceRequests = await _serviceRequestRepository.GetByStatusAsync(status);
                return _mapper.Map<List<ServiceRequestResponseDto>>(serviceRequests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Durum bazlı servis talepleri getirilirken hata: Status {Status}", status);
                throw new ServiceException("Durum bazlı servis talepleri getirilemedi", ex);
            }
        }

        /// <summary>
        /// Bekleyen servis taleplerini getirir
        /// </summary>
        public async Task<List<ServiceRequestResponseDto>> GetPendingRequestsAsync()
        {
            try
            {
                var serviceRequests = await _serviceRequestRepository.GetPendingRequestsAsync();
                return _mapper.Map<List<ServiceRequestResponseDto>>(serviceRequests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bekleyen servis talepleri getirilirken hata");
                throw new ServiceException("Bekleyen servis talepleri getirilemedi", ex);
            }
        }

        /// <summary>
        /// Süresi geçmiş servis taleplerini getirir
        /// </summary>
        public async Task<List<ServiceRequestResponseDto>> GetOverdueRequestsAsync()
        {
            try
            {
                var serviceRequests = await _serviceRequestRepository.GetOverdueRequestsAsync();
                return _mapper.Map<List<ServiceRequestResponseDto>>(serviceRequests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Süresi geçmiş servis talepleri getirilirken hata");
                throw new ServiceException("Süresi geçmiş servis talepleri getirilemedi", ex);
            }
        }

        /// <summary>
        /// Henüz teknisyen atanmamış servis taleplerini getirir
        /// </summary>
        public async Task<List<ServiceRequestResponseDto>> GetUnassignedRequestsAsync()
        {
            try
            {
                var serviceRequests = await _serviceRequestRepository.GetUnassignedRequestsAsync();
                return _mapper.Map<List<ServiceRequestResponseDto>>(serviceRequests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Atanmamış servis talepleri getirilirken hata");
                throw new ServiceException("Atanmamış servis talepleri getirilemedi", ex);
            }
        }

        /// <summary>
        /// Servis talebine teknisyen atar
        /// </summary>
        public async Task<ServiceRequestResponseDto> AssignTechnicianAsync(int serviceRequestId, int technicianId)
        {
            try
            {
                _logger.LogInformation("Servis talebine teknisyen atanıyor: ServiceRequestID {ServiceRequestId}, TechnicianID {TechnicianId}", 
                    serviceRequestId, technicianId);

                var serviceRequest = await _serviceRequestRepository.GetByIdAsync(serviceRequestId);
                if (serviceRequest == null)
                {
                    throw new NotFoundException($"ID {serviceRequestId} ile servis talebi bulunamadı");
                }

                var technician = await _userRepository.GetTechnicianByIdAsync(technicianId);
                if (technician == null)
                {
                    throw new NotFoundException($"ID {technicianId} ile teknisyen bulunamadı");
                }

                // Tüm teknisyenler müsait kabul edilir

                serviceRequest.AssignedTechnicianId = technicianId;
                serviceRequest.Status = ServiceStatus.InProgress;

                await _serviceRequestRepository.UpdateAsync(serviceRequest);

                // Bildirim gönder
                await _notificationService.SendTechnicianAssignedNotificationAsync(serviceRequestId, technicianId);

                _logger.LogInformation("Teknisyen başarıyla atandı: ServiceRequestID {ServiceRequestId}, TechnicianID {TechnicianId}", 
                    serviceRequestId, technicianId);

                return await GetByIdAsync(serviceRequestId);
            }
            catch (Exception ex) when (!(ex is NotFoundException || ex is BusinessException))
            {
                _logger.LogError(ex, "Teknisyen atanırken hata: ServiceRequestID {ServiceRequestId}, TechnicianID {TechnicianId}", 
                    serviceRequestId, technicianId);
                throw new ServiceException("Teknisyen atanamadı", ex);
            }
        }

        /// <summary>
        /// Servis talebinin durumunu değiştirir
        /// </summary>
        public async Task<ServiceRequestResponseDto> ChangeStatusAsync(int serviceRequestId, ServiceStatus newStatus, string? notes = null)
        {
            try
            {
                _logger.LogInformation("Servis talebi durumu değiştiriliyor: ID {ServiceRequestId}, Yeni Durum: {NewStatus}", 
                    serviceRequestId, newStatus);

                var serviceRequest = await _serviceRequestRepository.GetByIdAsync(serviceRequestId);
                if (serviceRequest == null)
                {
                    throw new NotFoundException($"ID {serviceRequestId} ile servis talebi bulunamadı");
                }

                var oldStatus = serviceRequest.Status;
                serviceRequest.Status = newStatus;

                if (newStatus == ServiceStatus.Completed)
                {
                    serviceRequest.CompletedDate = DateTime.UtcNow;
                }

                await _serviceRequestRepository.UpdateAsync(serviceRequest);

                // Bildirim gönder
                await _notificationService.SendStatusChangeNotificationAsync(serviceRequestId, newStatus);

                _logger.LogInformation("Servis talebi durumu başarıyla değiştirildi: ID {ServiceRequestId}, Eski: {OldStatus}, Yeni: {NewStatus}", 
                    serviceRequestId, oldStatus, newStatus);

                return await GetByIdAsync(serviceRequestId);
            }
            catch (Exception ex) when (!(ex is NotFoundException))
            {
                _logger.LogError(ex, "Servis talebi durumu değiştirilirken hata: ID {ServiceRequestId}", serviceRequestId);
                throw new ServiceException("Servis talebi durumu değiştirilemedi", ex);
            }
        }

        /// <summary>
        /// Servis talebini tamamlar
        /// </summary>
        public async Task<ServiceRequestResponseDto> CompleteServiceRequestAsync(int serviceRequestId, string resolution, decimal? actualCost = null, int? actualHours = null)
        {
            try
            {
                _logger.LogInformation("Servis talebi tamamlanıyor: ID {ServiceRequestId}", serviceRequestId);

                var serviceRequest = await _serviceRequestRepository.GetByIdAsync(serviceRequestId);
                if (serviceRequest == null)
                {
                    throw new NotFoundException($"ID {serviceRequestId} ile servis talebi bulunamadı");
                }

                serviceRequest.Status = ServiceStatus.Completed;
                serviceRequest.CompletedDate = DateTime.UtcNow;
                serviceRequest.Resolution = resolution;
                serviceRequest.ActualCost = actualCost;
                serviceRequest.ActualHours = actualHours;

                await _serviceRequestRepository.UpdateAsync(serviceRequest);

                // Tamamlanma bildirimi gönder
                await _notificationService.SendServiceCompletedNotificationAsync(serviceRequestId);

                // AI rapor analizi yap ve kaydet
                try
                {
                    await _reportService.GenerateAndSaveAiReportAnalysisAsync(serviceRequestId);
                    _logger.LogInformation("AI rapor analizi başarıyla oluşturuldu ve kaydedildi: ID {ServiceRequestId}", serviceRequestId);
                }
                catch (Exception aiEx)
                {
                    _logger.LogError(aiEx, "AI rapor analizi oluşturulurken hata: ID {ServiceRequestId}", serviceRequestId);
                    // AI analizi başarısız olsa bile servis talebi tamamlanmış sayılır
                }

                _logger.LogInformation("Servis talebi başarıyla tamamlandı: ID {ServiceRequestId}", serviceRequestId);

                return await GetByIdAsync(serviceRequestId);
            }
            catch (Exception ex) when (!(ex is NotFoundException))
            {
                _logger.LogError(ex, "Servis talebi tamamlanırken hata: ID {ServiceRequestId}", serviceRequestId);
                throw new ServiceException("Servis talebi tamamlanamadı", ex);
            }
        }

        /// <summary>
        /// Servis talebinde arama yapar
        /// </summary>
        public async Task<List<ServiceRequestResponseDto>> SearchAsync(string searchTerm)
        {
            try
            {
                var serviceRequests = await _serviceRequestRepository.SearchAsync(searchTerm);
                return _mapper.Map<List<ServiceRequestResponseDto>>(serviceRequests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Servis talebi araması yapılırken hata: SearchTerm {SearchTerm}", searchTerm);
                throw new ServiceException("Arama yapılamadı", ex);
            }
        }

        /// <summary>
        /// Filtrelenmiş servis taleplerini getirir
        /// </summary>
        public async Task<List<ServiceRequestResponseDto>> GetFilteredAsync(
            ServiceStatus? status = null,
            Priority? priority = null,
            ServiceCategory? category = null,
            int? customerId = null,
            int? technicianId = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            try
            {
                var serviceRequests = await _serviceRequestRepository.GetFilteredAsync(
                    status, priority, category, customerId, technicianId, startDate, endDate);
                return _mapper.Map<List<ServiceRequestResponseDto>>(serviceRequests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Filtrelenmiş servis talepleri getirilirken hata");
                throw new ServiceException("Filtrelenmiş servis talepleri getirilemedi", ex);
            }
        }

        /// <summary>
        /// Servis talebi istatistiklerini getirir
        /// </summary>
        public async Task<ServiceRequestStatistics> GetStatisticsAsync()
        {
            try
            {
                var statistics = new ServiceRequestStatistics();

                // Temel sayılar
                statistics.TotalRequests = await _serviceRequestRepository.GetTotalCountAsync();
                statistics.PendingRequests = await _serviceRequestRepository.GetCountByStatusAsync(ServiceStatus.Pending);
                statistics.InProgressRequests = await _serviceRequestRepository.GetCountByStatusAsync(ServiceStatus.InProgress);
                statistics.CompletedRequests = await _serviceRequestRepository.GetCountByStatusAsync(ServiceStatus.Completed);
                statistics.CancelledRequests = await _serviceRequestRepository.GetCountByStatusAsync(ServiceStatus.Cancelled);

                // Özel durumlar
                var overdueRequests = await _serviceRequestRepository.GetOverdueRequestsAsync();
                statistics.OverdueRequests = overdueRequests.Count;

                var unassignedRequests = await _serviceRequestRepository.GetUnassignedRequestsAsync();
                statistics.UnassignedRequests = unassignedRequests.Count;

                // Kategori dağılımı
                foreach (ServiceCategory category in Enum.GetValues<ServiceCategory>())
                {
                    var categoryRequests = await _serviceRequestRepository.GetByCategoryAsync(category);
                    statistics.CategoryDistribution[category] = categoryRequests.Count;
                }

                // Öncelik dağılımı
                foreach (Priority priority in Enum.GetValues<Priority>())
                {
                    var priorityRequests = await _serviceRequestRepository.GetByPriorityAsync(priority);
                    statistics.PriorityDistribution[priority] = priorityRequests.Count;
                }

                // Ortalama çözüm süresi hesaplama (basit versiyon)
                var completedRequests = await _serviceRequestRepository.GetByStatusAsync(ServiceStatus.Completed);
                if (completedRequests.Any())
                {
                    var totalHours = completedRequests
                        .Where(sr => sr.CompletedDate.HasValue)
                        .Sum(sr => (sr.CompletedDate!.Value - sr.CreatedDate).TotalHours);
                    statistics.AverageResolutionTime = totalHours / completedRequests.Count;
                }

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Servis talebi istatistikleri hesaplanırken hata");
                throw new ServiceException("İstatistikler hesaplanamadı", ex);
            }
        }
    }

    #region Custom Exceptions

    /// <summary>
    /// Servis katmanı genel hata sınıfı
    /// </summary>
    public class ServiceException : Exception
    {
        public ServiceException(string message) : base(message) { }
        public ServiceException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Kaynak bulunamadı hatası
    /// </summary>
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
    }

    /// <summary>
    /// İş kuralı hatası
    /// </summary>
    public class BusinessException : Exception
    {
        public BusinessException(string message) : base(message) { }
    }

    #endregion
}