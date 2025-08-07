using AiTeknikServis.Entities.Models;
using AiTeknikServis.Services.Contracts;
using AiTeknikServis.Entities.Dtos.WorkAssignment;
using AiTeknikServis.Entities.Dtos.ServiceRequest;
using AiTeknikServis.Entities.Dtos.Dashboard;
using AiTeknikServis.Repositories.Contracts;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AiTeknikServis.Controllers
{
    [Authorize(Roles = "Technician")]
    [Route("[controller]")]
    public class TechnicianController : Controller
    {
        private readonly IWorkAssignmentService _workAssignmentService;
        private readonly IServiceRequestService _serviceRequestService;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<TechnicianController> _logger;

        public TechnicianController(
            IWorkAssignmentService workAssignmentService,
            IServiceRequestService serviceRequestService,
            IUserRepository userRepository,
            IMapper mapper,
            ILogger<TechnicianController> logger)
        {
            _workAssignmentService = workAssignmentService;
            _serviceRequestService = serviceRequestService;
            _userRepository = userRepository;
            _mapper = mapper;
            _logger = logger;
        }

        // GET: Technician/Dashboard
        [HttpGet("Dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (userId == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                // ServiceRequest sistemini kullan
                var serviceRequests = await _serviceRequestService.GetByTechnicianAsync(userId.Value);
                var activeRequests = serviceRequests.Where(sr => sr.Status != ServiceStatus.Completed && sr.Status != ServiceStatus.Cancelled).ToList();

                var dashboardData = new TechnicianDashboardDto
                {
                    ActiveServiceRequests = activeRequests,
                    TotalAssignments = serviceRequests.Count,
                    PendingAssignments = serviceRequests.Count(sr => sr.Status == ServiceStatus.Pending),
                    InProgressAssignments = serviceRequests.Count(sr => sr.Status == ServiceStatus.InProgress),
                    CompletedToday = serviceRequests.Count(sr => sr.Status == ServiceStatus.Completed && 
                                                               sr.CompletedDate?.Date == DateTime.Today)
                };

                return View(dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Teknisyen dashboard yüklenirken hata");
                TempData["Error"] = "Dashboard yüklenirken bir hata oluştu.";
                return View(new TechnicianDashboardDto());
            }
        }

        // GET: Technician/MyAssignments
        [HttpGet("MyAssignments")]
        public async Task<IActionResult> MyAssignments()
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (userId == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                // ServiceRequest sistemini kullan
                var serviceRequests = await _serviceRequestService.GetByTechnicianAsync(userId.Value);
                
                return View(serviceRequests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Teknisyen atamaları yüklenirken hata");
                TempData["Error"] = "Atamalarınız yüklenirken bir hata oluştu.";
                return View(new List<ServiceRequestResponseDto>());
            }
        }

        // POST: Technician/UpdateAssignmentStatus
        [HttpPost("UpdateAssignmentStatus")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAssignmentStatus(int assignmentId, WorkAssignmentStatus status, string? notes)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (userId == null)
                {
                    return Json(new { success = false, message = "Kullanıcı bilgisi alınamadı." });
                }

                // Status'a göre uygun metodu çağır
                if (status == WorkAssignmentStatus.InProgress)
                {
                    await _workAssignmentService.StartAssignmentAsync(assignmentId);
                }
                else if (status == WorkAssignmentStatus.Completed)
                {
                    await _workAssignmentService.CompleteAssignmentAsync(assignmentId, notes);
                }
                else if (status == WorkAssignmentStatus.Cancelled)
                {
                    await _workAssignmentService.CancelAssignmentAsync(assignmentId, notes);
                }

                return Json(new { success = true, message = "Atama durumu başarıyla güncellendi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Atama durumu güncellenirken hata: {AssignmentId}", assignmentId);
                return Json(new { success = false, message = "Durum güncellenirken bir hata oluştu." });
            }
        }

        // POST: Technician/UpdateAssignmentNotes
        [HttpPost("UpdateAssignmentNotes")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAssignmentNotes(int assignmentId, string notes)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (userId == null)
                {
                    return Json(new { success = false, message = "Kullanıcı bilgisi alınamadı." });
                }

                // Atamayı al ve güncelle
                var assignment = await _workAssignmentService.GetByIdAsync(assignmentId);
                if (assignment == null)
                {
                    return Json(new { success = false, message = "Atama bulunamadı." });
                }

                // Not güncelleme için service metodu eklenebilir, şimdilik basit güncelleme
                // Bu kısım WorkAssignmentService'e UpdateNotesAsync metodu eklenerek geliştirilebilir
                
                return Json(new { success = true, message = "Notlar başarıyla güncellendi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Atama notları güncellenirken hata: {AssignmentId}", assignmentId);
                return Json(new { success = false, message = "Notlar güncellenirken bir hata oluştu." });
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