using Microsoft.AspNetCore.Mvc;
using Pidar.Models;
using System.Diagnostics;

namespace Pidar.Controllers
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
            return RedirectToAction("Index", "Metadatas");
        }
        //public IActionResult Index()
        //{
        //    ViewData["ActivePage"] = "Home";
        //    return View();

        //}

        public IActionResult Statistic()
        {
            ViewData["ActivePage"] = "Statistic";
            return View();
        }
        public IActionResult Contribute()
        {
            ViewData["ActivePage"] = "Contribute";
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
