using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FertilizerShop.Models;
using Microsoft.AspNetCore.Authorization;

namespace FertilizerShop.Controllers;

public class HomeController : Controller
{   
    private readonly FertilizershopdbContext _db;
    public HomeController(FertilizershopdbContext db)
    {
        _db = db;
    }
    public IActionResult Index()
    {
        ViewBag.WelcomeMessage = "ยินดีต้อนรับสู่ ชาติชายฟาร์ม";
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
