using Microsoft.AspNetCore.Mvc;

namespace WebApplication3.Views
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
