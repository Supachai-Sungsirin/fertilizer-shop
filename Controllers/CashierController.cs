using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FertilizerShop.Models;
using System.Security.Claims;
using FertilizerShop.ViewModels;

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
            // ดึงเฉพาะสินค้าที่มีสต็อกมากกว่า 0 มาขาย (Include หมวดหมู่มาด้วย)
            var products = _db.Products
                              .Include(p => p.Category)
                              .Where(p => p.StockQuantity > 0)
                              .ToList();

            // ดึงหมวดหมู่ทั้งหมดไปทำปุ่ม Filter กรองสินค้า
            ViewBag.Categories = _db.Categories.ToList();

            return View(products);
        }
        [HttpPost]
        public IActionResult Checkout([FromBody] List<CartItemViewModel> cartItems)
        {
            if (cartItems == null || cartItems.Count == 0)
            {
                return Json(new { success = false, message = "ตะกร้าสินค้าว่างเปล่า" });
            }

            try
            {
                // ดึง ID ของพนักงานที่กำลังล็อกอินอยู่ 
                var cashierId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                // สร้างเลขที่ใบเสร็จแบบอัตโนมัติ (เช่น REC20260326103015)
                string receiptNo = "REC" + DateTime.Now.ToString("yyyyMMddHHmmss");

                // คำนวณยอดรวมทั้งหมด
                decimal totalAmount = cartItems.Sum(item => item.Price * item.Qty);

                // สร้างหัวบิล (Order) 
                var newOrder = new Order
                {
                    ReceiptNo = receiptNo,
                    OrderDate = DateTime.Now,
                    CashierId = cashierId,
                    // CustomerId = null,
                    TotalAmount = totalAmount,
                    DiscountAmount = 0,
                    NetAmount = totalAmount,
                    PaymentMethod = "Cash"
                };

                _db.Orders.Add(newOrder);
                _db.SaveChanges();

                // วนลูปสินค้าในตะกร้า เพื่อสร้างรายละเอียดบิล (OrderDetail) และ ตัดสต็อก
                foreach (var item in cartItems)
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

                    // ตัดสต็อกสินค้า
                    var product = _db.Products.Find(item.Id);
                    if (product != null)
                    {
                        product.StockQuantity -= item.Qty;
                    }
                }

                _db.SaveChanges();

                return Json(new { success = true, orderId = newOrder.OrderId, receiptNo = newOrder.ReceiptNo });
            }
            catch (Exception ex)
            {
                // ถ้ามี Error จะส่งข้อความกลับไปโชว์ที่หน้าเว็บ
                return Json(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }
    }
}