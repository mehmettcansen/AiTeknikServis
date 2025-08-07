using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AutoMapper;
using AiTeknikServis.Services.Contracts;
using AiTeknikServis.Entities.Dtos.ServiceRequest;
using AiTeknikServis.Entities.Dtos.Dashboard;
using AiTeknikServis.Entities.Dtos.Notification;
using AiTeknikServis.Entities.Models;
using AiTeknikServis.Repositories.Contracts;
using System.Security.Claims;

namespace AiTeknikServis.Controllers
{
    [Authorize(Roles = "Customer")]
    [Route("[controller]")]
    public class CustomerController : Controller
    {
        private readonly IServiceRequestService _serviceRequestService;
        private readonly INotificationService _notificationService;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<CustomerController> _logger;
        private readonly IFileService _fileService;

        public CustomerController(
            IServiceRequestService serviceRequestService,
            INotificationService notificationService,
            IUserRepository userRepository,
            IMapper mapper,
            ILogger<CustomerController> logger,
            IFileService fileService)
        {
            _serviceRequestService = serviceRequestService;
            _notificationService = notificationService;
            _userRepository = userRepository;
            _mapper = mapper;
            _logger = logger;
            _fileService = fileService;
        }

        // GET: Customer/Dashboard
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

                var myRequests = await _serviceRequestService.GetByCustomerAsync(userId.Value);
                var notifications = await _notificationService.GetByUserAsync(userId.Value);

                var dashboardData = new CustomerDashboardDto
                {
                    MyRequests = _mapper.Map<List<ServiceRequestResponseDto>>(myRequests),
                    RecentNotifications = _mapper.Map<List<NotificationResponseDto>>(notifications.Take(5).ToList()),
                    TotalRequests = myRequests.Count,
                    PendingRequests = myRequests.Count(r => r.Status == ServiceStatus.Pending),
                    ActiveRequests = myRequests.Count(r => r.Status != ServiceStatus.Completed && r.Status != ServiceStatus.Cancelled),
                    CompletedRequests = myRequests.Count(r => r.Status == ServiceStatus.Completed)
                };

                return View(dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Müşteri dashboard yüklenirken hata");
                TempData["Error"] = "Dashboard yüklenirken bir hata oluştu.";
                return View();
            }
        }

        // GET: Customer/MyRequests
        [HttpGet("MyRequests")]
        public async Task<IActionResult> MyRequests()
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (userId == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var myRequests = await _serviceRequestService.GetByCustomerAsync(userId.Value);
                var responseDto = _mapper.Map<List<ServiceRequestResponseDto>>(myRequests);
                
                return View(responseDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Müşteri talepleri yüklenirken hata");
                TempData["Error"] = "Talepleriniz yüklenirken bir hata oluştu.";
                return View(new List<ServiceRequestResponseDto>());
            }
        }

        // GET: Customer/CreateRequest
        [HttpGet("CreateRequest")]
        public IActionResult CreateRequest()
        {
            return View(new ServiceRequestCreateDto());
        }

        // POST: Customer/CreateRequest
        [HttpPost("CreateRequest")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRequest(ServiceRequestCreateDto createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(createDto);
                }

                var userId = await GetCurrentUserIdAsync();
                if (userId == null)
                {
                    _logger.LogError("Customer ID could not be retrieved for user: {UserName}", User.Identity?.Name);
                    TempData["Error"] = "Kullanıcı bilgisi alınamadı. Lütfen çıkış yapıp tekrar giriş yapmayı deneyin.";
                    return View(createDto);
                }

                _logger.LogInformation("Creating service request for customer ID: {CustomerId}", userId.Value);
                createDto.CustomerId = userId.Value;
                var serviceRequest = await _serviceRequestService.CreateAsync(createDto);

                // Dosya yükleme işlemi
                if (createDto.Files != null && createDto.Files.Any())
                {
                    try
                    {
                        // List<IFormFile> to IFormFileCollection conversion
                        var fileCollection = new FormFileCollection();
                        foreach (var file in createDto.Files)
                        {
                            fileCollection.Add(file);
                        }
                        
                        await _fileService.UploadFilesAsync(serviceRequest.Id, fileCollection, userId.Value);
                        _logger.LogInformation("Files uploaded successfully for service request: {ServiceRequestId}", serviceRequest.Id);
                    }
                    catch (Exception fileEx)
                    {
                        _logger.LogError(fileEx, "Error uploading files for service request: {ServiceRequestId}", serviceRequest.Id);
                        TempData["Warning"] = "Servis talebi oluşturuldu ancak dosya yükleme sırasında bir hata oluştu.";
                    }
                }

                TempData["Success"] = "Servis talebiniz başarıyla oluşturuldu.";
                return RedirectToAction(nameof(MyRequests));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Müşteri talep oluştururken hata");
                ModelState.AddModelError("", "Talep oluşturulurken bir hata oluştu: " + ex.Message);
                return View(createDto);
            }
        }

        // POST: Customer/DeleteRequest/5
        [HttpPost("DeleteRequest/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRequest(int id)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (userId == null)
                {
                    TempData["Error"] = "Kullanıcı bilgisi alınamadı.";
                    return RedirectToAction("MyRequests");
                }

                // Talebin müşteriye ait olup olmadığını kontrol et
                var serviceRequest = await _serviceRequestService.GetByIdAsync(id);
                if (serviceRequest == null)
                {
                    TempData["Error"] = "Servis talebi bulunamadı.";
                    return RedirectToAction("MyRequests");
                }

                if (serviceRequest.CustomerId != userId.Value)
                {
                    TempData["Error"] = "Bu talebi silme yetkiniz yok.";
                    return RedirectToAction("MyRequests");
                }

                var result = await _serviceRequestService.DeleteAsync(id);
                if (result)
                {
                    TempData["Success"] = "Servis talebiniz başarıyla silindi.";
                }
                else
                {
                    TempData["Error"] = "Servis talebi silinirken bir hata oluştu.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Müşteri servis talebi silinirken hata: {Id}", id);
                TempData["Error"] = "Servis talebi silinirken bir hata oluştu.";
            }

            return RedirectToAction("MyRequests");
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
            var identityUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(identityUserId))
            {
                return null;
            }

            // Synchronous version - not recommended but needed for compatibility
            var user = _userRepository.GetByIdentityUserIdAsync(identityUserId).Result;
            return user?.Id;
        }
    }
}