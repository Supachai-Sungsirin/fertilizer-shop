using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using FertilizerShop.Models;
using FertilizerShop.ViewModels;
using Claim = System.Security.Claims.Claim;

namespace FertilizerShop.Controllers
{
    public class CustomerAuthController : Controller
    {
        private readonly FertilizershopdbContext _db;

        public CustomerAuthController(FertilizershopdbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity.IsAuthenticated && User.IsInRole("Customer")) return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        public IActionResult Register(CustomerRegisterViewModel data)
        {
            if (ModelState.IsValid)
            {
                // เช็คว่าเบอร์นี้เคยสมัครหรือยัง
                var existingCustomer = _db.Customers.FirstOrDefault(c => c.Phone == data.Phone);
                
                if (existingCustomer != null)
                {
                    // ถ้ามีเบอร์นี้ในระบบแล้ว (เช่น แคชเชียร์เคยคีย์ให้) แต่ยังไม่มีรหัสผ่าน
                    if (string.IsNullOrEmpty(existingCustomer.PasswordHash))
                    {
                        existingCustomer.FullName = data.FullName;
                        existingCustomer.PasswordHash = BCrypt.Net.BCrypt.HashPassword(data.Password);
                        _db.SaveChanges();
                        TempData["Success"] = "ตั้งรหัสผ่านและเปิดใช้งานบัญชีสำเร็จ! กรุณาเข้าสู่ระบบ";
                        return RedirectToAction("Login");
                    }
                    else
                    {
                        ModelState.AddModelError("Phone", "เบอร์โทรศัพท์นี้ถูกใช้งานแล้ว");
                        return View(data);
                    }
                }

                // สร้างลูกค้าใหม่
                var newCustomer = new Customer
                {
                    Phone = data.Phone,
                    FullName = data.FullName,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(data.Password),
                    RewardPoints = 0,
                    TotalWeightBought = 0,
                    CreatedAt = DateTime.Now
                };

                _db.Customers.Add(newCustomer);
                _db.SaveChanges();

                TempData["Success"] = "สมัครสมาชิกสำเร็จ! กรุณาเข้าสู่ระบบ";
                return RedirectToAction("Login");
            }
            return View(data);
        }

        // --- หน้า เข้าสู่ระบบ ---
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated && User.IsInRole("Customer")) return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(CustomerLoginViewModel data)
        {
            if (ModelState.IsValid)
            {
                var customer = _db.Customers.FirstOrDefault(c => c.Phone == data.Phone);

                if (customer != null && !string.IsNullOrEmpty(customer.PasswordHash))
                {
                    bool isPasswordValid = BCrypt.Net.BCrypt.Verify(data.Password, customer.PasswordHash);
                    if (isPasswordValid)
                    {
                        // สร้างบัตรประจำตัว (Claims) ให้ลูกค้า
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.NameIdentifier, customer.CustomerId.ToString()),
                            new Claim(ClaimTypes.Name, customer.FullName),
                            new Claim(ClaimTypes.Role, "Customer") // กำหนด Role ให้รู้ว่าเป็นลูกค้า
                        };

                        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var principal = new ClaimsPrincipal(identity);

                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                        return RedirectToAction("Index", "Home");
                    }
                }
                ModelState.AddModelError("", "เบอร์โทรศัพท์หรือรหัสผ่านไม่ถูกต้อง");
            }
            return View(data);
        }

        // --- ออกจากระบบ ---
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}