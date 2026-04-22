using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using FertilizerShop.Models;
using FertilizerShop.ViewModels;

namespace FertilizerShop.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerController : Controller
    {
        private readonly FertilizershopdbContext _db;

        public CustomerController(FertilizershopdbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            // ดึง ID ของลูกค้าที่ล็อกอินอยู่จากระบบ (Claims)
            var customerIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(customerIdStr, out int customerId)) 
            {
                return RedirectToAction("Login", "CustomerAuth");
            }

            // ดึงข้อมูลส่วนตัวของลูกค้า
            var customer = _db.Customers.Find(customerId);
            if (customer == null) return NotFound();

            // ดึงประวัติการซื้อ (พร้อมรายละเอียดสินค้าในบิล)
            var orders = _db.Orders
                            .Include(o => o.Orderdetails)
                                .ThenInclude(od => od.Product)
                            .Where(o => o.CustomerId == customerId)
                            .OrderByDescending(o => o.OrderDate)
                            .ToList();

            // ประกอบร่างส่งให้ View
            var model = new CustomerProfileViewModel
            {
                FullName = customer.FullName,
                Phone = customer.Phone,
                RewardPoints = customer.RewardPoints ?? 0,
                TotalWeightBought = customer.TotalWeightBought ?? 0,
                MemberSince = customer.CreatedAt,
                OrderHistory = orders
            };

            ViewBag.MyClaims = _db.Claims.Where(c => c.CustomerId == customerId).ToList();

            return View(model);
        }

        // --- หน้าจอแบบฟอร์มแจ้งเคลม ---
        [HttpGet]
        public IActionResult CreateClaim(int orderId, int productId)
        {
            var customerIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(customerIdStr, out int customerId)) return RedirectToAction("Login", "CustomerAuth");

            // ดึงรายละเอียดสินค้าในบิลที่ลูกค้าคลิกมา
            var orderDetail = _db.Orderdetails
                                 .Include(od => od.Order)
                                 .Include(od => od.Product)
                                 .FirstOrDefault(od => od.OrderId == orderId && od.ProductId == productId && od.Order.CustomerId == customerId);

            if (orderDetail == null) return NotFound();

            // ป้องกันคนพิมพ์ URL มาเคลมซ้ำ
            bool isAlreadyClaimed = _db.Claims.Any(c => c.OrderId == orderId && c.ProductId == productId);
            if (isAlreadyClaimed)
            {
                TempData["Error"] = "สินค้านี้ถูกแจ้งเคลมไปแล้ว ไม่สามารถแจ้งซ้ำได้";
                return RedirectToAction("Index");
            }

            ViewBag.OrderDetail = orderDetail;
            return View();
        }

        // --- รับข้อมูลเมื่อลูกค้ากดส่งฟอร์ม ---
        [HttpPost]
        public IActionResult CreateClaim(int orderId, int productId, string problemDescription)
        {
            var customerIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(customerIdStr, out int customerId)) return RedirectToAction("Login", "CustomerAuth");

            // ป้องกันการกดย้ำๆ (Double Submit)
            if (_db.Claims.Any(c => c.OrderId == orderId && c.ProductId == productId))
            {
                return RedirectToAction("Index");
            }

            var claim = new FertilizerShop.Models.Claim
            {
                CustomerId = customerId,
                OrderId = orderId,
                ProductId = productId,
                ProblemDescription = problemDescription,
                Status = "Pending", 
                CreatedAt = DateTime.Now
            };

            _db.Claims.Add(claim);
            _db.SaveChanges();

            TempData["Success"] = "ส่งเรื่องแจ้งเคลมสำเร็จ! ทางร้านจะตรวจสอบและติดต่อกลับโดยเร็วที่สุด";
            return RedirectToAction("Index");
        }
    }
}