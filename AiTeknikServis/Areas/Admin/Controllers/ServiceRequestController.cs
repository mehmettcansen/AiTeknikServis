using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AutoMapper;
using AiTeknikServis.Services.Contracts;
using AiTeknikServis.Entities.Dtos.ServiceRequest;
using AiTeknikServis.Entities.Models;

namespace AiTeknikServis.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ServiceRequestController : Controller
    {
        private readonly IServiceRequestService _serviceRequestService;
        private readonly IFileService _fileService;
        private readonly IMapper _mapper;
        private readonly ILogger<ServiceRequestController> _logger;

        public ServiceRequestController(
            IServiceRequestService serviceRequestService,
            IFileService fileService,
            IMapper mapper,
            ILogger<ServiceRequestController> logger)
        {
            _serviceRequestService = serviceRequestService;
            _fileService = fileService;
            _mapper = mapper;
            _logger = logger;
        }

        // GET: Admin/ServiceRequest
        [HttpGet]
        public async Task<IActionResult> Index(string? status = null, string? priority = null, string? category = null)
        {
            try
            {
                var serviceRequests = await _serviceRequestService.GetAllAsync();
                
                // Filtreleme
                if (!string.IsNullOrEmpty(status) && Enum.TryParse<ServiceStatus>(status, out var statusEnum))
                {
                    serviceRequests = serviceRequests.Where(r => r.Status == statusEnum).ToList();
                }
                
                if (!string.IsNullOrEmpty(priority) && Enum.TryParse<Priority>(priority, out var priorityEnum))
                {
                    serviceRequests = serviceRequests.Where(r => r.Priority == priorityEnum).ToList();
                }
                
                if (!string.IsNullOrEmpty(category) && Enum.TryParse<ServiceCategory>(category, out var categoryEnum))
                {
                    serviceRequests = serviceRequests.Where(r => r.Category == categoryEnum).ToList();
                }

                return View(serviceRequests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin servis talepleri listelenirken hata oluştu");
                TempData["Error"] = "Servis talepleri yüklenirken bir hata oluştu.";
                return View(new List<ServiceRequestResponseDto>());
            }
        }

        // GET: Admin/ServiceRequest/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var serviceRequest = await _serviceRequestService.GetByIdAsync(id);
                if (serviceRequest == null)
                {
                    TempData["Error"] = "Servis talebi bulunamadı.";
                    return RedirectToAction(nameof(Index));
                }

                return View(serviceRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin servis talebi detayı yüklenirken hata: {Id}", id);
                TempData["Error"] = "Servis talebi detayları yüklenirken bir hata oluştu.";
                return RedirectToAction(nameof(Index));
            }
        }

        
        // POST: Admin/ServiceRequest/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
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
                _logger.LogError(ex, "Admin servis talebi silinirken hata: {Id}", id);
                TempData["Error"] = "Servis talebi silinirken bir hata oluştu.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/ServiceRequest/DownloadFile/5
        [HttpGet]
        public async Task<IActionResult> DownloadFile(int fileId)
        {
            try
            {
                var (stream, fileName, contentType) = await _fileService.DownloadFileAsync(fileId);
                return File(stream, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin dosya indirilirken hata: FileId {FileId}", fileId);
                TempData["Error"] = "Dosya indirilemedi.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/ServiceRequest/GetFile/5
        [HttpGet]
        public async Task<IActionResult> GetFile(int fileId)
        {
            try
            {
                var (stream, fileName, contentType) = await _fileService.DownloadFileAsync(fileId);
                
                // Resim dosyaları için inline görüntüleme
                if (contentType.StartsWith("image/"))
                {
                    return File(stream, contentType);
                }
                
                // Diğer dosyalar için download
                return File(stream, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin dosya görüntülenirken hata: FileId {FileId}", fileId);
                return NotFound();
            }
        }
    }
}