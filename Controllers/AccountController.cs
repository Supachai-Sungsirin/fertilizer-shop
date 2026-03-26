using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using FertilizerShop.Models;
using FertilizerShop.ViewModels;

namespace FertilizerShop.Controllers
{
    public class AccountController : Controller
    {
        private readonly FertilizershopdbContext _db;


        public AccountController(FertilizershopdbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel data)
        {
            if (ModelState.IsValid)
            {
                // 1. ค้นหาพนักงานจาก EmployeeId ใน ViewModel
                var user = (from u in _db.Users
                            where u.EmployeeId == data.EmployeeId && u.IsActive == true
                            select u).FirstOrDefault();

                // 2. เช็คว่าเจอพนักงานไหม และรหัสผ่านตรงไหม
                if (user != null && user.PasswordHash == data.Password)
                {
                    var role = (from r in _db.Roles
                                where r.RoleId == user.RoleId
                                select r).FirstOrDefault();

                    var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FirstName + " " + user.LastName),
                new Claim("EmployeeId", user.EmployeeId),
                new Claim(ClaimTypes.Role, role.RoleName)
            };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    // เพิ่มการตั้งค่า Remember Me (ถ้าหน้าเว็บติ๊กถูกมา จะจำล็อกอินไว้แม้ปิดเบราว์เซอร์)
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = data.RememberMe
                    };

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

                    if (role.RoleName == "Owner") return RedirectToAction("Dashboard", "Owner");
                    else if (role.RoleName == "Manager") return RedirectToAction("Index", "Manager");
                    else if (role.RoleName == "Cashier") return RedirectToAction("Index", "Cashier");

                    return RedirectToAction("Index", "Home");
                }
                ViewBag.Error = "รหัสพนักงาน หรือ รหัสผ่าน ไม่ถูกต้อง";
            }

            // ถ้าลืมกรอกข้อมูล หรือรหัสผิด ให้ส่ง ViewModel กลับไปแสดง Error ที่หน้าเดิม
            return View(data);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        public IActionResult Register()
        {
            return View();
        }
    }
}