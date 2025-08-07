using System.Diagnostics;
using AiTeknikServis.Models;
using Microsoft.AspNetCore.Mvc;

namespace AiTeknikServis.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            // Eğer kullanıcı giriş yapmışsa, rolüne göre yönlendir
            if (User.Identity.IsAuthenticated)
            {
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
            }
            
            return View();
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
