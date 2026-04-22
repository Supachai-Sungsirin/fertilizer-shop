using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FertilizerShop.Models;
using FertilizerShop.ViewModels;
using System.Security.Claims;
using System.Text.Json;

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

        [HttpGet]
        public IActionResult CheckCustomer(string phone)
        {
            if (string.IsNullOrEmpty(phone)) return Json(new { success = false });

            var customer = _db.Customers.FirstOrDefault(c => c.Phone == phone);
            if (customer != null)
            {
                // ถ้าเจอ ส่งชื่อและแต้มกลับไปโชว์
                return Json(new { success = true, name = customer.FullName, points = customer.RewardPoints ?? 0 });
            }
            return Json(new { success = false });
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
                decimal netAmount = totalAmount - data.DiscountAmount;
                Customer customer = null;
                if (!string.IsNullOrEmpty(data.CustomerPhone))
                {
                    customer = _db.Customers.FirstOrDefault(c => c.Phone == data.CustomerPhone);
                }
                var newOrder = new Order
                {
                    ReceiptNo = receiptNo,
                    OrderDate = DateTime.Now,
                    CashierId = cashierId,
                    TotalAmount = totalAmount,
                    DiscountAmount = data.DiscountAmount,
                    NetAmount = netAmount,
                    PaymentMethod = data.PaymentMethod,
                    CustomerId = customer?.CustomerId
                };

                _db.Orders.Add(newOrder);
                _db.SaveChanges(); 

                decimal totalWeightInOrder = 0;

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
                    if (product != null) 
                    {
                        product.StockQuantity -= item.Qty; 
                        
                        totalWeightInOrder += (product.WeightPerUnit * item.Qty); 
                    }
                }

                if (customer != null)
                {
                    int earnedPoints = (int)(netAmount / 100);
                    customer.RewardPoints = (customer.RewardPoints ?? 0) + earnedPoints;

                    customer.TotalWeightBought = (customer.TotalWeightBought ?? 0) + totalWeightInOrder; 
                    
                    _db.Customers.Update(customer);
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
            var order = _db.Orders
                           .Include(o => o.Customer)
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