using Microsoft.AspNetCore.Mvc;

namespace FoodShoppingApp.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
