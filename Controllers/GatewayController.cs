using Microsoft.AspNetCore.Mvc;

namespace FertilizerShop.Controllers
{
    public class GatewayController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}