using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FertilizerShop.Models;
using FertilizerShop.ViewModels;
using System.Security.Claims;
using System.Text.Json; // เพิ่มบรรทัดนี้สำหรับส่ง Json

namespace FertilizerShop.Controllers
{
    [Authorize(Roles = "Cashier,Owner,Manager")] 
    public class CashierController : Controller
    {
        private readonly FertilizershopdbContext _db;

        public CashierController(FertilizershopdbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            var products = _db.Products.Include(p => p.Category).Where(p => p.StockQuantity > 0).ToList();
            ViewBag.Categories = _db.Categories.ToList();

            // ดึงโปรโมชั่นที่กำลัง "เปิดใช้งาน" ส่งไปให้หน้า POS คำนวณ
            var activePromos = _db.Promotions.Where(p => p.IsActive == true).ToList();
            ViewBag.PromotionsJson = JsonSerializer.Serialize(activePromos);

            return View(products);
        }

        [HttpPost]
        public IActionResult Checkout([FromBody] CheckoutViewModel data)
        {
            if (data.CartItems == null || data.CartItems.Count == 0)
                return Json(new { success = false, message = "ตะกร้าสินค้าว่างเปล่า" });

            try
            {
                var cashierId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                string receiptNo = "REC" + DateTime.Now.ToString("yyyyMMddHHmmss");

                decimal totalAmount = data.CartItems.Sum(item => item.Price * item.Qty);
                // ยอดสุทธิ = ยอดรวม - ส่วนลดที่หน้าเว็บคำนวณมาให้
                decimal netAmount = totalAmount - data.DiscountAmount; 

                var newOrder = new Order
                {
                    ReceiptNo = receiptNo,
                    OrderDate = DateTime.Now,
                    CashierId = cashierId,
                    TotalAmount = totalAmount,
                    DiscountAmount = data.DiscountAmount,
                    NetAmount = netAmount,
                    PaymentMethod = data.PaymentMethod
                };

                _db.Orders.Add(newOrder);
                _db.SaveChanges(); 

                foreach (var item in data.CartItems)
                {
                    var orderDetail = new Orderdetail
                    {
                        OrderId = newOrder.OrderId,
                        ProductId = item.Id,
                        Quantity = item.Qty,
                        UnitPrice = item.Price,
                        SubTotal = item.Price * item.Qty 
                    };
                    _db.Orderdetails.Add(orderDetail);

                    var product = _db.Products.Find(item.Id);
                    if (product != null) product.StockQuantity -= item.Qty;
                }

                _db.SaveChanges();
                return Json(new { success = true, orderId = newOrder.OrderId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        public IActionResult Receipt(int id)
        {
            // ดึงข้อมูลบิล พร้อมรายละเอียดสินค้าและชื่อพนักงานขาย
            var order = _db.Orders
                           .FirstOrDefault(o => o.OrderId == id);

            if (order == null) return NotFound();

            // ดึงรายการสินค้าในบิลนี้
            ViewBag.OrderDetails = (from od in _db.Orderdetails
                join p in _db.Products on od.ProductId equals p.ProductId
                where od.OrderId == id
                select new { 
                    ProductName = p.Name, 
                    Qty = od.Quantity, 
                    Price = od.UnitPrice, 
                    SubTotal = od.SubTotal 
                }).ToList();

            return View(order);
        }
    }
}