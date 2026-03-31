using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        // --- ส่วนการจัดการโปรโมชั่น (Promotions) ---
        public IActionResult Promotions()
        {
            // ดึงโปรโมชั่นทั้งหมดมาแสดง (เรียงจากวันที่สร้างล่าสุด หรือ ID ล่าสุด)
            // หมายเหตุ: แก้ไขชื่อ Property ให้ตรงกับ Model Promotion ของคุณนะครับ
            var promotions = _db.Promotions.ToList();
            return View(promotions);
        }

        [HttpGet]
        public IActionResult AddPromotion()
        {
            return View();
        }

        [HttpPost]
        public IActionResult AddPromotion(PromotionViewModel data)
        {
            if (ModelState.IsValid)
            {
                var newPromo = new Promotion
                {
                    Name = data.Name,
                    ConditionType = data.ConditionType,
                    ConditionValue = data.ConditionValue,
                    RewardType = data.RewardType,
                    RewardValue = data.RewardValue,
                    IsActive = true
                };

                _db.Promotions.Add(newPromo);
                _db.SaveChanges();
                return RedirectToAction("Promotions");
            }
            return View(data);
        }

        // ปุ่มเปิด/ปิดการใช้งานโปรโมชั่น (Toggle)
        public IActionResult TogglePromotion(int id)
        {
            var promo = _db.Promotions.Find(id);
            if (promo != null)
            {
                promo.IsActive = !promo.IsActive; // สลับสถานะ
                _db.SaveChanges();
            }
            return RedirectToAction("Promotions");
        }

        public IActionResult DeletePromotion(int id)
        {
            var promo = _db.Promotions.Find(id);
            if (promo != null)
            {
                _db.Promotions.Remove(promo);
                _db.SaveChanges();
            }
            return RedirectToAction("Promotions");
        }

        // --- ส่วนการตรวจสอบประวัติการขาย (Sales History) ---

        public IActionResult SalesHistory()
        {
            var orders = _db.Orders
                .Include(o => o.Cashier)
                .OrderByDescending(o => o.OrderDate)
                .ToList();
            return View(orders);
        }

        public IActionResult OrderDetails(int id)
        {
            var order = _db.Orders
                .Include(o => o.Cashier)
                .FirstOrDefault(o => o.OrderId == id);

            if (order == null) return NotFound();

            var details = _db.Orderdetails
                .Include(od => od.Product)
                .Where(od => od.OrderId == id)
                .ToList();

            ViewBag.OrderDetails = details;
            return View(order);
        }

        // --- ส่วนการรับสินค้าเข้าสต็อก ---
        [HttpGet]
        public IActionResult ReceiveStock()
        {
            // ดึงรายการสินค้าทั้งหมดมาทำ Dropdown โชว์รหัส, ชื่อ และสต็อกปัจจุบัน ให้ผู้จัดการดูง่ายๆ
            var products = _db.Products.Select(p => new
            {
                p.ProductId,
                DisplayText = $"[{p.Sku}] {p.Name} (มีอยู่: {p.StockQuantity})"
            }).ToList();

            ViewBag.ProductList = new SelectList(products, "ProductId", "DisplayText");
            return View();
        }

        [HttpPost]
        public IActionResult ReceiveStock(ReceiveStockViewModel data)
        {
            if (ModelState.IsValid)
            {
                var product = _db.Products.Find(data.ProductId);
                if (product != null)
                {
                    // เอาสต็อกเดิม + กับจำนวนที่รับเข้าใหม่
                    product.StockQuantity += data.QuantityAdded;

                    _db.SaveChanges();

                    TempData["SuccessMessage"] = $"เพิ่ม '{product.Name}' จำนวน {data.QuantityAdded} ชิ้น เข้าสต็อกเรียบร้อยแล้ว! (สต็อกใหม่: {product.StockQuantity})";

                    return RedirectToAction("ReceiveStock");
                }
                ModelState.AddModelError("", "ไม่พบสินค้านี้ในระบบ");
            }

            var products = _db.Products.Select(p => new
            {
                p.ProductId,
                DisplayText = $"[{p.Sku}] {p.Name} (มีอยู่: {p.StockQuantity})"
            }).ToList();
            ViewBag.ProductList = new SelectList(products, "ProductId", "DisplayText");

            return View(data);
        }
    }
}