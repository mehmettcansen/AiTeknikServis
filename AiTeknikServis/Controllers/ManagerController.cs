using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AutoMapper;
using AiTeknikServis.Services.Contracts;
using AiTeknikServis.Repositories.Contracts;
using AiTeknikServis.Entities.Dtos.ServiceRequest;
using AiTeknikServis.Entities.Dtos.WorkAssignment;
using AiTeknikServis.Entities.Dtos.Dashboard;
using AiTeknikServis.Entities.Models;
using System.Security.Claims;

namespace AiTeknikServis.Controllers
{
    [Authorize(Roles = "Manager,Admin")]
    [Route("[controller]")]
    public class ManagerController : Controller
    {
        private readonly IServiceRequestService _serviceRequestService;
        private readonly IWorkAssignmentService _workAssignmentService;
        private readonly INotificationService _notificationService;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<ManagerController> _logger;

        public ManagerController(
            IServiceRequestService serviceRequestService,
            IWorkAssignmentService workAssignmentService,
            INotificationService notificationService,
            IUserRepository userRepository,
            IMapper mapper,
            ILogger<ManagerController> logger)
        {
            _serviceRequestService = serviceRequestService;
            _workAssignmentService = workAssignmentService;
            _notificationService = notificationService;
            _userRepository = userRepository;
            _mapper = mapper;
            _logger = logger;
        }

        // GET: Manager/Dashboard
        [HttpGet("Dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var allRequests = await _serviceRequestService.GetAllAsync();
                var allAssignments = await _workAssignmentService.GetActiveTechnicianAssignmentsAsync(0); // Tüm aktif atamaları al
                var technicianWorkload = await _workAssignmentService.GetDetailedTechnicianWorkloadAsync();

                var dashboardData = new ManagerDashboardDto
                {
                    TotalRequests = allRequests.Count,
                    PendingRequests = allRequests.Count(r => r.Status == ServiceStatus.Pending),
                    InProgressRequests = allRequests.Count(r => r.Status == ServiceStatus.InProgress),
                    CompletedRequests = allRequests.Count(r => r.Status == ServiceStatus.Completed),
                    RecentRequests = _mapper.Map<List<ServiceRequestResponseDto>>(allRequests.OrderByDescending(r => r.CreatedDate).Take(10)),
                    UrgentRequests = _mapper.Map<List<ServiceRequestResponseDto>>(allRequests.Where(r => r.Priority == Priority.High || r.Priority == Priority.Critical).Take(5)),
                    
                    // Kategori verileri
                    SoftwareRequests = allRequests.Count(r => r.Category == ServiceCategory.SoftwareIssue),
                    HardwareRequests = allRequests.Count(r => r.Category == ServiceCategory.HardwareIssue),
                    NetworkRequests = allRequests.Count(r => r.Category == ServiceCategory.NetworkIssue),
                    SecurityRequests = allRequests.Count(r => r.Category == ServiceCategory.SecurityIssue),
                    MaintenanceRequests = allRequests.Count(r => r.Category == ServiceCategory.Maintenance)
                };

                return View(dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Yönetici dashboard yüklenirken hata");
                TempData["Error"] = "Dashboard yüklenirken bir hata oluştu.";
                return View(new ManagerDashboardDto());
            }
        }

        /// <summary>
        /// Detaylı teknisyen iş yükü bilgilerini JSON olarak döndürür
        /// </summary>
        [HttpGet("GetDetailedTechnicianWorkload")]
        public async Task<IActionResult> GetDetailedTechnicianWorkload()
        {
            try
            {
                var workloadData = await _workAssignmentService.GetDetailedTechnicianWorkloadAsync();
                return Json(workloadData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Detaylı teknisyen iş yükü bilgileri alınırken hata");
                return Json(new { error = "İş yükü bilgileri alınamadı" });
            }
        }



        // GET: Manager/AllRequests
        [HttpGet("AllRequests")]
        public async Task<IActionResult> AllRequests()
        {
            try
            {
                var allRequests = await _serviceRequestService.GetAllAsync();
                var responseDto = _mapper.Map<List<ServiceRequestResponseDto>>(allRequests);
                
                return View(responseDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tüm talepler yüklenirken hata");
                TempData["Error"] = "Talepler yüklenirken bir hata oluştu.";
                return View(new List<ServiceRequestResponseDto>());
            }
        }

        // GET: Manager/TechnicianWorkload
        [HttpGet("TechnicianWorkload")]
        public async Task<IActionResult> TechnicianWorkload()
        {
            try
            {
                var workloadData = await _workAssignmentService.GetTechnicianWorkloadAsync();
                return View(workloadData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Teknisyen iş yükü yüklenirken hata");
                TempData["Error"] = "Teknisyen iş yükü yüklenirken bir hata oluştu.";
                return View(new Dictionary<int, TechnicianWorkload>());
            }
        }



        // GET: Manager/GetTechnicians
        [HttpGet("GetTechnicians")]
        public async Task<IActionResult> GetTechnicians(int? serviceRequestId = null, int? excludeTechnicianId = null)
        {
            try
            {
                // Sadece aktif teknisyenleri al
                var technicians = await _userRepository.GetAvailableTechniciansAsync();
                
                // Eğer excludeTechnicianId varsa, o teknisyeni listeden çıkar
                if (excludeTechnicianId.HasValue)
                {
                    technicians = technicians.Where(t => t.Id != excludeTechnicianId.Value).ToList();
                }

                ServiceCategory? requestCategory = null;
                
                // Eğer serviceRequestId varsa, servis talebinin kategorisini al
                if (serviceRequestId.HasValue)
                {
                    var serviceRequest = await _serviceRequestService.GetByIdAsync(serviceRequestId.Value);
                    if (serviceRequest != null)
                    {
                        requestCategory = serviceRequest.Category;
                    }
                }

                var technicianList = technicians.Select(t => new
                {
                    id = t.Id,
                    name = $"{t.FirstName} {t.LastName}",
                    isAvailable = true, // Tüm teknisyenler müsait kabul edilir
                    specializations = t.Specializations ?? "Genel",
                    isRecommended = IsRecommendedForCategory(t.Specializations, requestCategory),
                    matchScore = CalculateMatchScore(t.Specializations, requestCategory)
                })
                .OrderByDescending(t => t.isRecommended)
                .ThenByDescending(t => t.matchScore)
                .ThenBy(t => t.name)
                .ToList();

                return Json(technicianList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Teknisyen listesi alınırken hata");
                return Json(new { success = false, message = "Teknisyen listesi alınamadı." });
            }
        }

        private bool IsRecommendedForCategory(string? specializations, ServiceCategory? category)
        {
            if (string.IsNullOrEmpty(specializations) || !category.HasValue)
                return false;

            var specs = specializations.ToLower();
            
            return category.Value switch
            {
                ServiceCategory.SoftwareIssue => specs.Contains("yazılım") || specs.Contains("software") || specs.Contains("uygulama") || specs.Contains("program"),
                ServiceCategory.HardwareIssue => specs.Contains("donanım") || specs.Contains("hardware") || specs.Contains("bilgisayar") || specs.Contains("laptop"),
                ServiceCategory.NetworkIssue => specs.Contains("ağ") || specs.Contains("network") || specs.Contains("internet") || specs.Contains("wifi"),
                ServiceCategory.SecurityIssue => specs.Contains("güvenlik") || specs.Contains("security") || specs.Contains("virüs") || specs.Contains("firewall"),
                ServiceCategory.Maintenance => specs.Contains("bakım") || specs.Contains("maintenance") || specs.Contains("temizlik") || specs.Contains("genel"),
                _ => false
            };
        }

        private int CalculateMatchScore(string? specializations, ServiceCategory? category)
        {
            if (string.IsNullOrEmpty(specializations) || !category.HasValue)
                return 0;

            var specs = specializations.ToLower();
            int score = 0;

            switch (category.Value)
            {
                case ServiceCategory.SoftwareIssue:
                    if (specs.Contains("yazılım")) score += 10;
                    if (specs.Contains("software")) score += 10;
                    if (specs.Contains("uygulama")) score += 8;
                    if (specs.Contains("program")) score += 8;
                    break;
                case ServiceCategory.HardwareIssue:
                    if (specs.Contains("donanım")) score += 10;
                    if (specs.Contains("hardware")) score += 10;
                    if (specs.Contains("bilgisayar")) score += 8;
                    if (specs.Contains("laptop")) score += 8;
                    break;
                case ServiceCategory.NetworkIssue:
                    if (specs.Contains("ağ")) score += 10;
                    if (specs.Contains("network")) score += 10;
                    if (specs.Contains("internet")) score += 8;
                    if (specs.Contains("wifi")) score += 6;
                    break;
                case ServiceCategory.SecurityIssue:
                    if (specs.Contains("güvenlik")) score += 10;
                    if (specs.Contains("security")) score += 10;
                    if (specs.Contains("virüs")) score += 8;
                    if (specs.Contains("firewall")) score += 8;
                    break;
                case ServiceCategory.Maintenance:
                    if (specs.Contains("bakım")) score += 10;
                    if (specs.Contains("maintenance")) score += 10;
                    if (specs.Contains("genel")) score += 8;
                    break;
            }

            return score;
        }

        // POST: Manager/AssignTechnician
        [HttpPost("AssignTechnician")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignTechnician(int serviceRequestId, int technicianId, string? notes)
        {
            try
            {
                _logger.LogInformation("Teknisyen atama işlemi başlatılıyor: ServiceRequestId {ServiceRequestId}, TechnicianId {TechnicianId}", 
                    serviceRequestId, technicianId);

                await _workAssignmentService.ManualAssignAsync(serviceRequestId, technicianId, null, notes);

                _logger.LogInformation("Teknisyen başarıyla atandı: ServiceRequestId {ServiceRequestId}, TechnicianId {TechnicianId}", 
                    serviceRequestId, technicianId);

                return Json(new { success = true, message = "Teknisyen başarıyla atandı." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Teknisyen atanırken hata: ServiceRequestId {ServiceRequestId}, TechnicianId {TechnicianId}", 
                    serviceRequestId, technicianId);
                return Json(new { success = false, message = $"Teknisyen atanırken bir hata oluştu: {ex.Message}" });
            }
        }

        // POST: Manager/ReassignTechnician
        [HttpPost("ReassignTechnician")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReassignTechnician(int assignmentId, int newTechnicianId, string reason)
        {
            try
            {
                _logger.LogInformation("Yeniden atama işlemi başlatılıyor: AssignmentId {AssignmentId}, NewTechnicianId {NewTechnicianId}", 
                    assignmentId, newTechnicianId);

                await _workAssignmentService.ReassignAsync(assignmentId, newTechnicianId, reason);

                return Json(new { success = true, message = "Atama başarıyla değiştirildi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Atama değiştirilirken hata: AssignmentId {AssignmentId}, NewTechnicianId {NewTechnicianId}", 
                    assignmentId, newTechnicianId);
                return Json(new { success = false, message = $"Atama değiştirilirken bir hata oluştu: {ex.Message}" });
            }
        }

        // POST: Manager/CancelAssignment
        [HttpPost("CancelAssignment")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAssignment(int assignmentId, string reason)
        {
            try
            {
                _logger.LogInformation("Atama iptal işlemi başlatılıyor: AssignmentId {AssignmentId}", assignmentId);

                await _workAssignmentService.CancelAssignmentAsync(assignmentId, reason);

                return Json(new { success = true, message = "Atama başarıyla iptal edildi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Atama iptal edilirken hata: AssignmentId {AssignmentId}", assignmentId);
                return Json(new { success = false, message = $"Atama iptal edilirken bir hata oluştu: {ex.Message}" });
        }
    }

    // POST: Manager/DeleteRequest/5
    [HttpPost("DeleteRequest/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRequest(int id)
    {
        try
        {
            var result = await _serviceRequestService.DeleteAsync(id);
            if (result)
            {
                TempData["Success"] = "Servis talebi başarıyla silindi.";
            }
            else
            {
                TempData["Error"] = "Servis talebi silinirken bir hata oluştu.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manager servis talebi silinirken hata: {Id}", id);
            TempData["Error"] = "Servis talebi silinirken bir hata oluştu.";
        }

        return RedirectToAction("AllRequests");
    }

        // GET: Manager/GetServiceRequestDetails
        [HttpGet("GetServiceRequestDetails")]
        public async Task<IActionResult> GetServiceRequestDetails(int id)
        {
            try
            {
                var serviceRequest = await _serviceRequestService.GetByIdAsync(id);
                if (serviceRequest == null)
                {
                    return Json(new { success = false, message = "Servis talebi bulunamadı." });
                }

                return Json(new { 
                    success = true, 
                    assignedTechnicianId = serviceRequest.AssignedTechnicianId,
                    category = serviceRequest.Category.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Servis talebi detayları alınırken hata: Id {Id}", id);
                return Json(new { success = false, message = "Servis talebi detayları alınamadı." });
            }
        }

        // GET: Manager/GetActiveAssignmentId
        [HttpGet("GetActiveAssignmentId")]
        public async Task<IActionResult> GetActiveAssignmentId(int serviceRequestId)
        {
            try
            {
                var assignments = await _workAssignmentService.GetByServiceRequestAsync(serviceRequestId);
                var activeAssignment = assignments.FirstOrDefault(a => 
                    a.Status == WorkAssignmentStatus.Assigned || 
                    a.Status == WorkAssignmentStatus.InProgress);

                if (activeAssignment != null)
                {
                    return Json(new { success = true, assignmentId = activeAssignment.Id });
                }

                return Json(new { success = false, message = "Aktif atama bulunamadı." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Aktif atama ID'si alınırken hata: ServiceRequestId {ServiceRequestId}", serviceRequestId);
                return Json(new { success = false, message = "Aktif atama bilgisi alınamadı." });
            }
        }

        // GET: Manager/GetTechnicianDetails
        [HttpGet("GetTechnicianDetails")]
        public async Task<IActionResult> GetTechnicianDetails(int technicianId)
        {
            try
            {
                var assignments = await _workAssignmentService.GetByTechnicianAsync(technicianId);
                
                var assignedCount = assignments.Count(a => a.Status == WorkAssignmentStatus.Assigned);
                var inProgressCount = assignments.Count(a => a.Status == WorkAssignmentStatus.InProgress);
                var completedCount = assignments.Count(a => a.Status == WorkAssignmentStatus.Completed);
                var cancelledCount = assignments.Count(a => a.Status == WorkAssignmentStatus.Cancelled);

                var recentAssignments = assignments
                    .Where(a => a.Status == WorkAssignmentStatus.Assigned || a.Status == WorkAssignmentStatus.InProgress)
                    .OrderByDescending(a => a.AssignedDate)
                    .Take(10)
                    .ToList();

                var html = $@"
                    <div class='row mb-3'>
                        <div class='col-md-3'>
                            <div class='card bg-warning text-white'>
                                <div class='card-body text-center'>
                                    <h4>{assignedCount}</h4>
                                    <small>Bekleyen</small>
                                </div>
                            </div>
                        </div>
                        <div class='col-md-3'>
                            <div class='card bg-primary text-white'>
                                <div class='card-body text-center'>
                                    <h4>{inProgressCount}</h4>
                                    <small>İşlemde</small>
                                </div>
                            </div>
                        </div>
                        <div class='col-md-3'>
                            <div class='card bg-success text-white'>
                                <div class='card-body text-center'>
                                    <h4>{completedCount}</h4>
                                    <small>Tamamlandı</small>
                                </div>
                            </div>
                        </div>
                        <div class='col-md-3'>
                            <div class='card bg-danger text-white'>
                                <div class='card-body text-center'>
                                    <h4>{cancelledCount}</h4>
                                    <small>İptal</small>
                                </div>
                            </div>
                        </div>
                    </div>";

                if (recentAssignments.Any())
                {
                    html += @"
                        <h6>Son Görevler</h6>
                        <div class='table-responsive'>
                            <table class='table table-sm'>
                                <thead>
                                    <tr>
                                        <th>Talep No</th>
                                        <th>Başlık</th>
                                        <th>Durum</th>
                                        <th>Atanma Tarihi</th>
                                    </tr>
                                </thead>
                                <tbody>";

                    foreach (var assignment in recentAssignments)
                    {
                        var statusBadge = assignment.Status switch
                        {
                            WorkAssignmentStatus.Assigned => "<span class='badge bg-warning'>Bekleyen</span>",
                            WorkAssignmentStatus.InProgress => "<span class='badge bg-primary'>İşlemde</span>",
                            WorkAssignmentStatus.Completed => "<span class='badge bg-success'>Tamamlandı</span>",
                            WorkAssignmentStatus.Cancelled => "<span class='badge bg-danger'>İptal</span>",
                            _ => "<span class='badge bg-secondary'>Bilinmiyor</span>"
                        };

                        html += $@"
                            <tr>
                                <td>#{assignment.ServiceRequestId}</td>
                                <td>{assignment.ServiceRequestTitle}</td>
                                <td>{statusBadge}</td>
                                <td>{assignment.AssignedDate:dd.MM.yyyy}</td>
                            </tr>";
                    }

                    html += @"
                                </tbody>
                            </table>
                        </div>";
                }
                else
                {
                    html += "<p class='text-muted'>Aktif görev bulunmuyor.</p>";
                }

                return Content(html, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Teknisyen detayları alınırken hata: TechnicianId {TechnicianId}", technicianId);
                return Content("<div class='alert alert-danger'>Teknisyen detayları yüklenirken hata oluştu.</div>", "text/html");
            }
        }

        private async Task<int?> GetCurrentUserIdAsync()
        {
            try
            {
                var identityUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Identity User ID: {IdentityUserId}", identityUserId);
                
                if (string.IsNullOrEmpty(identityUserId))
                {
                    _logger.LogWarning("Identity User ID is null or empty");
                    return null;
                }

                var user = await _userRepository.GetByIdentityUserIdAsync(identityUserId);
                _logger.LogInformation("Found user by IdentityUserId: {UserId}, Email: {Email}", user?.Id, user?.Email);
                
                if (user != null)
                {
                    return user.Id;
                }

                // Fallback: Email ile arama yap
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name;
                if (!string.IsNullOrEmpty(userEmail))
                {
                    _logger.LogInformation("Trying to find user by email: {Email}", userEmail);
                    var userByEmail = await _userRepository.GetByEmailAsync(userEmail);
                    if (userByEmail != null)
                    {
                        _logger.LogInformation("Found user by email: {UserId}, updating IdentityUserId", userByEmail.Id);
                        // IdentityUserId'yi güncelle
                        userByEmail.IdentityUserId = identityUserId;
                        await _userRepository.UpdateAsync(userByEmail);
                        return userByEmail.Id;
                    }
                }
                
                _logger.LogWarning("User not found with IdentityUserId: {IdentityUserId} or Email: {Email}", identityUserId, userEmail);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user ID");
                return null;
            }
        }

        private int? GetCurrentUserId()
        {
            // Synchronous wrapper for compatibility
            return GetCurrentUserIdAsync().Result;
        }
    }
}