using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FertilizerShop.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using FertilizerShop.ViewModels;
using System.Security.Claims;

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
            return View();
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