using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SeiPDFManagement.Models;

namespace SeiPDFManagement.Controllers
{
    /// <summary>
    /// Controller della pagina principale dell'applicazione.
    /// </summary>
    public class HomeController(ILogger<HomeController> logger) : Controller
    {
        private readonly ILogger<HomeController> _logger = logger;

        /// <summary>
        /// Pagina di presentazione di SeiPDF Management.
        /// </summary>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Pagina mostrata in caso di accesso non autorizzato.
        /// </summary>
        public IActionResult AccessoNegato()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
