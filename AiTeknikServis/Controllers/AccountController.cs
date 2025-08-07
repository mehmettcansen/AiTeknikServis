using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AiTeknikServis.Entities.Models;
using AiTeknikServis.Entities.Dtos.Auth;
using AiTeknikServis.Entities.Dtos.User;
using AiTeknikServis.Entities.Dtos.EmailVerification;
using AiTeknikServis.Services.Contracts;
using System.Security.Claims;

namespace AiTeknikServis.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ICustomerService _customerService;
        private readonly IEmailVerificationService _emailVerificationService;
        private readonly IUserValidationService _userValidationService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ICustomerService customerService,
            IEmailVerificationService emailVerificationService,
            IUserValidationService userValidationService,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _customerService = customerService;
            _emailVerificationService = emailVerificationService;
            _userValidationService = userValidationService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDto model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(
                    model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    
                    // Role-based yönlendirme
                    var user = await _userManager.FindByEmailAsync(model.Email);
                    if (user != null)
                    {
                        var roles = await _userManager.GetRolesAsync(user);
                        if (roles.Contains("Admin"))
                        {
                            return RedirectToAction("Dashboard", "Admin", new { area = "Admin" });
                        }
                        else if (roles.Contains("Manager"))
                        {
                            return RedirectToAction("Dashboard", "Manager");
                        }
                        else if (roles.Contains("Technician"))
                        {
                            return RedirectToAction("Dashboard", "Technician");
                        }
                        else if (roles.Contains("Customer"))
                        {
                            return RedirectToAction("Dashboard", "Customer");
                        }
                    }
                    
                    return RedirectToLocal(returnUrl);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Geçersiz giriş denemesi.");
                    return View(model);
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterDto model)
        {
            if (ModelState.IsValid)
            {
                // Email benzersizlik kontrolü
                var emailValidation = await _userValidationService.ValidateEmailForCustomerRegistrationAsync(model.Email);
                if (!emailValidation.IsValid)
                {
                    ModelState.AddModelError("Email", emailValidation.Message);
                    return View(model);
                }

                // Email doğrulama kodu gönder
                var verificationDto = new EmailVerificationCreateDto
                {
                    Email = model.Email,
                    Type = EmailVerificationType.CustomerRegistration,
                    Purpose = "Müşteri kayıt doğrulaması",
                    ExpiryMinutes = 15
                };

                var verificationResult = await _emailVerificationService.CreateVerificationCodeAsync(verificationDto);

                if (verificationResult.IsSuccess)
                {
                    // Geçici olarak model'i TempData'da sakla
                    TempData["RegisterModel"] = System.Text.Json.JsonSerializer.Serialize(model);
                    TempData["SuccessMessage"] = "Email adresinize doğrulama kodu gönderildi. Lütfen email'inizi kontrol edin.";
                    
                    return RedirectToAction("VerifyEmail", new { email = model.Email, type = EmailVerificationType.CustomerRegistration });
                }
                else
                {
                    ModelState.AddModelError(string.Empty, verificationResult.Message);
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult VerifyEmail(string email, EmailVerificationType type)
        {
            ViewBag.Email = email;
            ViewBag.Type = type;
            ViewBag.TypeDisplayName = GetTypeDisplayName(type);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyEmail(EmailVerificationVerifyDto model)
        {
            if (ModelState.IsValid)
            {
                var verificationResult = await _emailVerificationService.VerifyCodeAsync(model);

                if (verificationResult.IsSuccess)
                {
                    // Doğrulama başarılı - kayıt işlemini tamamla
                    if (model.Type == EmailVerificationType.CustomerRegistration)
                    {
                        return await CompleteCustomerRegistration();
                    }
                    else if (model.Type == EmailVerificationType.PasswordReset)
                    {
                        return RedirectToAction("ResetPassword", new { email = model.Email });
                    }
                    else if (model.Type == EmailVerificationType.UserCreation)
                    {
                        TempData["SuccessMessage"] = "Email doğrulama başarılı. Kullanıcı oluşturma işlemi tamamlanabilir.";
                        return RedirectToAction("CreateUser", "Admin", new { area = "Admin", email = model.Email });
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, verificationResult.Message);
                }
            }

            ViewBag.Email = model.Email;
            ViewBag.Type = model.Type;
            ViewBag.TypeDisplayName = GetTypeDisplayName(model.Type);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ResendVerificationCode(string email, EmailVerificationType type)
        {
            var result = await _emailVerificationService.ResendVerificationCodeAsync(email, type);
            
            if (result.IsSuccess)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction("VerifyEmail", new { email, type });
        }

        private async Task<IActionResult> CompleteCustomerRegistration()
        {
            try
            {
                var modelJson = TempData["RegisterModel"] as string;
                if (string.IsNullOrEmpty(modelJson))
                {
                    TempData["ErrorMessage"] = "Kayıt bilgileri bulunamadı. Lütfen tekrar kayıt olmayı deneyin.";
                    return RedirectToAction("Register");
                }

                var model = System.Text.Json.JsonSerializer.Deserialize<RegisterDto>(modelJson);
                if (model == null)
                {
                    TempData["ErrorMessage"] = "Kayıt bilgileri okunamadı. Lütfen tekrar kayıt olmayı deneyin.";
                    return RedirectToAction("Register");
                }

                // Önce Identity kullanıcısı oluştur
                var identityUser = new IdentityUser { UserName = model.Email, Email = model.Email };
                var result = await _userManager.CreateAsync(identityUser, model.Password);

                if (result.Succeeded)
                {
                    // Kullanıcıya Customer rolü ata
                    await _userManager.AddToRoleAsync(identityUser, "Customer");
                    
                    // Sonra Customer kaydı oluştur
                    var customerDto = new CustomerCreateDto
                    {
                        FirstName = model.FirstName ?? "",
                        LastName = model.LastName ?? "",
                        Email = model.Email,
                        Phone = model.Phone,
                        CompanyName = model.CompanyName ?? "",
                        ContactPreference = ContactPreference.Email,
                        IdentityUserId = identityUser.Id
                    };

                    await _customerService.CreateAsync(customerDto);
                    
                    _logger.LogInformation("User created a new account with email verification: {Email}", model.Email);
                    await _signInManager.SignInAsync(identityUser, isPersistent: false);
                    
                    TempData["SuccessMessage"] = "Kayıt işleminiz başarıyla tamamlandı!";
                    return RedirectToAction("Dashboard", "Customer");
                }

                foreach (var error in result.Errors)
                {
                    TempData["ErrorMessage"] = error.Description;
                }
                
                return RedirectToAction("Register");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Müşteri kaydı tamamlanırken hata oluştu");
                TempData["ErrorMessage"] = "Kayıt işlemi sırasında bir hata oluştu. Lütfen tekrar deneyin.";
                return RedirectToAction("Register");
            }
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    // Güvenlik için kullanıcı bulunamasa bile başarılı mesajı göster
                    TempData["SuccessMessage"] = "Eğer bu email adresi sistemde kayıtlıysa, şifre sıfırlama kodu gönderildi.";
                    return RedirectToAction("ForgotPassword");
                }

                // Email doğrulama kodu gönder
                var verificationDto = new EmailVerificationCreateDto
                {
                    Email = model.Email,
                    Type = EmailVerificationType.PasswordReset,
                    Purpose = "Şifre sıfırlama doğrulaması",
                    ExpiryMinutes = 15
                };

                var verificationResult = await _emailVerificationService.CreateVerificationCodeAsync(verificationDto);

                if (verificationResult.IsSuccess)
                {
                    TempData["SuccessMessage"] = "Şifre sıfırlama kodu email adresinize gönderildi.";
                    return RedirectToAction("VerifyEmail", new { email = model.Email, type = EmailVerificationType.PasswordReset });
                }
                else
                {
                    ModelState.AddModelError(string.Empty, verificationResult.Message);
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("ForgotPassword");
            }

            var model = new ResetPasswordDto { Email = email };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Kullanıcı bulunamadı.";
                    return RedirectToAction("ForgotPassword");
                }

                // Şifre sıfırlama token'ı oluştur ve şifreyi değiştir
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, model.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User reset password: {Email}", model.Email);
                    TempData["SuccessMessage"] = "Şifreniz başarıyla değiştirildi. Yeni şifrenizle giriş yapabilirsiniz.";
                    return RedirectToAction("Login");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        private string GetTypeDisplayName(EmailVerificationType type)
        {
            return type switch
            {
                EmailVerificationType.CustomerRegistration => "Müşteri Kaydı",
                EmailVerificationType.UserCreation => "Kullanıcı Oluşturma",
                EmailVerificationType.PasswordReset => "Şifre Sıfırlama",
                EmailVerificationType.EmailChange => "Email Değişikliği",
                EmailVerificationType.AccountActivation => "Hesap Aktivasyonu",
                _ => type.ToString()
            };
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                // Role-based yönlendirme yap
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("Dashboard", "Admin", new { area = "Admin" });
                }
                else if (User.IsInRole("Manager"))
                {
                    return RedirectToAction("Dashboard", "Manager");
                }
                else if (User.IsInRole("Technician"))
                {
                    return RedirectToAction("Dashboard", "Technician");
                }
                else if (User.IsInRole("Customer"))
                {
                    return RedirectToAction("Dashboard", "Customer");
                }
                
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }
    }
}