using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FertilizerShop.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using FertilizerShop.ViewModels;
using System.Security.Claims;
using System.Text.Json;

namespace FertilizerShop.Controllers
{
    [Authorize(Roles = "Owner,Manager")]
    public class OwnerController : Controller
    {
        private readonly FertilizershopdbContext _db;

        public OwnerController(FertilizershopdbContext db)
        {
            _db = db;
        }

        // Dashboard ภาพรวม
        public IActionResult Dashboard()
        {
            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            // 1. คำนวณยอดขายวันนี้ และ เดือนนี้
            var todayOrders = _db.Orders.Where(o => o.OrderDate.Value.Date == today).ToList();
            var monthOrders = _db.Orders.Where(o => o.OrderDate.Value.Date >= startOfMonth).ToList();

            // 2. หาสินค้าใกล้หมดสต็อก
            var lowStock = _db.Products.Count(p => p.StockQuantity <= 10);

            // 3. หาสินค้าขายดี 5 อันดับแรก (Top 5 Best Sellers)
            var topProducts = _db.Orderdetails
                                 .Include(od => od.Product)
                                 .AsEnumerable() // ดึงมาประมวลผลใน Memory
                                 .GroupBy(od => new { od.ProductId, od.Product?.Name })
                                 .Select(g => new TopSellingProduct
                                 {
                                     ProductName = g.Key.Name ?? "ไม่ทราบชื่อ",
                                     TotalQty = g.Sum(x => x.Quantity),
                                     TotalAmount = g.Sum(x => x.SubTotal)
                                 })
                                 .OrderByDescending(x => x.TotalQty)
                                 .Take(5)
                                 .ToList();

            // 4. เตรียมข้อมูลทำ "กราฟยอดขายย้อนหลัง 7 วัน"
            var last7Days = today.AddDays(-6);
            var recentOrders = _db.Orders.Where(o => o.OrderDate.Value.Date >= last7Days).ToList();

            // สร้างลิสต์วันที่ 7 วันเพื่อป้องกันวันไหนไม่มีบิล กราฟจะได้ไม่แหว่ง
            var chartLabels = new List<string>();
            var chartValues = new List<decimal>();

            for (int i = 6; i >= 0; i--)
            {
                var targetDate = today.AddDays(-i);
                chartLabels.Add(targetDate.ToString("dd/MM")); // ชื่อแกน X (เช่น 25/03)
                                                               // ยอดรวมของวันนั้น แกน Y
                chartValues.Add(recentOrders.Where(o => o.OrderDate.Value.Date == targetDate).Sum(o => o.TotalAmount));
            }

            // ส่งข้อมูลกราฟไปให้ JavaScript
            ViewBag.ChartLabels = JsonSerializer.Serialize(chartLabels);
            ViewBag.ChartValues = JsonSerializer.Serialize(chartValues);

            // ดึงสินค้าที่สต็อกเหลือน้อยที่สุด 5 อันดับแรก (น้อยกว่าหรือเท่ากับ 10)
            var topLowStock = _db.Products
                                 .Where(p => p.StockQuantity <= 10)
                                 .OrderBy(p => p.StockQuantity) // เรียงจากน้อยไปมาก
                                 .Take(5)
                                 .ToList();

            // ดึงพนักงานที่เปิดบิลได้เยอะที่สุด 5 อันดับแรก (นับจากจำนวนบิล)
            var topEmployees = _db.Orders
                                  .Include(o => o.Cashier)
                                  .AsEnumerable() // ดึงมาคำนวณใน Memory
                                  .GroupBy(o => new { o.CashierId, FirstName = o.Cashier?.FirstName, LastName = o.Cashier?.LastName })
                                  .Select(g => new TopEmployeeViewModel
                                  {
                                      EmployeeName = $"{g.Key.FirstName} {g.Key.LastName}".Trim(),
                                      TotalBills = g.Count(), // นับจำนวนบิล
                                      TotalSales = g.Sum(x => x.TotalAmount) // รวมยอดขาย
                                  })
                                  .OrderByDescending(x => x.TotalBills) // เรียงจากจำนวนบิลมากสุด
                                  .Take(5)
                                  .ToList();

            // แพ็คข้อมูลทั้งหมดลง ViewModel
            var model = new OwnerDashboardViewModel
            {
                TodaySales = todayOrders.Sum(o => o.TotalAmount),
                MonthSales = monthOrders.Sum(o => o.TotalAmount),
                TotalOrdersMonth = monthOrders.Count,
                LowStockCount = lowStock,
                TopProducts = topProducts,
                LowStockProductsList = topLowStock, 
                TopEmployees = topEmployees       
            };

            return View(model);
        }
        // จัดการพนักงาน
        public IActionResult Employees()
        {
            // ดึงข้อมูลพนักงานทั้งหมด พร้อมกับชื่อตำแหน่ง (Role)
            var users = _db.Users.Include(u => u.Role).ToList();
            return View(users);
        }
        [HttpGet]
        public IActionResult AddEmployee()
        {
            ViewBag.RoleList = new SelectList(_db.Roles, "RoleId", "RoleName");
            return View();
        }

        [HttpPost]
        public IActionResult AddEmployee(EmployeeViewModel data)
        {
            if (ModelState.IsValid)
            {
                // ทำการ Mapping ข้อมูลจาก ViewModel โยนใส่ Model หลัก (User)
                var newUser = new User
                {
                    EmployeeId = data.EmployeeId,
                    PasswordHash = data.PasswordHash,
                    FirstName = data.FirstName,
                    LastName = data.LastName,
                    RoleId = data.RoleId,
                    IsActive = true
                };

                _db.Users.Add(newUser);
                _db.SaveChanges();

                return RedirectToAction("Employees");
            }

            ViewBag.RoleList = new SelectList(_db.Roles, "RoleId", "RoleName");
            return View(data);
        }

        [HttpGet]
        public IActionResult EditEmployee(int id)
        {
            var user = _db.Users.Find(id);
            if (user == null) return NotFound();

            var model = new EditEmployeeViewModel
            {
                UserId = user.UserId,
                EmployeeId = user.EmployeeId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                RoleId = user.RoleId,
                IsActive = user.IsActive ?? false
            };

            ViewBag.RoleList = new SelectList(_db.Roles, "RoleId", "RoleName", user.RoleId);
            return View(model);
        }

        [HttpPost]
        public IActionResult EditEmployee(EditEmployeeViewModel data)
        {
            if (ModelState.IsValid)
            {
                // ดึง ID ของคนที่ล็อกอินอยู่ปัจจุบัน
                var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                if (data.UserId == currentUserId)
                {
                    data.IsActive = true;
                }

                var user = _db.Users.Find(data.UserId);
                if (user == null) return NotFound();

                // (โค้ดอัปเดตข้อมูล user... เหมือนเดิม)
                user.EmployeeId = data.EmployeeId;
                user.FirstName = data.FirstName;
                user.LastName = data.LastName;
                user.RoleId = data.RoleId;
                user.IsActive = data.IsActive;

                if (!string.IsNullOrEmpty(data.PasswordHash))
                {
                    user.PasswordHash = data.PasswordHash;
                }

                _db.SaveChanges();
                return RedirectToAction("Employees");
            }

            ViewBag.RoleList = new SelectList(_db.Roles, "RoleId", "RoleName", data.RoleId);
            return View(data);
        }

        public IActionResult ToggleStatus(int id)
        {
            // ดึง ID ของคนที่ล็อกอินอยู่ปัจจุบัน
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            if (id == currentUserId)
            {
                TempData["ErrorMessage"] = "คุณไม่สามารถระงับการใช้งานบัญชีของตัวเองได้!";
                return RedirectToAction("Employees");
            }

            var user = _db.Users.Find(id);
            if (user != null)
            {
                user.IsActive = !user.IsActive;
                _db.SaveChanges();
            }
            return RedirectToAction("Employees");
        }

        // --- จัดการหมวดหมู่สินค้า ---
        public IActionResult Categories()
        {
            // ดึงข้อมูลหมวดหมู่ทั้งหมด
            var categories = _db.Categories.ToList();
            return View(categories);
        }

        [HttpGet]
        public IActionResult AddCategory()
        {
            return View();
        }

        [HttpPost]
        public IActionResult AddCategory(CategoryViewModel data)
        {
            // ตรวจสอบความถูกต้องของข้อมูลที่ส่งมาจากฟอร์ม
            if (ModelState.IsValid)
            {
                var newCategory = new Category
                {
                    CategoryName = data.CategoryName
                };
                _db.Categories.Add(newCategory);
                _db.SaveChanges();
                return RedirectToAction("Categories");
            }
            return View(data);
        }

        [HttpGet]
        public IActionResult EditCategory(int id)
        {
            // ดึงข้อมูลหมวดหมู่ที่ต้องการแก้ไขมาแสดงในฟอร์ม
            var category = _db.Categories.Find(id);
            if (category == null) return NotFound();

            var model = new CategoryViewModel
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName
            };
            return View(model);
        }

        [HttpPost]
        public IActionResult EditCategory(CategoryViewModel data)
        {
            // ตรวจสอบความถูกต้องของข้อมูลที่ส่งมาจากฟอร์ม
            if (ModelState.IsValid)
            {
                var category = _db.Categories.Find(data.CategoryId);
                if (category == null) return NotFound();

                category.CategoryName = data.CategoryName;
                _db.SaveChanges();
                return RedirectToAction("Categories");
            }
            return View(data);
        }

        public IActionResult DeleteCategory(int id)
        {
            // ดึงข้อมูลหมวดหมู่ที่ต้องการลบ
            var category = _db.Categories.Find(id);
            if (category != null)
            {
                // ระบบป้องกัน: เช็คว่ามี "สินค้า" ไหนใช้หมวดหมู่นี้อยู่ไหม ถ้ามีห้ามลบ!
                bool hasProducts = _db.Products.Any(p => p.CategoryId == id);
                if (hasProducts)
                {
                    TempData["ErrorMessage"] = $"ไม่สามารถลบหมวดหมู่ '{category.CategoryName}' ได้ เพราะมีสินค้าใช้งานหมวดหมู่นี้อยู่";
                    return RedirectToAction("Categories");
                }

                _db.Categories.Remove(category);
                _db.SaveChanges();
            }
            return RedirectToAction("Categories");
        }

        // --- ส่วนการจัดการสินค้า ---
        public IActionResult Products()
        {
            // Include Category เข้ามาด้วย เพื่อให้ตารางโชว์ "ชื่อหมวดหมู่" ได้ (แทนที่จะโชว์แค่รหัส CategoryId)
            var products = _db.Products.Include(p => p.Category).ToList();
            return View(products);
        }

        [HttpGet]
        public IActionResult AddProduct()
        {
            // ดึงหมวดหมู่ทั้งหมดมาทำ Dropdown ให้เลือกตอนเพิ่มสินค้า
            ViewBag.CategoryList = new SelectList(_db.Categories, "CategoryId", "CategoryName");
            return View();
        }

        [HttpPost]
        public IActionResult AddProduct(ProductViewModel data)
        {
            if (ModelState.IsValid)
            {
                // โยนข้อมูลจาก ViewModel เข้า Model จริงของตาราง
                var newProduct = new Product
                {
                    Sku = data.Sku,
                    Name = data.Name,
                    CategoryId = data.CategoryId,
                    WeightPerUnit = data.WeightPerUnit,
                    Price = data.Price,
                    StockQuantity = data.StockQuantity,
                    ExpiryDate = data.ExpiryDate
                };

                _db.Products.Add(newProduct);
                _db.SaveChanges(); // คราวนี้ Save ผ่านแน่นอนเพราะข้อมูลครบ
                return RedirectToAction("Products");
            }

            ViewBag.CategoryList = new SelectList(_db.Categories, "CategoryId", "CategoryName", data.CategoryId);
            return View(data);
        }

        [HttpGet]
        public IActionResult EditProduct(int id)
        {
            var product = _db.Products.Find(id);
            if (product == null) return NotFound();

            var model = new ProductViewModel
            {
                ProductId = product.ProductId,
                Sku = product.Sku,
                Name = product.Name,
                CategoryId = product.CategoryId,
                WeightPerUnit = product.WeightPerUnit,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                ExpiryDate = product.ExpiryDate
            };

            ViewBag.CategoryList = new SelectList(_db.Categories, "CategoryId", "CategoryName", product.CategoryId);
            return View(model);
        }

        [HttpPost]
        public IActionResult EditProduct(ProductViewModel data)
        {
            if (ModelState.IsValid)
            {
                var product = _db.Products.Find(data.ProductId);
                if (product == null) return NotFound();

                product.Sku = data.Sku;
                product.Name = data.Name;
                product.CategoryId = data.CategoryId;
                product.WeightPerUnit = data.WeightPerUnit;
                product.Price = data.Price;
                product.StockQuantity = data.StockQuantity;
                product.ExpiryDate = data.ExpiryDate;

                _db.SaveChanges();
                return RedirectToAction("Products");
            }

            ViewBag.CategoryList = new SelectList(_db.Categories, "CategoryId", "CategoryName", data.CategoryId);
            return View(data);
        }

        public IActionResult DeleteProduct(int id)
        {
            var product = _db.Products.Find(id);
            if (product != null)
            {
                // ป้องกัน: เช็คว่าสินค้านี้เคยถูกขายไปหรือยัง (อยู่ใน OrderDetail) ถ้ามีห้ามลบเด็ดขาด
                bool isSold = _db.Orderdetails.Any(od => od.ProductId == id);
                if (isSold)
                {
                    TempData["ErrorMessage"] = $"ไม่สามารถลบ '{product.Name}' ได้ เพราะเคยมีประวัติการขายแล้ว แนะนำให้ปรับสต็อกเป็น 0 แทน";
                    return RedirectToAction("Products");
                }

                _db.Products.Remove(product);
                _db.SaveChanges();
            }
            return RedirectToAction("Products");
        }
    }
}