using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FertilizerShop.Models;

namespace FertilizerShop.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        ViewBag.WelcomeMessage = "ยินดีต้อนรับสู่ FertilizerShop ระบบจัดการร้านขายปุ๋ย";
        return View();
    }

    public IActionResult About()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
