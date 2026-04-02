using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using FertilizerShop.Models;
using FertilizerShop.ViewModels;

namespace FertilizerShop.Controllers
{
    // 🌟 ล็อคไว้ให้เฉพาะคนที่มี Role เป็น "Customer" เท่านั้น
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
            // 1. ดึง ID ของลูกค้าที่ล็อกอินอยู่จากระบบ (Claims)
            var customerIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(customerIdStr, out int customerId)) 
            {
                return RedirectToAction("Login", "CustomerAuth");
            }

            // 2. ดึงข้อมูลส่วนตัวของลูกค้า
            var customer = _db.Customers.Find(customerId);
            if (customer == null) return NotFound();

            // 3. ดึงประวัติการซื้อ (พร้อมรายละเอียดสินค้าในบิล)
            var orders = _db.Orders
                            .Include(o => o.Orderdetails)
                                .ThenInclude(od => od.Product) // ดึงชื่อสินค้ามาด้วย
                            .Where(o => o.CustomerId == customerId)
                            .OrderByDescending(o => o.OrderDate)
                            .ToList();

            // 4. ประกอบร่างส่งให้ View
            var model = new CustomerProfileViewModel
            {
                FullName = customer.FullName,
                Phone = customer.Phone,
                RewardPoints = customer.RewardPoints ?? 0,
                TotalWeightBought = customer.TotalWeightBought ?? 0,
                MemberSince = customer.CreatedAt,
                OrderHistory = orders
            };

            return View(model);
        }
    }
}