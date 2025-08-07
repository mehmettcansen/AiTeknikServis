using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AutoMapper;
using AiTeknikServis.Services.Contracts;
using AiTeknikServis.Repositories.Contracts;
using AiTeknikServis.Entities.Dtos.ServiceRequest;
using AiTeknikServis.Entities.Models;
using System.Security.Claims;

namespace AiTeknikServis.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class ServiceRequestController : Controller
    {
        private readonly IServiceRequestService _serviceRequestService;
        private readonly IServiceRequestRepository _serviceRequestRepository;
        private readonly IFileService _fileService;
        private readonly IUserRepository _userRepository;
        private readonly IReportService _reportService;
        private readonly IMapper _mapper;
        private readonly ILogger<ServiceRequestController> _logger;

        public ServiceRequestController(
            IServiceRequestService serviceRequestService,
            IServiceRequestRepository serviceRequestRepository,
            IFileService fileService,
            IUserRepository userRepository,
            IReportService reportService,
            IMapper mapper,
            ILogger<ServiceRequestController> logger)
        {
            _serviceRequestService = serviceRequestService;
            _serviceRequestRepository = serviceRequestRepository;
            _fileService = fileService;
            _userRepository = userRepository;
            _reportService = reportService;
            _mapper = mapper;
            _logger = logger;
        }

        // GET: ServiceRequest
        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var serviceRequests = await _serviceRequestService.GetAllAsync();
                var responseDto = _mapper.Map<List<ServiceRequestResponseDto>>(serviceRequests);
                return View(responseDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Servis talepleri listelenirken hata oluştu");
                TempData["Error"] = "Servis talepleri yüklenirken bir hata oluştu.";
                return View(new List<ServiceRequestResponseDto>());
            }
        }

        // GET: ServiceRequest/Details/5
        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var serviceRequest = await _serviceRequestService.GetByIdAsync(id);
                if (serviceRequest == null)
                {
                    return NotFound("Servis talebi bulunamadı.");
                }

                // Yetkilendirme kontrolü
                if (User.IsInRole("Customer"))
                {
                    var currentUserId = GetCurrentUserId();
                    if (currentUserId == null || serviceRequest.CustomerId != currentUserId.Value)
                    {
                        TempData["Error"] = "Bu talebi görüntüleme yetkiniz yok.";
                        return RedirectToAction("MyRequests", "Customer");
                    }
                }
                else if (User.IsInRole("Technician"))
                {
                    var currentUserId = GetCurrentUserId();
                    if (currentUserId == null || serviceRequest.AssignedTechnicianId != currentUserId.Value)
                    {
                        TempData["Error"] = "Bu talebi görüntüleme yetkiniz yok.";
                        return RedirectToAction("MyAssignments", "Technician");
                    }
                }

                // Servis talebine ait dosyaları getir
                var files = await _fileService.GetFilesByServiceRequestIdAsync(id);
                
                // ServiceRequestResponseDto'ya map et
                var responseDto = _mapper.Map<ServiceRequestResponseDto>(serviceRequest);
                responseDto.Files = _mapper.Map<List<ServiceRequestFileDto>>(files);

                return View(responseDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Servis talebi detayı yüklenirken hata: {Id}", id);
                TempData["Error"] = "Servis talebi detayları yüklenirken bir hata oluştu.";
                return RedirectToAction(nameof(Index));
            }
        }

        
        

        // POST: ServiceRequest/Delete/5
        [HttpPost("Delete/{id}")]
        [Authorize(Roles = "Customer,Manager,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                // Yetkilendirme kontrolü
                if (User.IsInRole("Customer"))
                {
                    var currentUserId = GetCurrentUserId();
                    if (currentUserId == null)
                    {
                        TempData["Error"] = "Kullanıcı bilgisi alınamadı.";
                        return RedirectToAction("MyRequests", "Customer");
                    }

                    var serviceRequest = await _serviceRequestService.GetByIdAsync(id);
                    if (serviceRequest == null)
                    {
                        TempData["Error"] = "Servis talebi bulunamadı.";
                        return RedirectToAction("MyRequests", "Customer");
                    }

                    if (serviceRequest.CustomerId != currentUserId.Value)
                    {
                        TempData["Error"] = "Bu talebi silme yetkiniz yok.";
                        return RedirectToAction("MyRequests", "Customer");
                    }
                }

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
                _logger.LogError(ex, "Servis talebi silinirken hata: {Id}", id);
                TempData["Error"] = "Servis talebi silinirken bir hata oluştu.";
            }

            // Kullanıcı rolüne göre yönlendirme
            if (User.IsInRole("Customer"))
            {
                return RedirectToAction("MyRequests", "Customer");
            }
            else if (User.IsInRole("Manager"))
            {
                return RedirectToAction("AllRequests", "Manager");
            }
            else if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "ServiceRequest", new { area = "Admin" });
            }
            else
            {
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: ServiceRequest/UploadFiles/5
        [HttpPost("UploadFiles/{id}")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadFiles(int id, IFormFileCollection files)
        {
            try
            {
                if (files == null || !files.Any())
                {
                    return Json(new { success = false, message = "Dosya seçilmedi." });
                }

                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Json(new { success = false, message = "Kullanıcı bilgisi alınamadı." });
                }

                var uploadedFiles = await _fileService.UploadFilesAsync(id, files, userId.Value);

                return Json(new { 
                    success = true, 
                    message = $"{uploadedFiles.Count} dosya başarıyla yüklendi.",
                    files = uploadedFiles.Select(f => new { 
                        id = f.Id, 
                        name = f.FileName, // OriginalFileName yerine FileName kullan
                        size = f.FileSize 
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dosya yüklenirken hata: ServiceRequestId {Id}", id);
                return Json(new { success = false, message = "Dosya yüklenirken bir hata oluştu." });
            }
        }

        // GET: ServiceRequest/DownloadFile/{fileId}
        [HttpGet("DownloadFile/{fileId}")]
        [Authorize]
        public async Task<IActionResult> DownloadFile(int fileId)
        {
            try
            {
                var file = await _fileService.GetFileByIdAsync(fileId);
                if (file == null)
                {
                    return NotFound("Dosya bulunamadı.");
                }

                // Yetkilendirme kontrolü - kullanıcı bu dosyaya erişebilir mi?
                var serviceRequest = await _serviceRequestRepository.GetByIdAsync(file.ServiceRequestId);
                if (serviceRequest == null)
                {
                    return NotFound("Servis talebi bulunamadı.");
                }

                // Yetki kontrolü
                if (!await HasAccessToServiceRequest(_mapper.Map<ServiceRequestResponseDto>(serviceRequest)))
                {
                    TempData["Error"] = "Bu dosyaya erişim yetkiniz yok.";
                    return RedirectToAction("Index", "Home");
                }

                var (stream, fileName, contentType) = await _fileService.DownloadFileAsync(fileId);
                
                return File(stream, contentType ?? "application/octet-stream", fileName ?? file.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dosya indirme hatası: FileID {FileId}", fileId);
                TempData["Error"] = "Dosya indirilemedi.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: ServiceRequest/GetFile/{fileId}
        [HttpGet("GetFile/{fileId}")]
        [Authorize]
        public async Task<IActionResult> GetFile(int fileId)
        {
            try
            {
                var file = await _fileService.GetFileByIdAsync(fileId);
                if (file == null)
                {
                    return NotFound("Dosya bulunamadı.");
                }

                // Yetkilendirme kontrolü
                var serviceRequest = await _serviceRequestRepository.GetByIdAsync(file.ServiceRequestId);
                if (serviceRequest == null)
                {
                    return NotFound("Servis talebi bulunamadı.");
                }

                if (!await HasAccessToServiceRequest(_mapper.Map<ServiceRequestResponseDto>(serviceRequest)))
                {
                    return Forbid();
                }

                var (stream, fileName, contentType) = await _fileService.DownloadFileAsync(fileId);
                
                // Resim dosyaları için inline görüntüleme
                if (!string.IsNullOrEmpty(contentType) && contentType.StartsWith("image/"))
                {
                    return File(stream, contentType);
                }
                
                return File(stream, contentType ?? "application/octet-stream", fileName ?? file.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dosya görüntüleme hatası: FileID {FileId}", fileId);
                return NotFound();
            }
        }

        // Helper method - Kullanıcının servis talebine erişim yetkisi var mı?
        private async Task<bool> HasAccessToServiceRequest(ServiceRequestResponseDto serviceRequest)
        {
            if (User.IsInRole("Admin") || User.IsInRole("Manager"))
            {
                return true;
            }

            if (User.IsInRole("Customer"))
            {
                var currentUserId = GetCurrentUserId();
                return currentUserId.HasValue && serviceRequest.CustomerId == currentUserId.Value;
            }

            if (User.IsInRole("Technician"))
            {
                var currentUserId = GetCurrentUserId();
                return currentUserId.HasValue && serviceRequest.AssignedTechnicianId == currentUserId.Value;
            }

            return false;
        }

        // POST: ServiceRequest/UpdateStatus
        [HttpPost("UpdateStatus")]
        [Authorize(Roles = "Technician,Manager,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, ServiceStatus status)
        {
            try
            {
                await _serviceRequestService.ChangeStatusAsync(id, status);
                TempData["Success"] = "Talep durumu başarıyla güncellendi.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Talep durumu güncellenirken hata: {Id}", id);
                TempData["Error"] = "Talep durumu güncellenirken bir hata oluştu.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: ServiceRequest/CompleteRequest
        [HttpPost("CompleteRequest")]
        [Authorize(Roles = "Technician,Manager,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteRequest(int id, string resolution, int? actualHours = null, decimal? actualCost = null)
        {
            try
            {
                await _serviceRequestService.CompleteServiceRequestAsync(id, resolution, actualCost, actualHours);
                TempData["Success"] = "Talep başarıyla tamamlandı.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Talep tamamlanırken hata: {Id}", id);
                TempData["Error"] = "Talep tamamlanırken bir hata oluştu.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // Helper method to get current user ID
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

        // GET: ServiceRequest/ViewReport/5
        [HttpGet("ViewReport/{id}")]
        [Authorize]
        public async Task<IActionResult> ViewReport(int id)
        {
            try
            {
                var serviceRequest = await _serviceRequestService.GetByIdAsync(id);
                if (serviceRequest == null)
                {
                    return NotFound("Servis talebi bulunamadı.");
                }

                // Sadece tamamlanan talepler için rapor göster
                if (serviceRequest.Status != ServiceStatus.Completed)
                {
                    TempData["Error"] = "Sadece tamamlanan talepler için rapor görüntülenebilir.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Yetkilendirme kontrolü
                var serviceRequestDto = _mapper.Map<ServiceRequestResponseDto>(serviceRequest);
                if (!await HasAccessToServiceRequest(serviceRequestDto))
                {
                    TempData["Error"] = "Bu raporu görüntüleme yetkiniz yok.";
                    return RedirectToAction("Index", "Home");
                }

                var reportData = await _reportService.GenerateServiceRequestReportAsync(id);
                return View(reportData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rapor görüntülenirken hata: {Id}", id);
                TempData["Error"] = "Rapor görüntülenirken bir hata oluştu.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // GET: ServiceRequest/DownloadReport/5
        [HttpGet("DownloadReport/{id}")]
        [Authorize]
        public async Task<IActionResult> DownloadReport(int id)
        {
            try
            {
                var serviceRequest = await _serviceRequestService.GetByIdAsync(id);
                if (serviceRequest == null)
                {
                    return NotFound("Servis talebi bulunamadı.");
                }

                // Sadece tamamlanan talepler için rapor indir
                if (serviceRequest.Status != ServiceStatus.Completed)
                {
                    TempData["Error"] = "Sadece tamamlanan talepler için rapor indirilebilir.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Yetkilendirme kontrolü
                var serviceRequestDto = _mapper.Map<ServiceRequestResponseDto>(serviceRequest);
                if (!await HasAccessToServiceRequest(serviceRequestDto))
                {
                    TempData["Error"] = "Bu raporu indirme yetkiniz yok.";
                    return RedirectToAction("Index", "Home");
                }

                var pdfBytes = await _reportService.GenerateServiceRequestPdfAsync(id);
                var fileName = $"Servis_Raporu_{id}_{DateTime.Now:yyyyMMdd}.pdf";

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PDF raporu indirirken hata: {Id}", id);
                TempData["Error"] = "PDF raporu indirirken bir hata oluştu.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }
    }
}