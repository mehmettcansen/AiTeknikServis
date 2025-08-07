using AutoMapper;
using AiTeknikServis.Entities.Dtos.WorkAssignment;
using AiTeknikServis.Entities.Dtos.Dashboard;
using AiTeknikServis.Entities.Models;
using AiTeknikServis.Repositories.Contracts;
using AiTeknikServis.Services.Contracts;

namespace AiTeknikServis.Services
{
    public class WorkAssignmentService : IWorkAssignmentService
    {
        private readonly IWorkAssignmentRepository _workAssignmentRepository;
        private readonly IServiceRequestRepository _serviceRequestRepository;
        private readonly IUserRepository _userRepository;
        private readonly IAiPredictionService _aiPredictionService;
        private readonly INotificationService _notificationService;
        private readonly IMapper _mapper;
        private readonly ILogger<WorkAssignmentService> _logger;

        /// <summary>
        /// WorkAssignmentService constructor - Gerekli servisleri enjekte eder
        /// </summary>
        public WorkAssignmentService(
            IWorkAssignmentRepository workAssignmentRepository,
            IServiceRequestRepository serviceRequestRepository,
            IUserRepository userRepository,
            IAiPredictionService aiPredictionService,
            INotificationService notificationService,
            IMapper mapper,
            ILogger<WorkAssignmentService> logger)
        {
            _workAssignmentRepository = workAssignmentRepository;
            _serviceRequestRepository = serviceRequestRepository;
            _userRepository = userRepository;
            _aiPredictionService = aiPredictionService;
            _notificationService = notificationService;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Yeni bir iş ataması oluşturur
        /// </summary>
        public async Task<WorkAssignmentResponseDto> CreateAsync(WorkAssignmentCreateDto dto)
        {
            try
            {
                _logger.LogInformation("Yeni iş ataması oluşturuluyor: ServiceRequestID {ServiceRequestId}, TechnicianID {TechnicianId}", 
                    dto.ServiceRequestId, dto.TechnicianId);

                // Validasyonlar
                await ValidateAssignmentAsync(dto.ServiceRequestId, dto.TechnicianId, dto.ScheduledDate);

                // DTO'yu entity'ye dönüştür
                var workAssignment = _mapper.Map<WorkAssignment>(dto);
                workAssignment.AssignedDate = DateTime.UtcNow;

                // İş atamasını kaydet
                var createdAssignment = await _workAssignmentRepository.CreateAsync(workAssignment);

                // Servis talebinin durumunu güncelle
                await UpdateServiceRequestStatusAsync(dto.ServiceRequestId, dto.TechnicianId);

                // Bildirim gönder
                await _notificationService.SendTechnicianAssignedNotificationAsync(dto.ServiceRequestId, dto.TechnicianId);

                _logger.LogInformation("İş ataması başarıyla oluşturuldu: ID {Id}", createdAssignment.Id);

                return await GetByIdAsync(createdAssignment.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İş ataması oluşturulurken hata: ServiceRequestID {ServiceRequestId}, TechnicianID {TechnicianId}", 
                    dto.ServiceRequestId, dto.TechnicianId);
                throw new ServiceException("İş ataması oluşturulamadı", ex);
            }
        }

        /// <summary>
        /// Otomatik görev ataması yapar (AI destekli)
        /// </summary>
        public async Task<WorkAssignmentResponseDto> AutoAssignAsync(int serviceRequestId)
        {
            try
            {
                _logger.LogInformation("Otomatik görev ataması başlatılıyor: ServiceRequestID {ServiceRequestId}", serviceRequestId);

                var serviceRequest = await _serviceRequestRepository.GetByIdAsync(serviceRequestId);
                if (serviceRequest == null)
                {
                    throw new NotFoundException($"ID {serviceRequestId} ile servis talebi bulunamadı");
                }

                // AI'dan teknisyen önerisi al
                var suggestedTechnicianIds = await _aiPredictionService.SuggestTechniciansAsync(
                    serviceRequest.Category, serviceRequest.Priority);

                if (!suggestedTechnicianIds.Any())
                {
                    // AI önerisi yoksa en uygun teknisyeni bul
                    var bestTechnicianId = await FindBestTechnicianAsync(
                        serviceRequest.Category, serviceRequest.Priority);
                    
                    if (!bestTechnicianId.HasValue)
                    {
                        throw new BusinessException("Uygun teknisyen bulunamadı");
                    }
                    
                    suggestedTechnicianIds = new List<int> { bestTechnicianId.Value };
                }

                // İlk uygun teknisyeni seç
                int selectedTechnicianId = 0;
                foreach (var technicianId in suggestedTechnicianIds)
                {
                    if (await IsTechnicianAvailableAsync(technicianId, DateTime.UtcNow))
                    {
                        selectedTechnicianId = technicianId;
                        break;
                    }
                }

                if (selectedTechnicianId == 0)
                {
                    throw new BusinessException("Önerilen teknisyenler müsait değil");
                }

                // Otomatik atama yap
                var assignmentDto = new WorkAssignmentCreateDto
                {
                    ServiceRequestId = serviceRequestId,
                    TechnicianId = selectedTechnicianId,
                    Status = WorkAssignmentStatus.Assigned,
                    Notes = "Otomatik atama (AI destekli)"
                };

                var result = await CreateAsync(assignmentDto);

                _logger.LogInformation("Otomatik görev ataması başarılı: ServiceRequestID {ServiceRequestId}, TechnicianID {TechnicianId}", 
                    serviceRequestId, selectedTechnicianId);

                return result;
            }
            catch (Exception ex) when (!(ex is NotFoundException || ex is BusinessException))
            {
                _logger.LogError(ex, "Otomatik görev ataması yapılırken hata: ServiceRequestID {ServiceRequestId}", serviceRequestId);
                throw new ServiceException("Otomatik görev ataması yapılamadı", ex);
            }
        }

        /// <summary>
        /// Manuel görev ataması yapar
        /// </summary>
        public async Task<WorkAssignmentResponseDto> ManualAssignAsync(int serviceRequestId, int technicianId, DateTime? scheduledDate = null, string? notes = null)
        {
            try
            {
                _logger.LogInformation("Manuel görev ataması yapılıyor: ServiceRequestID {ServiceRequestId}, TechnicianID {TechnicianId}", 
                    serviceRequestId, technicianId);

                var assignmentDto = new WorkAssignmentCreateDto
                {
                    ServiceRequestId = serviceRequestId,
                    TechnicianId = technicianId,
                    ScheduledDate = scheduledDate,
                    Status = WorkAssignmentStatus.Assigned,
                    Notes = notes ?? "Manuel atama"
                };

                return await CreateAsync(assignmentDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Manuel görev ataması yapılırken hata: ServiceRequestID {ServiceRequestId}, TechnicianID {TechnicianId}", 
                    serviceRequestId, technicianId);
                throw;
            }
        }

        /// <summary>
        /// ID'ye göre iş atamasını getirir
        /// </summary>
        public async Task<WorkAssignmentResponseDto> GetByIdAsync(int id)
        {
            try
            {
                var workAssignment = await _workAssignmentRepository.GetByIdWithDetailsAsync(id);
                if (workAssignment == null)
                {
                    throw new NotFoundException($"ID {id} ile iş ataması bulunamadı");
                }

                return _mapper.Map<WorkAssignmentResponseDto>(workAssignment);
            }
            catch (Exception ex) when (!(ex is NotFoundException))
            {
                _logger.LogError(ex, "İş ataması getirilirken hata: ID {Id}", id);
                throw new ServiceException("İş ataması getirilemedi", ex);
            }
        }

        /// <summary>
        /// Belirli bir teknisyenin iş atamalarını getirir
        /// </summary>
        public async Task<List<WorkAssignmentResponseDto>> GetByTechnicianAsync(int technicianId)
        {
            try
            {
                var workAssignments = await _workAssignmentRepository.GetByTechnicianIdAsync(technicianId);
                return _mapper.Map<List<WorkAssignmentResponseDto>>(workAssignments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Teknisyen iş atamaları getirilirken hata: TechnicianID {TechnicianId}", technicianId);
                throw new ServiceException("Teknisyen iş atamaları getirilemedi", ex);
            }
        }

        /// <summary>
        /// Belirli bir servis talebinin iş atamalarını getirir
        /// </summary>
        public async Task<List<WorkAssignmentResponseDto>> GetByServiceRequestAsync(int serviceRequestId)
        {
            try
            {
                var workAssignments = await _workAssignmentRepository.GetByServiceRequestIdAsync(serviceRequestId);
                return _mapper.Map<List<WorkAssignmentResponseDto>>(workAssignments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Servis talebi iş atamaları getirilirken hata: ServiceRequestID {ServiceRequestId}", serviceRequestId);
                throw new ServiceException("Servis talebi iş atamaları getirilemedi", ex);
            }
        }

        /// <summary>
        /// Teknisyenin aktif iş atamalarını getirir
        /// </summary>
        public async Task<List<WorkAssignmentResponseDto>> GetActiveTechnicianAssignmentsAsync(int technicianId)
        {
            try
            {
                var workAssignments = await _workAssignmentRepository.GetActiveTechnicianAssignmentsAsync(technicianId);
                return _mapper.Map<List<WorkAssignmentResponseDto>>(workAssignments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Teknisyen aktif iş atamaları getirilirken hata: TechnicianID {TechnicianId}", technicianId);
                throw new ServiceException("Teknisyen aktif iş atamaları getirilemedi", ex);
            }
        }

        /// <summary>
        /// Belirli bir tarihteki iş atamalarını getirir
        /// </summary>
        public async Task<List<WorkAssignmentResponseDto>> GetScheduledAssignmentsAsync(DateTime date)
        {
            try
            {
                var workAssignments = await _workAssignmentRepository.GetScheduledAssignmentsAsync(date);
                return _mapper.Map<List<WorkAssignmentResponseDto>>(workAssignments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Planlı iş atamaları getirilirken hata: Date {Date}", date);
                throw new ServiceException("Planlı iş atamaları getirilemedi", ex);
            }
        }

        /// <summary>
        /// Süresi geçmiş iş atamalarını getirir
        /// </summary>
        public async Task<List<WorkAssignmentResponseDto>> GetOverdueAssignmentsAsync()
        {
            try
            {
                var workAssignments = await _workAssignmentRepository.GetOverdueAssignmentsAsync();
                return _mapper.Map<List<WorkAssignmentResponseDto>>(workAssignments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Süresi geçmiş iş atamaları getirilirken hata");
                throw new ServiceException("Süresi geçmiş iş atamaları getirilemedi", ex);
            }
        }

        /// <summary>
        /// İş atamasını başlatır
        /// </summary>
        public async Task<WorkAssignmentResponseDto> StartAssignmentAsync(int assignmentId)
        {
            try
            {
                _logger.LogInformation("İş ataması başlatılıyor: ID {AssignmentId}", assignmentId);

                var workAssignment = await _workAssignmentRepository.GetByIdAsync(assignmentId);
                if (workAssignment == null)
                {
                    throw new NotFoundException($"ID {assignmentId} ile iş ataması bulunamadı");
                }

                if (workAssignment.Status != WorkAssignmentStatus.Assigned)
                {
                    throw new BusinessException("Sadece atanmış görevler başlatılabilir");
                }

                workAssignment.Status = WorkAssignmentStatus.InProgress;
                workAssignment.StartedDate = DateTime.UtcNow;

                await _workAssignmentRepository.UpdateAsync(workAssignment);

                _logger.LogInformation("İş ataması başarıyla başlatıldı: ID {AssignmentId}", assignmentId);

                return await GetByIdAsync(assignmentId);
            }
            catch (Exception ex) when (!(ex is NotFoundException || ex is BusinessException))
            {
                _logger.LogError(ex, "İş ataması başlatılırken hata: ID {AssignmentId}", assignmentId);
                throw new ServiceException("İş ataması başlatılamadı", ex);
            }
        }

        /// <summary>
        /// İş atamasını tamamlar
        /// </summary>
        public async Task<WorkAssignmentResponseDto> CompleteAssignmentAsync(int assignmentId, string? completionNotes = null, int? actualHours = null)
        {
            try
            {
                _logger.LogInformation("İş ataması tamamlanıyor: ID {AssignmentId}", assignmentId);

                var workAssignment = await _workAssignmentRepository.GetByIdAsync(assignmentId);
                if (workAssignment == null)
                {
                    throw new NotFoundException($"ID {assignmentId} ile iş ataması bulunamadı");
                }

                if (workAssignment.Status == WorkAssignmentStatus.Completed)
                {
                    throw new BusinessException("İş ataması zaten tamamlanmış");
                }

                workAssignment.Status = WorkAssignmentStatus.Completed;
                workAssignment.CompletedDate = DateTime.UtcNow;
                workAssignment.CompletionNotes = completionNotes;
                workAssignment.ActualHours = actualHours;

                await _workAssignmentRepository.UpdateAsync(workAssignment);

                // Servis talebinin durumunu kontrol et ve güncelle
                await CheckAndUpdateServiceRequestStatusAsync(workAssignment.ServiceRequestId);

                _logger.LogInformation("İş ataması başarıyla tamamlandı: ID {AssignmentId}", assignmentId);

                return await GetByIdAsync(assignmentId);
            }
            catch (Exception ex) when (!(ex is NotFoundException || ex is BusinessException))
            {
                _logger.LogError(ex, "İş ataması tamamlanırken hata: ID {AssignmentId}", assignmentId);
                throw new ServiceException("İş ataması tamamlanamadı", ex);
            }
        }

        /// <summary>
        /// İş atamasını iptal eder
        /// </summary>
        public async Task<WorkAssignmentResponseDto> CancelAssignmentAsync(int assignmentId, string? reason = null)
        {
            try
            {
                _logger.LogInformation("İş ataması iptal ediliyor: ID {AssignmentId}", assignmentId);

                var workAssignment = await _workAssignmentRepository.GetByIdAsync(assignmentId);
                if (workAssignment == null)
                {
                    throw new NotFoundException($"ID {assignmentId} ile iş ataması bulunamadı");
                }

                if (workAssignment.Status == WorkAssignmentStatus.Completed)
                {
                    throw new BusinessException("Tamamlanmış iş ataması iptal edilemez");
                }

                workAssignment.Status = WorkAssignmentStatus.Cancelled;
                workAssignment.Notes = $"{workAssignment.Notes} | İptal nedeni: {reason ?? "Belirtilmedi"}";

                await _workAssignmentRepository.UpdateAsync(workAssignment);

                _logger.LogInformation("İş ataması başarıyla iptal edildi: ID {AssignmentId}", assignmentId);

                return await GetByIdAsync(assignmentId);
            }
            catch (Exception ex) when (!(ex is NotFoundException || ex is BusinessException))
            {
                _logger.LogError(ex, "İş ataması iptal edilirken hata: ID {AssignmentId}", assignmentId);
                throw new ServiceException("İş ataması iptal edilemedi", ex);
            }
        }

        /// <summary>
        /// İş atamasını yeniden atar
        /// </summary>
        public async Task<WorkAssignmentResponseDto> ReassignAsync(int assignmentId, int newTechnicianId, string? reason = null)
        {
            try
            {
                _logger.LogInformation("İş ataması yeniden atanıyor: ID {AssignmentId}, Yeni TechnicianID {NewTechnicianId}", 
                    assignmentId, newTechnicianId);

                // Mevcut atamayı iptal et
                await CancelAssignmentAsync(assignmentId, $"Yeniden atama: {reason}");

                // Mevcut atamanın bilgilerini al
                var oldAssignment = await _workAssignmentRepository.GetByIdAsync(assignmentId);
                
                // Yeni atama oluştur
                var newAssignmentDto = new WorkAssignmentCreateDto
                {
                    ServiceRequestId = oldAssignment!.ServiceRequestId,
                    TechnicianId = newTechnicianId,
                    ScheduledDate = oldAssignment.ScheduledDate,
                    Notes = $"Yeniden atama - Önceki atama ID: {assignmentId}. Neden: {reason ?? "Belirtilmedi"}"
                };

                var newAssignment = await CreateAsync(newAssignmentDto);

                _logger.LogInformation("İş ataması başarıyla yeniden atandı: Eski ID {AssignmentId}, Yeni ID {NewAssignmentId}", 
                    assignmentId, newAssignment.Id);

                return newAssignment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İş ataması yeniden atanırken hata: ID {AssignmentId}, Yeni TechnicianID {NewTechnicianId}", 
                    assignmentId, newTechnicianId);
                throw new ServiceException("İş ataması yeniden atanamadı", ex);
            }
        }

        /// <summary>
        /// Teknisyenin müsaitlik durumunu kontrol eder
        /// </summary>
        public async Task<bool> IsTechnicianAvailableAsync(int technicianId, DateTime scheduledDate)
        {
            try
            {
                return await _workAssignmentRepository.IsTechnicianAvailableAsync(technicianId, scheduledDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Teknisyen müsaitlik kontrolü yapılırken hata: TechnicianID {TechnicianId}", technicianId);
                return false;
            }
        }

        /// <summary>
        /// En uygun teknisyeni bulur
        /// </summary>
        public async Task<int?> FindBestTechnicianAsync(ServiceCategory category, Priority priority, DateTime? scheduledDate = null)
        {
            try
            {
                var availableTechnicians = await _userRepository.GetAvailableTechniciansAsync();
                
                if (!availableTechnicians.Any())
                {
                    return null;
                }

                // Kategori bazında filtreleme
                var categoryKeyword = GetCategoryKeyword(category);
                var suitableTechnicians = availableTechnicians
                    .Where(t => string.IsNullOrEmpty(categoryKeyword) || 
                               (t.Specializations != null && t.Specializations.ToLower().Contains(categoryKeyword)))
                    .ToList();

                if (!suitableTechnicians.Any())
                {
                    suitableTechnicians = availableTechnicians; // Kategori uyuşmazlığında tüm teknisyenler
                }

                // İş yükü bazında sıralama
                var technicianWorkloads = await GetTechnicianWorkloadAsync();
                var bestTechnician = suitableTechnicians
                    .Where(t => !scheduledDate.HasValue || IsTechnicianAvailableAsync(t.Id, scheduledDate.Value).Result)
                    .OrderBy(t => technicianWorkloads.ContainsKey(t.Id) ? technicianWorkloads[t.Id].WorkloadPercentage : 0)
                    .FirstOrDefault();

                return bestTechnician?.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "En uygun teknisyen bulunurken hata: Category {Category}, Priority {Priority}", category, priority);
                return null;
            }
        }

        /// <summary>
        /// Teknisyen iş yükü dağılımını getirir
        /// </summary>
        public async Task<Dictionary<int, TechnicianWorkload>> GetTechnicianWorkloadAsync()
        {
            try
            {
                var technicians = await _userRepository.GetAllTechniciansAsync();
                var workloadSummary = await _workAssignmentRepository.GetTechnicianWorkloadSummaryAsync();
                var result = new Dictionary<int, TechnicianWorkload>();

                foreach (var technician in technicians)
                {
                    var activeAssignments = workloadSummary.ContainsKey(technician.Id) ? workloadSummary[technician.Id] : 0;
                    const int maxConcurrentAssignments = 5; // Varsayılan değer
                    var workloadPercentage = maxConcurrentAssignments > 0 
                        ? (double)activeAssignments / maxConcurrentAssignments * 100 
                        : 0;

                    result[technician.Id] = new TechnicianWorkload
                    {
                        TechnicianId = technician.Id,
                        TechnicianName = $"{technician.FirstName} {technician.LastName}",
                        ActiveAssignments = activeAssignments,
                        MaxConcurrentAssignments = maxConcurrentAssignments,
                        WorkloadPercentage = workloadPercentage,
                        IsAvailable = true, // Tüm teknisyenler müsait kabul edilir
                        Specializations = technician.GetSpecializationsList(),
                        AverageCompletionTime = await _workAssignmentRepository.GetTechnicianAverageCompletionTimeAsync(technician.Id)
                    };
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Teknisyen iş yükü dağılımı hesaplanırken hata");
                throw new ServiceException("Teknisyen iş yükü dağılımı hesaplanamadı", ex);
            }
        }

        /// <summary>
        /// İş ataması performans metriklerini getirir
        /// </summary>
        public async Task<WorkAssignmentPerformanceMetrics> GetPerformanceMetricsAsync(int? technicianId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var metrics = new WorkAssignmentPerformanceMetrics();

                // Temel metrikler
                metrics.TotalAssignments = await _workAssignmentRepository.GetTotalAssignmentCountAsync();
                metrics.CompletedAssignments = await _workAssignmentRepository.GetAssignmentCountByStatusAsync(WorkAssignmentStatus.Completed);
                metrics.CancelledAssignments = await _workAssignmentRepository.GetAssignmentCountByStatusAsync(WorkAssignmentStatus.Cancelled);
                
                var overdueAssignments = await _workAssignmentRepository.GetOverdueAssignmentsAsync();
                metrics.OverdueAssignments = overdueAssignments.Count;

                // Oranlar
                metrics.CompletionRate = metrics.TotalAssignments > 0 
                    ? (double)metrics.CompletedAssignments / metrics.TotalAssignments * 100 
                    : 0;

                // Ortalama tamamlama süresi
                metrics.AverageCompletionTime = technicianId.HasValue 
                    ? await _workAssignmentRepository.GetTechnicianAverageCompletionTimeAsync(technicianId.Value)
                    : await _workAssignmentRepository.GetAverageCompletionTimeAsync();

                // Zamanında tamamlama oranı (basit hesaplama)
                var completedOnTime = metrics.CompletedAssignments - metrics.OverdueAssignments;
                metrics.OnTimeCompletionRate = metrics.CompletedAssignments > 0 
                    ? (double)completedOnTime / metrics.CompletedAssignments * 100 
                    : 0;

                // Teknisyen bazında performans
                if (!technicianId.HasValue)
                {
                    var technicians = await _userRepository.GetAllTechniciansAsync();
                    foreach (var technician in technicians)
                    {
                        var techCompletedAssignments = await _workAssignmentRepository.GetCompletedAssignmentsByTechnicianAsync(
                            technician.Id, startDate, endDate);
                        
                        var techPerformance = new TechnicianPerformance
                        {
                            TechnicianId = technician.Id,
                            TechnicianName = $"{technician.FirstName} {technician.LastName}",
                            CompletedTasks = techCompletedAssignments.Count,
                            AverageCompletionTime = await _workAssignmentRepository.GetTechnicianAverageCompletionTimeAsync(technician.Id),
                            OnTimeRate = 85.0, // Basit hesaplama - gerçek implementasyonda daha detaylı olacak
                            PerformanceScore = CalculatePerformanceScore(techCompletedAssignments.Count, 85.0)
                        };

                        metrics.TechnicianPerformances[technician.Id] = techPerformance;
                    }
                }

                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İş ataması performans metrikleri hesaplanırken hata");
                throw new ServiceException("Performans metrikleri hesaplanamadı", ex);
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// İş ataması validasyonlarını yapar
        /// </summary>
        private async Task ValidateAssignmentAsync(int serviceRequestId, int technicianId, DateTime? scheduledDate)
        {
            // Servis talebi kontrolü
            var serviceRequest = await _serviceRequestRepository.GetByIdAsync(serviceRequestId);
            if (serviceRequest == null)
            {
                throw new NotFoundException($"ID {serviceRequestId} ile servis talebi bulunamadı");
            }

            // Teknisyen kontrolü
            var technician = await _userRepository.GetTechnicianByIdAsync(technicianId);
            if (technician == null)
            {
                throw new NotFoundException($"ID {technicianId} ile teknisyen bulunamadı");
            }

            // Tüm teknisyenler müsait kabul edilir

            // Tarih kontrolü
            if (scheduledDate.HasValue && !await IsTechnicianAvailableAsync(technicianId, scheduledDate.Value))
            {
                throw new BusinessException($"Teknisyen {scheduledDate:dd.MM.yyyy HH:mm} tarihinde müsait değil");
            }
        }

        /// <summary>
        /// Servis talebinin durumunu günceller
        /// </summary>
        private async Task UpdateServiceRequestStatusAsync(int serviceRequestId, int technicianId)
        {
            var serviceRequest = await _serviceRequestRepository.GetByIdAsync(serviceRequestId);
            if (serviceRequest != null)
            {
                serviceRequest.AssignedTechnicianId = technicianId;
                if (serviceRequest.Status == ServiceStatus.Pending)
                {
                    serviceRequest.Status = ServiceStatus.InProgress;
                }
                await _serviceRequestRepository.UpdateAsync(serviceRequest);
            }
        }

        /// <summary>
        /// Servis talebinin durumunu kontrol eder ve günceller
        /// </summary>
        private async Task CheckAndUpdateServiceRequestStatusAsync(int serviceRequestId)
        {
            var assignments = await _workAssignmentRepository.GetByServiceRequestIdAsync(serviceRequestId);
            var allCompleted = assignments.All(a => a.Status == WorkAssignmentStatus.Completed || a.Status == WorkAssignmentStatus.Cancelled);
            
            if (allCompleted && assignments.Any(a => a.Status == WorkAssignmentStatus.Completed))
            {
                var serviceRequest = await _serviceRequestRepository.GetByIdAsync(serviceRequestId);
                if (serviceRequest != null && serviceRequest.Status != ServiceStatus.Completed)
                {
                    serviceRequest.Status = ServiceStatus.Completed;
                    serviceRequest.CompletedDate = DateTime.UtcNow;
                    await _serviceRequestRepository.UpdateAsync(serviceRequest);
                    
                    // Tamamlanma bildirimi gönder
                    await _notificationService.SendServiceCompletedNotificationAsync(serviceRequestId);
                }
            }
        }

        /// <summary>
        /// Kategori için anahtar kelime döndürür
        /// </summary>
        private string GetCategoryKeyword(ServiceCategory category)
        {
            return category switch
            {
                ServiceCategory.SoftwareIssue => "yazılım",
                ServiceCategory.HardwareIssue => "donanım",
                ServiceCategory.NetworkIssue => "ağ",
                ServiceCategory.SecurityIssue => "güvenlik",
                ServiceCategory.Maintenance => "bakım",
                _ => ""
            };
        }

        /// <summary>
        /// Detaylı teknisyen iş yükü bilgilerini getirir
        /// </summary>
        public async Task<List<TechnicianWorkloadDto>> GetDetailedTechnicianWorkloadAsync()
        {
            try
            {
                var technicians = await _userRepository.GetAllTechniciansAsync();
                var result = new List<TechnicianWorkloadDto>();

                foreach (var technician in technicians)
                {
                    var assignments = await _workAssignmentRepository.GetByTechnicianIdAsync(technician.Id);
                    
                    var totalAssignments = assignments.Count;
                    var pendingAssignments = assignments.Count(a => a.Status == WorkAssignmentStatus.Assigned);
                    var inProgressAssignments = assignments.Count(a => a.Status == WorkAssignmentStatus.InProgress);
                    var completedAssignments = assignments.Count(a => a.Status == WorkAssignmentStatus.Completed);
                    var cancelledAssignments = assignments.Count(a => a.Status == WorkAssignmentStatus.Cancelled);
                    
                    var completionRate = totalAssignments > 0 ? (double)completedAssignments / totalAssignments * 100 : 0;
                    
                    // Ortalama tamamlama süresi hesaplama
                    var completedWithDates = assignments.Where(a => a.Status == WorkAssignmentStatus.Completed && a.CompletedDate.HasValue).ToList();
                    var averageCompletionDays = completedWithDates.Any() 
                        ? completedWithDates.Average(a => (a.CompletedDate!.Value - a.AssignedDate).TotalDays)
                        : 0;

                    // Süresi geçmiş görevler
                    var overdueAssignments = assignments.Count(a => 
                        (a.Status == WorkAssignmentStatus.Assigned || a.Status == WorkAssignmentStatus.InProgress) &&
                        a.ScheduledDate.HasValue && a.ScheduledDate.Value < DateTime.UtcNow);

                    // İş yükü durumu belirleme
                    var activeAssignments = pendingAssignments + inProgressAssignments;
                    var workloadStatus = DetermineWorkloadStatus(activeAssignments);

                    // Son görevler
                    var recentAssignments = assignments
                        .OrderByDescending(a => a.AssignedDate)
                        .Take(5)
                        .Select(a => new RecentAssignmentDto
                        {
                            AssignmentId = a.Id,
                            ServiceRequestId = a.ServiceRequestId,
                            ServiceRequestTitle = a.ServiceRequest?.Title ?? $"Talep #{a.ServiceRequestId}",
                            Status = GetStatusText(a.Status),
                            StatusColor = GetStatusColor(a.Status),
                            AssignedDate = a.AssignedDate,
                            CompletedDate = a.CompletedDate,
                            DaysInProgress = (int)(DateTime.UtcNow - a.AssignedDate).TotalDays,
                            IsOverdue = a.ScheduledDate.HasValue && a.ScheduledDate.Value < DateTime.UtcNow && 
                                       a.Status != WorkAssignmentStatus.Completed
                        }).ToList();

                    var workloadDto = new TechnicianWorkloadDto
                    {
                        TechnicianId = technician.Id,
                        TechnicianName = $"{technician.FirstName} {technician.LastName}",
                        Email = technician.Email,
                        Specializations = technician.Specializations,
                        IsActive = technician.IsActive,
                        TotalAssignments = totalAssignments,
                        PendingAssignments = pendingAssignments,
                        InProgressAssignments = inProgressAssignments,
                        CompletedAssignments = completedAssignments,
                        CancelledAssignments = cancelledAssignments,
                        CompletionRate = completionRate,
                        AverageCompletionDays = averageCompletionDays,
                        OverdueAssignments = overdueAssignments,
                        LastAssignmentDate = assignments.Any() ? assignments.Max(a => a.AssignedDate) : null,
                        LastCompletionDate = completedWithDates.Any() ? completedWithDates.Max(a => a.CompletedDate) : null,
                        WorkloadStatus = workloadStatus,
                        WorkloadStatusText = GetWorkloadStatusText(workloadStatus),
                        WorkloadStatusColor = GetWorkloadStatusColor(workloadStatus),
                        RecentAssignments = recentAssignments
                    };

                    result.Add(workloadDto);
                }

                return result.OrderBy(t => t.TechnicianName).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Detaylı teknisyen iş yükü bilgileri alınırken hata");
                throw new ServiceException("Teknisyen iş yükü bilgileri alınamadı", ex);
            }
        }

        /// <summary>
        /// İş yükü durumunu belirler
        /// </summary>
        private WorkloadStatus DetermineWorkloadStatus(int activeAssignments)
        {
            return activeAssignments switch
            {
                0 => WorkloadStatus.Available,
                <= 2 => WorkloadStatus.Normal,
                <= 4 => WorkloadStatus.Busy,
                _ => WorkloadStatus.Overloaded
            };
        }

        /// <summary>
        /// İş yükü durumu metnini döndürür
        /// </summary>
        private string GetWorkloadStatusText(WorkloadStatus status)
        {
            return status switch
            {
                WorkloadStatus.Available => "Müsait",
                WorkloadStatus.Normal => "Normal",
                WorkloadStatus.Busy => "Yoğun",
                WorkloadStatus.Overloaded => "Aşırı Yüklü",
                _ => "Bilinmiyor"
            };
        }

        /// <summary>
        /// İş yükü durumu rengini döndürür
        /// </summary>
        private string GetWorkloadStatusColor(WorkloadStatus status)
        {
            return status switch
            {
                WorkloadStatus.Available => "success",
                WorkloadStatus.Normal => "info",
                WorkloadStatus.Busy => "warning",
                WorkloadStatus.Overloaded => "danger",
                _ => "secondary"
            };
        }

        /// <summary>
        /// Durum metnini döndürür
        /// </summary>
        private string GetStatusText(WorkAssignmentStatus status)
        {
            return status switch
            {
                WorkAssignmentStatus.Assigned => "Atandı",
                WorkAssignmentStatus.InProgress => "İşlemde",
                WorkAssignmentStatus.Completed => "Tamamlandı",
                WorkAssignmentStatus.Cancelled => "İptal",
                _ => status.ToString()
            };
        }

        /// <summary>
        /// Durum rengini döndürür
        /// </summary>
        private string GetStatusColor(WorkAssignmentStatus status)
        {
            return status switch
            {
                WorkAssignmentStatus.Assigned => "warning",
                WorkAssignmentStatus.InProgress => "primary",
                WorkAssignmentStatus.Completed => "success",
                WorkAssignmentStatus.Cancelled => "danger",
                _ => "secondary"
            };
        }

        /// <summary>
        /// Performans skoru hesaplar
        /// </summary>
        private double CalculatePerformanceScore(int completedTasks, double onTimeRate)
        {
            // Basit performans skoru hesaplama
            var taskScore = Math.Min(completedTasks * 0.1, 5.0); // Maksimum 5 puan
            var timeScore = onTimeRate / 100 * 5.0; // Maksimum 5 puan
            return Math.Round(taskScore + timeScore, 1);
        }

        /// <summary>
        /// Belirli bir teknisyenin detaylı iş yükü bilgilerini getirir
        /// </summary>
        public async Task<TechnicianWorkloadDto?> GetTechnicianWorkloadDetailsAsync(int technicianId)
        {
            try
            {
                var technician = await _userRepository.GetTechnicianByIdAsync(technicianId);
                if (technician == null)
                    return null;

                var assignments = await _workAssignmentRepository.GetByTechnicianIdAsync(technicianId);
                
                var pendingAssignments = assignments.Count(a => a.Status == WorkAssignmentStatus.Assigned);
                var inProgressAssignments = assignments.Count(a => a.Status == WorkAssignmentStatus.InProgress);
                var completedAssignments = assignments.Count(a => a.Status == WorkAssignmentStatus.Completed);
                var overdueAssignments = assignments.Count(a => 
                    (a.Status == WorkAssignmentStatus.Assigned || a.Status == WorkAssignmentStatus.InProgress) &&
                    a.ScheduledDate.HasValue && a.ScheduledDate.Value < DateTime.Now);

                var totalAssignments = assignments.Count;
                var completionRate = totalAssignments > 0 ? (double)completedAssignments / totalAssignments * 100 : 0;

                var completedTasks = assignments.Where(a => a.Status == WorkAssignmentStatus.Completed && a.CompletedDate.HasValue);
                var averageCompletionDays = completedTasks.Any() 
                    ? completedTasks.Average(a => (a.CompletedDate!.Value - a.AssignedDate).TotalDays)
                    : 0;

                var activeAssignments = pendingAssignments + inProgressAssignments;
                var workloadStatus = DetermineWorkloadStatus(activeAssignments);

                // Son görevleri al
                var recentAssignments = assignments
                    .OrderByDescending(a => a.AssignedDate)
                    .Take(10)
                    .Select(a => new RecentAssignmentDto
                    {
                        AssignmentId = a.Id,
                        ServiceRequestId = a.ServiceRequestId,
                        ServiceRequestTitle = a.ServiceRequest?.Title ?? "Bilinmiyor",
                        Status = GetStatusText(a.Status),
                        StatusColor = GetStatusColor(a.Status),
                        AssignedDate = a.AssignedDate,
                        CompletedDate = a.CompletedDate,
                        DaysInProgress = (int)(DateTime.Now - a.AssignedDate).TotalDays,
                        IsOverdue = a.ScheduledDate.HasValue && a.ScheduledDate.Value < DateTime.Now && 
                                   (a.Status == WorkAssignmentStatus.Assigned || a.Status == WorkAssignmentStatus.InProgress)
                    }).ToList();

                return new TechnicianWorkloadDto
                {
                    TechnicianId = technician.Id,
                    TechnicianName = $"{technician.FirstName} {technician.LastName}",
                    Email = technician.Email,
                    Specializations = technician.Specializations,
                    TotalAssignments = totalAssignments,
                    PendingAssignments = pendingAssignments,
                    InProgressAssignments = inProgressAssignments,
                    CompletedAssignments = completedAssignments,
                    CompletionRate = completionRate,
                    AverageCompletionDays = averageCompletionDays,
                    OverdueAssignments = overdueAssignments,
                    LastAssignmentDate = assignments.OrderByDescending(a => a.AssignedDate).FirstOrDefault()?.AssignedDate,
                    LastCompletionDate = assignments.Where(a => a.CompletedDate.HasValue).OrderByDescending(a => a.CompletedDate).FirstOrDefault()?.CompletedDate,
                    WorkloadStatus = workloadStatus,
                    WorkloadStatusText = GetWorkloadStatusText(workloadStatus),
                    WorkloadStatusColor = GetWorkloadStatusColor(workloadStatus),
                    RecentAssignments = recentAssignments
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Teknisyen iş yükü detayları alınırken hata: {TechnicianId}", technicianId);
                return null;
            }
        }

        #endregion
    }
}