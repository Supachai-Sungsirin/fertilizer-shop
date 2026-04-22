using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FertilizerShop.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using FertilizerShop.ViewModels;
using System.Security.Claims;
using System.Text.Json;
using ClosedXML.Excel;
using System.IO;

namespace FertilizerShop.Controllers
{
    [Authorize(Roles = "Owner,Manager")]
    public class OwnerController : Controller
    {
        private readonly FertilizershopdbContext _db;
        private readonly IWebHostEnvironment _env;

        public OwnerController(FertilizershopdbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // Dashboard ภาพรวม
        public IActionResult Dashboard()
        {
            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            // คำนวณยอดขายวันนี้ และ เดือนนี้
            var todayOrders = _db.Orders.Where(o => o.OrderDate.Value.Date == today).ToList();
            var monthOrders = _db.Orders.Where(o => o.OrderDate.Value.Date >= startOfMonth).ToList();

            // หาสินค้าใกล้หมดสต็อก
            var lowStock = _db.Products.Count(p => p.StockQuantity <= 10);

            // หาสินค้าขายดี 5 อันดับแรก
            var topProducts = _db.Orderdetails
                                 .Include(od => od.Product)
                                 .AsEnumerable()
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

            // เตรียมข้อมูลทำ "กราฟยอดขายย้อนหลัง 7 วัน"
            var last7Days = today.AddDays(-6);
            var recentOrders = _db.Orders.Where(o => o.OrderDate.Value.Date >= last7Days).ToList();

            // สร้างลิสต์วันที่ 7 วันเพื่อป้องกันวันไหนไม่มีบิล กราฟจะได้ไม่แหว่ง
            var chartLabels = new List<string>();
            var chartValues = new List<decimal>();

            for (int i = 6; i >= 0; i--)
            {
                var targetDate = today.AddDays(-i);
                chartLabels.Add(targetDate.ToString("dd/MM")); 
                chartValues.Add(recentOrders.Where(o => o.OrderDate.Value.Date == targetDate).Sum(o => o.TotalAmount));
            }

            // ส่งข้อมูลกราฟไปให้ JavaScript
            ViewBag.ChartLabels = JsonSerializer.Serialize(chartLabels);
            ViewBag.ChartValues = JsonSerializer.Serialize(chartValues);

            // ดึงสินค้าที่สต็อกเหลือน้อยที่สุด 5 อันดับแรก (น้อยกว่าหรือเท่ากับ 10)
            var topLowStock = _db.Products
                                 .Where(p => p.StockQuantity <= 10)
                                 .OrderBy(p => p.StockQuantity) 
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
                string? imageUrl = null;

                // ระบบอัปโหลดรูปภาพ
                if (data.ImageUpload != null)
                {
                    string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "products");
                    Directory.CreateDirectory(uploadsFolder); // สร้างโฟลเดอร์ถ้ายังไม่มี
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + data.ImageUpload.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        data.ImageUpload.CopyTo(fileStream);
                    }
                    imageUrl = "/images/products/" + uniqueFileName;
                }

                var newProduct = new Product
                {
                    Sku = data.Sku,
                    Name = data.Name,
                    CategoryId = data.CategoryId,
                    WeightPerUnit = data.WeightPerUnit,
                    Price = data.Price,
                    StockQuantity = data.StockQuantity,
                    ExpiryDate = data.ExpiryDate,
                    ImageUrl = imageUrl
                };

                _db.Products.Add(newProduct);
                _db.SaveChanges(); 
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

                if (data.ImageUpload != null)
                {
                    string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "products"); 
                    Directory.CreateDirectory(uploadsFolder);
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + data.ImageUpload.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        data.ImageUpload.CopyTo(fileStream);
                    }
                    product.ImageUrl = "/images/products/" + uniqueFileName; 
                }

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

        // --- ส่วนของศูนย์รวมรายงาน (Reports) ---
        [HttpGet]
        public IActionResult Reports()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ExportIncomeExpense(DateTime startDate, DateTime endDate)
        {
            // ปรับเวลาให้ครอบคลุมทั้งวัน (ตั้งแต่ 00:00:00 ถึง 23:59:59)
            var endOfDay = endDate.Date.AddDays(1).AddTicks(-1);

            // 1. ดึงข้อมูลรายรับ (จากการขาย)
            var incomeOrders = _db.Orders
                .Where(o => o.OrderDate >= startDate.Date && o.OrderDate <= endOfDay)
                .OrderBy(o => o.OrderDate)
                .ToList();

            // 2. ดึงข้อมูลรายจ่าย (จากการสั่งซื้อ PO ที่ "รับของแล้ว" เท่านั้น)
            var expensePOs = _db.Purchaseorders
                .Where(p => p.OrderDate >= startDate.Date && p.OrderDate <= endOfDay && p.Status == "Received")
                .OrderBy(p => p.OrderDate)
                .ToList();

            // เริ่มสร้างไฟล์ Excel
            using (var workbook = new XLWorkbook())
            {
                // ชีตที่ 1: สรุปภาพรวม (Summary)
                var wsSummary = workbook.Worksheets.Add("สรุปภาพรวม");
                wsSummary.Cell(1, 1).Value = $"รายงานสรุปรายรับ-รายจ่าย ตั้งแต่วันที่ {startDate:dd/MM/yyyy} ถึง {endDate:dd/MM/yyyy}";
                wsSummary.Range("A1:C1").Merge().Style.Font.Bold = true;

                decimal totalIncome = incomeOrders.Sum(o => o.NetAmount);
                decimal totalExpense = expensePOs.Sum(p => p.TotalAmount);
                decimal profit = totalIncome - totalExpense;

                wsSummary.Cell(3, 1).Value = "รวมรายรับจากการขาย:";
                wsSummary.Cell(3, 2).Value = totalIncome;
                wsSummary.Cell(4, 1).Value = "รวมรายจ่ายจากการสั่งซื้อ (ต้นทุน):";
                wsSummary.Cell(4, 2).Value = totalExpense;
                wsSummary.Cell(5, 1).Value = "กำไร / ขาดทุน เบื้องต้น:";
                wsSummary.Cell(5, 2).Value = profit;

                // ใส่สีให้กำไร/ขาดทุน
                wsSummary.Cell(5, 2).Style.Font.FontColor = profit >= 0 ? XLColor.Green : XLColor.Red;
                wsSummary.Columns().AdjustToContents();

                // ชีตที่ 2: รายละเอียดรายรับ (Income)
                var wsIncome = workbook.Worksheets.Add("รายละเอียดรายรับ");
                wsIncome.Cell(1, 1).Value = "วันที่";
                wsIncome.Cell(1, 2).Value = "เลขที่ใบเสร็จ";
                wsIncome.Cell(1, 3).Value = "วิธีชำระเงิน";
                wsIncome.Cell(1, 4).Value = "ยอดสุทธิ (บาท)";
                wsIncome.Range("A1:D1").Style.Font.Bold = true;
                wsIncome.Range("A1:D1").Style.Fill.BackgroundColor = XLColor.LightGreen;

                int row = 2;
                foreach (var item in incomeOrders)
                {
                    wsIncome.Cell(row, 1).Value = item.OrderDate?.ToString("dd/MM/yyyy HH:mm");
                    wsIncome.Cell(row, 2).Value = item.ReceiptNo;
                    wsIncome.Cell(row, 3).Value = item.PaymentMethod;
                    wsIncome.Cell(row, 4).Value = item.NetAmount;
                    row++;
                }
                wsIncome.Columns().AdjustToContents();


                // ชีตที่ 3: รายละเอียดรายจ่าย (Expense)
                var wsExpense = workbook.Worksheets.Add("รายละเอียดรายจ่าย");
                wsExpense.Cell(1, 1).Value = "วันที่สั่งซื้อ";
                wsExpense.Cell(1, 2).Value = "เลขที่ PO";
                wsExpense.Cell(1, 3).Value = "ยอดรวมต้นทุน (บาท)";
                wsExpense.Range("A1:C1").Style.Font.Bold = true;
                wsExpense.Range("A1:C1").Style.Fill.BackgroundColor = XLColor.LightCoral;

                int rowExp = 2;
                foreach (var item in expensePOs)
                {
                    wsExpense.Cell(rowExp, 1).Value = item.OrderDate?.ToString("dd/MM/yyyy HH:mm");
                    wsExpense.Cell(rowExp, 2).Value = "PO-" + item.PoId.ToString("D4");
                    wsExpense.Cell(rowExp, 3).Value = item.TotalAmount;
                    rowExp++;
                }
                wsExpense.Columns().AdjustToContents();

                // แปลงไฟล์ Excel เป็น Stream เพื่อให้เบราว์เซอร์ดาวน์โหลด
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    string fileName = $"IncomeExpenseReport_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                    // ส่งไฟล์กลับไปให้ผู้ใช้ดาวน์โหลด
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }
        [HttpPost]
        public IActionResult ExportInventory()
        {
            // ดึงข้อมูลสินค้าทั้งหมด เรียงตามชื่อสินค้า
            var products = _db.Products.OrderBy(p => p.Name).ToList();

            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("รายงานสต็อกสินค้า");

                // 1. สร้างหัวตาราง
                ws.Cell(1, 1).Value = "รหัสสินค้า (SKU)";
                ws.Cell(1, 2).Value = "ชื่อสินค้า";
                ws.Cell(1, 3).Value = "จำนวนคงเหลือ (ชิ้น/กระสอบ)";
                ws.Cell(1, 4).Value = "สถานะสต็อก";

                ws.Range("A1:D1").Style.Font.Bold = true;
                ws.Range("A1:D1").Style.Fill.BackgroundColor = XLColor.LightSkyBlue;

                // 2. วนลูปใส่ข้อมูล
                int row = 2;
                foreach (var p in products)
                {
                    ws.Cell(row, 1).Value = p.Sku;
                    ws.Cell(row, 2).Value = p.Name;
                    ws.Cell(row, 3).Value = p.StockQuantity;

                    // เช็คสถานะพร้อมใส่สีตัวอักษรใน Excel อัตโนมัติ!
                    if (p.StockQuantity == 0)
                    {
                        ws.Cell(row, 4).Value = "หมดสต็อก (วิกฤต)";
                        ws.Cell(row, 4).Style.Font.FontColor = XLColor.Red;
                        ws.Cell(row, 4).Style.Font.Bold = true;
                    }
                    else if (p.StockQuantity <= 10)
                    {
                        ws.Cell(row, 4).Value = "ใกล้หมด (ต้องสั่งเพิ่ม)";
                        ws.Cell(row, 4).Style.Font.FontColor = XLColor.DarkOrange;
                    }
                    else
                    {
                        ws.Cell(row, 4).Value = "ปกติ";
                        ws.Cell(row, 4).Style.Font.FontColor = XLColor.Green;
                    }

                    row++;
                }

                // จัดความกว้างคอลัมน์ให้อ่านง่าย
                ws.Columns().AdjustToContents();

                // 3. แปลงไฟล์และส่งให้ดาวน์โหลด
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    string fileName = $"InventoryReport_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
                    
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }
    }
}