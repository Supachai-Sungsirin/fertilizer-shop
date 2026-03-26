using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FertilizerShop.Models;
using FertilizerShop.ViewModels;

namespace FertilizerShop.Controllers
{
    // กำหนดให้เข้าได้เฉพาะ Manager และ Owner
    [Authorize(Roles = "Manager,Owner")]
    public class ManagerController : Controller
    {
        private readonly FertilizershopdbContext _db;

        public ManagerController(FertilizershopdbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            var today = DateTime.Today;
            var todaysOrders = _db.Orders.Where(o => o.OrderDate != null && o.OrderDate.Value.Date == today).ToList();

            // ดึงสินค้าสต็อกน้อยกว่าหรือเท่ากับ 10
            var lowStock = _db.Products
                              .Include(p => p.Category)
                              .Where(p => p.StockQuantity <= 10)
                              .OrderBy(p => p.StockQuantity)
                              .ToList();

            // ดึงสินค้าที่กำลังจะหมดอายุภายใน 30 วันข้างหน้า
            var todayDateOnly = DateOnly.FromDateTime(today);
            var next30Days = todayDateOnly.AddDays(30);

            var expiring = _db.Products
                              .Include(p => p.Category)
                              .Where(p => p.ExpiryDate != null && p.ExpiryDate >= todayDateOnly && p.ExpiryDate <= next30Days)
                              .OrderBy(p => p.ExpiryDate)
                              .ToList();

            // แพ็คข้อมูลใส่ ViewModel
            var model = new ManagerDashboardViewModel
            {
                TodaySales = todaysOrders.Sum(o => o.TotalAmount),
                TodayOrdersCount = todaysOrders.Count,
                LowStockProducts = lowStock,
                ExpiringProducts = expiring
            };

            return View(model);
        }
    }
}