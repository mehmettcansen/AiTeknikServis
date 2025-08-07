using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using AutoMapper;
using AiTeknikServis.Services.Contracts;
using AiTeknikServis.Entities.Dtos.User;
using AiTeknikServis.Entities.Dtos.EmailVerification;
using AiTeknikServis.Entities.Models;
using AiTeknikServis.Repositories.Contracts;
using System.Security.Claims;

namespace AiTeknikServis.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserRepository _userRepository;
        private readonly IServiceRequestRepository _serviceRequestRepository;
        private readonly IEmailVerificationService _emailVerificationService;
        private readonly IUserValidationService _userValidationService;
        private readonly IWorkAssignmentService _workAssignmentService;
        private readonly IMapper _mapper;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            UserManager<IdentityUser> userManager,
            IUserRepository userRepository,
            IServiceRequestRepository serviceRequestRepository,
            IEmailVerificationService emailVerificationService,
            IUserValidationService userValidationService,
            IWorkAssignmentService workAssignmentService,
            IMapper mapper,
            ILogger<AdminController> logger)
        {
            _userManager = userManager;
            _userRepository = userRepository;
            _serviceRequestRepository = serviceRequestRepository;
            _emailVerificationService = emailVerificationService;
            _userValidationService = userValidationService;
            _workAssignmentService = workAssignmentService;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var users = await _userRepository.GetAllAsync();
            return View(users);
        }

        [HttpGet]
        public IActionResult CreateManager()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateManager(ManagerCreateDto model)
        {
            if (ModelState.IsValid)
            {
                // Email benzersizlik kontrolü
                var emailValidation = await _userValidationService.ValidateEmailForUserCreationAsync(model.Email, "Manager");
                if (!emailValidation.IsValid)
                {
                    ModelState.AddModelError("Email", emailValidation.Message);
                    return View(model);
                }

                // Email doğrulama kodu gönder
                var verificationDto = new EmailVerificationCreateDto
                {
                    Email = model.Email,
                    Type = EmailVerificationType.UserCreation,
                    Purpose = "Manager oluşturma doğrulaması",
                    ExpiryMinutes = 15,
                    AdditionalData = System.Text.Json.JsonSerializer.Serialize(new { UserType = "Manager" })
                };

                var verificationResult = await _emailVerificationService.CreateVerificationCodeAsync(verificationDto);

                if (verificationResult.IsSuccess)
                {
                    // Geçici olarak model'i TempData'da sakla
                    TempData["ManagerModel"] = System.Text.Json.JsonSerializer.Serialize(model);
                    TempData["SuccessMessage"] = "Email doğrulama kodu gönderildi. Lütfen email'i kontrol edin.";
                    
                    return RedirectToAction("VerifyUserCreation", new { email = model.Email, userType = "Manager" });
                }
                else
                {
                    ModelState.AddModelError(string.Empty, verificationResult.Message);
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult CreateTechnician()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTechnician(TechnicianCreateDto model)
        {
            if (ModelState.IsValid)
            {
                // Email benzersizlik kontrolü
                var emailValidation = await _userValidationService.ValidateEmailForUserCreationAsync(model.Email, "Technician");
                if (!emailValidation.IsValid)
                {
                    ModelState.AddModelError("Email", emailValidation.Message);
                    return View(model);
                }

                // Email doğrulama kodu gönder
                var verificationDto = new EmailVerificationCreateDto
                {
                    Email = model.Email,
                    Type = EmailVerificationType.UserCreation,
                    Purpose = "Teknisyen oluşturma doğrulaması",
                    ExpiryMinutes = 15,
                    AdditionalData = System.Text.Json.JsonSerializer.Serialize(new { UserType = "Technician" })
                };

                var verificationResult = await _emailVerificationService.CreateVerificationCodeAsync(verificationDto);

                if (verificationResult.IsSuccess)
                {
                    // Geçici olarak model'i TempData'da sakla
                    TempData["TechnicianModel"] = System.Text.Json.JsonSerializer.Serialize(model);
                    TempData["SuccessMessage"] = "Email doğrulama kodu gönderildi. Lütfen email'i kontrol edin.";
                    
                    return RedirectToAction("VerifyUserCreation", new { email = model.Email, userType = "Technician" });
                }
                else
                {
                    ModelState.AddModelError(string.Empty, verificationResult.Message);
                }
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Users()
        {
            var users = await _userRepository.GetAllAsync();
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user != null && !string.IsNullOrEmpty(user.IdentityUserId))
            {
                var identityUser = await _userManager.FindByIdAsync(user.IdentityUserId);
                if (identityUser != null)
                {
                    await _userManager.DeleteAsync(identityUser);
                }
                await _userRepository.DeleteAsync(id);
            }
            
            return RedirectToAction("Users");
        }

        [HttpGet]
        public IActionResult VerifyUserCreation(string email, string userType)
        {
            ViewBag.Email = email;
            ViewBag.UserType = userType;
            ViewBag.TypeDisplayName = $"{userType} Oluşturma";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyUserCreation(EmailVerificationVerifyDto model, string userType)
        {
            if (ModelState.IsValid)
            {
                var verificationResult = await _emailVerificationService.VerifyCodeAsync(model);

                if (verificationResult.IsSuccess)
                {
                    // Doğrulama başarılı - kullanıcı oluşturma işlemini tamamla
                    if (userType == "Manager")
                    {
                        return await CompleteManagerCreation();
                    }
                    else if (userType == "Technician")
                    {
                        return await CompleteTechnicianCreation();
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, verificationResult.Message);
                }
            }

            ViewBag.Email = model.Email;
            ViewBag.UserType = userType;
            ViewBag.TypeDisplayName = $"{userType} Oluşturma";
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ResendUserCreationCode(string email, string userType)
        {
            var result = await _emailVerificationService.ResendVerificationCodeAsync(email, EmailVerificationType.UserCreation);
            
            if (result.IsSuccess)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction("VerifyUserCreation", new { email, userType });
        }

        private async Task<IActionResult> CompleteManagerCreation()
        {
            try
            {
                var modelJson = TempData["ManagerModel"] as string;
                if (string.IsNullOrEmpty(modelJson))
                {
                    TempData["ErrorMessage"] = "Manager bilgileri bulunamadı. Lütfen tekrar deneyin.";
                    return RedirectToAction("CreateManager");
                }

                var model = System.Text.Json.JsonSerializer.Deserialize<ManagerCreateDto>(modelJson);
                if (model == null)
                {
                    TempData["ErrorMessage"] = "Manager bilgileri okunamadı. Lütfen tekrar deneyin.";
                    return RedirectToAction("CreateManager");
                }

                // Önce Identity kullanıcısı oluştur
                var identityUser = new IdentityUser { UserName = model.Email, Email = model.Email };
                var result = await _userManager.CreateAsync(identityUser, "Manager123!");

                if (result.Succeeded)
                {
                    // Kullanıcıya Manager rolü ata
                    await _userManager.AddToRoleAsync(identityUser, "Manager");
                    
                    // Sonra Manager kaydı oluştur
                    var manager = _mapper.Map<AiTeknikServis.Entities.Models.Manager>(model);
                    manager.IdentityUserId = identityUser.Id;
                    await _userRepository.CreateAsync(manager);
                    
                    _logger.LogInformation("Manager created successfully with email verification: {Email}", model.Email);
                    TempData["SuccessMessage"] = "Manager başarıyla oluşturuldu!";
                    return RedirectToAction("Dashboard");
                }

                foreach (var error in result.Errors)
                {
                    TempData["ErrorMessage"] = error.Description;
                }
                
                return RedirectToAction("CreateManager");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Manager oluşturulurken hata oluştu");
                TempData["ErrorMessage"] = "Manager oluşturma sırasında bir hata oluştu. Lütfen tekrar deneyin.";
                return RedirectToAction("CreateManager");
            }
        }

        private async Task<IActionResult> CompleteTechnicianCreation()
        {
            try
            {
                var modelJson = TempData["TechnicianModel"] as string;
                if (string.IsNullOrEmpty(modelJson))
                {
                    TempData["ErrorMessage"] = "Teknisyen bilgileri bulunamadı. Lütfen tekrar deneyin.";
                    return RedirectToAction("CreateTechnician");
                }

                var model = System.Text.Json.JsonSerializer.Deserialize<TechnicianCreateDto>(modelJson);
                if (model == null)
                {
                    TempData["ErrorMessage"] = "Teknisyen bilgileri okunamadı. Lütfen tekrar deneyin.";
                    return RedirectToAction("CreateTechnician");
                }

                // Önce Identity kullanıcısı oluştur
                var identityUser = new IdentityUser { UserName = model.Email, Email = model.Email };
                var result = await _userManager.CreateAsync(identityUser, "Technician123!");

                if (result.Succeeded)
                {
                    // Kullanıcıya Technician rolü ata
                    await _userManager.AddToRoleAsync(identityUser, "Technician");
                    
                    // Sonra Technician kaydı oluştur
                    var technician = _mapper.Map<Technician>(model);
                    technician.IdentityUserId = identityUser.Id;
                    await _userRepository.CreateAsync(technician);
                    
                    _logger.LogInformation("Technician created successfully with email verification: {Email}", model.Email);
                    TempData["SuccessMessage"] = "Teknisyen başarıyla oluşturuldu!";
                    return RedirectToAction("Dashboard");
                }

                foreach (var error in result.Errors)
                {
                    TempData["ErrorMessage"] = error.Description;
                }
                
                return RedirectToAction("CreateTechnician");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Teknisyen oluşturulurken hata oluştu");
                TempData["ErrorMessage"] = "Teknisyen oluşturma sırasında bir hata oluştu. Lütfen tekrar deneyin.";
                return RedirectToAction("CreateTechnician");
            }
        }

        /// <summary>
        /// Detaylı teknisyen iş yükü bilgilerini JSON olarak döndürür
        /// </summary>
        [HttpGet]
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

        /// <summary>
        /// Teknisyeni pasifleştirir/aktifleştirir
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleTechnicianStatus(int id)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(id);
                if (user == null || user.Role != UserRole.Technician)
                {
                    TempData["Error"] = "Teknisyen bulunamadı.";
                    return RedirectToAction("Users");
                }

                var technician = user as Technician;
                if (technician == null)
                {
                    TempData["Error"] = "Teknisyen bilgileri alınamadı.";
                    return RedirectToAction("Users");
                }

                // Durumu tersine çevir
                technician.IsActive = !technician.IsActive;
                await _userRepository.UpdateAsync(technician);

                // Eğer teknisyen pasifleştiriliyorsa, tamamlanmamış servis taleplerini "Pending" yap
                if (!technician.IsActive)
                {
                    await ResetIncompleteServiceRequestsAsync(technician.Id);
                }

                // Identity kullanıcısının da durumunu güncelle
                if (!string.IsNullOrEmpty(technician.IdentityUserId))
                {
                    var identityUser = await _userManager.FindByIdAsync(technician.IdentityUserId);
                    if (identityUser != null)
                    {
                        // Pasif teknisyenin giriş yapmasını engelle
                        identityUser.LockoutEnabled = !technician.IsActive;
                        if (!technician.IsActive)
                        {
                            // Pasifse lockout süresini çok uzun yap
                            identityUser.LockoutEnd = DateTimeOffset.MaxValue;
                        }
                        else
                        {
                            // Aktifse lockout'u kaldır
                            identityUser.LockoutEnd = null;
                        }
                        await _userManager.UpdateAsync(identityUser);
                    }
                }

                var statusText = technician.IsActive ? "aktifleştirildi" : "pasifleştirildi";
                TempData["Success"] = $"Teknisyen başarıyla {statusText}.";
                
                _logger.LogInformation("Teknisyen durumu değiştirildi: {TechnicianId}, Yeni Durum: {IsActive}", 
                    technician.Id, technician.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Teknisyen durumu değiştirilirken hata: {Id}", id);
                TempData["Error"] = "Teknisyen durumu değiştirilirken bir hata oluştu.";
            }

            return RedirectToAction("Users");
        }

        /// <summary>
        /// Pasifleştirilen teknisyenin tamamlanmamış servis taleplerini "Pending" durumuna getirir
        /// </summary>
        private async Task ResetIncompleteServiceRequestsAsync(int technicianId)
        {
            try
            {
                // Teknisyene atanmış ve tamamlanmamış servis taleplerini bul
                var incompleteRequests = await _serviceRequestRepository.GetByTechnicianIdAsync(technicianId);
                var requestsToReset = incompleteRequests.Where(sr => 
                    sr.Status == ServiceStatus.InProgress || 
                    sr.Status == ServiceStatus.OnHold).ToList();

                foreach (var request in requestsToReset)
                {
                    // Teknisyen atamasını kaldır ve durumu "Pending" yap
                    request.AssignedTechnicianId = null;
                    request.Status = ServiceStatus.Pending;
                    
                    await _serviceRequestRepository.UpdateAsync(request);
                    
                    _logger.LogInformation("Servis talebi teknisyen pasifleştirme nedeniyle 'Pending' durumuna getirildi: " +
                        "ServiceRequestId {ServiceRequestId}, TechnicianId {TechnicianId}", 
                        request.Id, technicianId);
                }

                if (requestsToReset.Any())
                {
                    _logger.LogInformation("Pasifleştirilen teknisyenin {Count} adet tamamlanmamış servis talebi 'Pending' durumuna getirildi. " +
                        "TechnicianId: {TechnicianId}", requestsToReset.Count, technicianId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Teknisyenin tamamlanmamış servis talepleri sıfırlanırken hata: TechnicianId {TechnicianId}", technicianId);
                // Bu hata ana işlemi durdurmaz, sadece log'lanır
            }
        }
    }
}