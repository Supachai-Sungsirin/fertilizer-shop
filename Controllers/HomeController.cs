using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FertilizerShop.Models;

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
        var products = _db.Products.OrderBy(p => p.Name).ToList();
        
        return View(products);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    // --- ส่วนแสดงรายละเอียดสินค้าสำหรับลูกค้า ---
    public IActionResult Details(int id)
    {
        // ค้นหาสินค้าจาก ID ที่ส่งมา
        var product = _db.Products.FirstOrDefault(p => p.ProductId == id);
        
        if (product == null)
        {
            return NotFound();
        }

        return View(product);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}