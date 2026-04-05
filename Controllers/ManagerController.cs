using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FertilizerShop.Models;
using FertilizerShop.ViewModels;
using System.Security.Claims;

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
                    RewardItemName = data.RewardItemName,
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

        // --- ส่วนระบบใบสั่งซื้อ (Purchase Order) ---
        [HttpGet]
        public IActionResult CreatePO()
        {
            // ดึงรายชื่อ Supplier มาทำ Dropdown
            // หมายเหตุ: เช็คชื่อ Property ของ Model Supplier ให้ตรงกับของคุณด้วยนะครับ
            ViewBag.Suppliers = new SelectList(_db.Suppliers, "SupplierId", "Name");

            // ดึงรายชื่อสินค้าทั้งหมดส่งไปให้ JavaScript วาด Dropdown
            ViewBag.ProductsList = _db.Products.Select(p => new
            {
                id = p.ProductId,
                name = p.Name,
                sku = p.Sku
            }).ToList();

            return View();
        }

        [HttpPost]
        public IActionResult CreatePO([FromBody] CreatePOViewModel data)
        {
            if (data == null || data.Items == null || !data.Items.Any())
            {
                return Json(new { success = false, message = "กรุณาเพิ่มสินค้าอย่างน้อย 1 รายการ" });
            }

            try
            {
                // ดึง ID ผู้จัดการที่กดสั่งซื้อ
                var managerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                // 1. สร้างหัวบิลสั่งซื้อ (PO)
                var po = new Purchaseorder
                {
                    SupplierId = data.SupplierId,
                    ManagerId = managerId,
                    OrderDate = DateTime.Now,
                    Status = "Pending", // สร้างบิลใหม่ สถานะคือ "รอของมาส่ง"
                    TotalAmount = data.Items.Sum(i => i.Qty * i.UnitCost)
                };

                _db.Purchaseorders.Add(po);
                _db.SaveChanges(); // เซฟเพื่อให้ได้ PoId กลับมาก่อน

                // 2. บันทึกรายละเอียดสินค้าในบิล
                foreach (var item in data.Items)
                {
                    var detail = new Purchaseorderdetail
                    {
                        PoId = po.PoId,
                        ProductId = item.ProductId,
                        Quantity = item.Qty,
                        UnitCost = item.UnitCost,
                        SubTotal = item.Qty * item.UnitCost
                    };
                    _db.Purchaseorderdetails.Add(detail);
                }

                _db.SaveChanges();
                return Json(new { success = true, poId = po.PoId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        // --- ส่วนการจัดการใบสั่งซื้อ (Purchase Order History & Receiving) ---
        public IActionResult POHistory()
        {
            // ดึงใบสั่งซื้อทั้งหมด เรียงจากล่าสุดขึ้นก่อน
            // *หมายเหตุ: เช็คชื่อ Navigation Property ใน Models/Purchaseorder.cs ของคุณด้วยนะครับ
            // มันอาจจะชื่อ Supplier หรือ Manager / ManagerNavigation
            var pos = _db.Purchaseorders
                         .Include(p => p.Supplier)
                         .OrderByDescending(p => p.OrderDate)
                         .ToList();
            return View(pos);
        }

        public IActionResult PODetails(int id)
        {
            var po = _db.Purchaseorders
                        .Include(p => p.Supplier)
                        .FirstOrDefault(p => p.PoId == id);

            if (po == null) return NotFound();

            // ดึงรายละเอียดสินค้าในบิล PO
            var details = _db.Purchaseorderdetails
                             .Include(d => d.Product)
                             .Where(d => d.PoId == id)
                             .ToList();

            ViewBag.PODetails = details;
            return View(po);
        }

        [HttpPost]
        public IActionResult MarkPOAsReceived(int id)
        {
            var po = _db.Purchaseorders.Find(id);
            if (po != null && po.Status == "Pending")
            {
                // 1. เปลี่ยนสถานะบิล
                po.Status = "Received";

                // 2. ดึงรายการสินค้าในบิลนี้มาวนลูปบวกสต็อก
                var details = _db.Purchaseorderdetails.Where(d => d.PoId == id).ToList();
                foreach (var item in details)
                {
                    var product = _db.Products.Find(item.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity += item.Quantity;
                    }
                }

                _db.SaveChanges();
                TempData["SuccessMessage"] = $"รับสินค้าจากใบสั่งซื้อ PO-{po.PoId} เข้าสต็อกเรียบร้อยแล้ว!";
            }
            return RedirectToAction("PODetails", new { id = id });
        }

        // --- หน้ารายการเคลมสินค้าทั้งหมด ---
        public IActionResult Claims()
        {
            // ดึงข้อมูลการเคลมทั้งหมด พ่วงข้อมูลลูกค้า, บิล และ สินค้า มาด้วย
            var claims = _db.Claims
                            .Include(c => c.Customer)
                            .Include(c => c.Order)
                            .Include(c => c.Product)
                            .OrderByDescending(c => c.CreatedAt)
                            .ToList();

            return View(claims);
        }

        // --- อนุมัติ/ปฏิเสธ ---
        [HttpPost]
        public IActionResult UpdateClaimStatus(int claimId, string newStatus)
        {
            var claim = _db.Claims.FirstOrDefault(c => c.ClaimId == claimId);
            if (claim != null)
            {
                claim.Status = newStatus;
                _db.SaveChanges();
                TempData["Success"] = "อัปเดตสถานะคำร้องเคลมสำเร็จ!";
            }
            return RedirectToAction("Claims");
        }
    }
}