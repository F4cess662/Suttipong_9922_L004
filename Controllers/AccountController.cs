using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Suttipong_9922_L004.Models;
using Suttipong_9922_L004.ViewModels;

namespace Suttipong_9922_L004.Controllers;

public class AccountController : Controller
{
    private readonly Csi402dbContext _db;

    public AccountController(Csi402dbContext db)
    {
        _db = db;
    }

    // ฟังก์ชันเข้ารหัสรหัสผ่านด้วย SHA-256 (ผลลัพธ์ตรงกับคำสั่ง SHA2(..., 256) ใน MySQL)
    private string HashPassword(string password)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLower();
    }

    public IActionResult Lab4()
    {
        return View();
    }

    public IActionResult Login(string? username, string? password, string? email, string? phoneNumber, string? source)
    {
        var model = new LoginViewModel
        {
            Username = username ?? "",
            Password = password ?? ""
        };

        ViewBag.Email = email;
        ViewBag.PhoneNumber = phoneNumber;
        ViewBag.Source = source;

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Login(LoginViewModel data, string? email, string? phoneNumber, string? source)
    {
        string hashedPassword = HashPassword(data.Password ?? "");

        // ค้นหาผู้ใช้จากฐานข้อมูลโดยเทียบกับรหัสผ่านที่ Hash ด้วย SHA-256
        var checkUser = (from u in _db.Users.Include(x => x.Employee)
                         where (u.UserName == data.Username || u.Email == data.Username)
                               && (u.Password == hashedPassword || u.Password == data.Password)
                         select u).FirstOrDefault();

        if (checkUser != null)
        {
            if (checkUser.Status != "active")
            {
                ViewBag.Status = "บัญชีนี้ถูกระงับการใช้งานหรือรอการอนุมัติ";
                return View(data);
            }

            // หากบัญชีเก่ายังเก็บรหัสผ่านแบบไม่ Hash ให้แปลงเป็น SHA-256 บันทึกลง DB อัตโนมัติ
            if (checkUser.Password != hashedPassword)
            {
                checkUser.Password = hashedPassword;
            }

            checkUser.LastActive = DateTime.Now;
            _db.SaveChanges();

            HttpContext.Session.SetInt32("UserId", checkUser.Id);
            HttpContext.Session.SetString("Username", checkUser.UserName);
            HttpContext.Session.SetString("Email", checkUser.Email);
            HttpContext.Session.SetString("Role", checkUser.Role ?? "customer");

            string dept = "";
            if (checkUser.Employee != null && !string.IsNullOrEmpty(checkUser.Employee.Department))
            {
                dept = checkUser.Employee.Department;
            }
            else if (checkUser.Role == "admin")
            {
                dept = "Dev";
            }
            HttpContext.Session.SetString("Department", dept);

            if (checkUser.Role == "admin")
            {
                return RedirectToAction("Dashboard", "Dev");
            }
            else if (checkUser.Role == "employee")
            {
                if (dept == "Support") return RedirectToAction("Index", "Support");
                else if (dept == "Dev") return RedirectToAction("Dashboard", "Dev");
                else if (dept == "Marketing") return RedirectToAction("Dashboard", "Marketing");
                else if (dept == "Accounting") return RedirectToAction("Dashboard", "Accounting");
                else return RedirectToAction("Dashboard", "Dev");
            }
            else
            {
                return RedirectToAction("Home", "Customer");
            }
        }
        else
        {
            ViewBag.Status = "ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง";
            return View(data);
        }
    }

    public IActionResult QuickLogin(string roleType)
    {
        User? user = null;
        try
        {
            if (roleType == "Customer")
                user = (from u in _db.Users where u.Role == "customer" && u.Status == "active" select u).FirstOrDefault();
            else if (roleType == "Admin")
                user = (from u in _db.Users where u.Role == "admin" && u.Status == "active" select u).FirstOrDefault();
            else
                user = (from u in _db.Users.Include(x => x.Employee)
                        where u.Employee != null && u.Employee.Department == roleType && u.Status == "active"
                        select u).FirstOrDefault();
        }
        catch { }

        string mappedDept = (roleType == "Admin") ? "Dev" : roleType;

        if (user != null)
        {
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("Username", user.UserName);
            HttpContext.Session.SetString("Email", user.Email);
            HttpContext.Session.SetString("Role", user.Role ?? "customer");
            HttpContext.Session.SetString("Department", user.Employee?.Department ?? mappedDept);
        }
        else
        {
            HttpContext.Session.SetInt32("UserId", 1);
            HttpContext.Session.SetString("Username", roleType.ToLower());
            HttpContext.Session.SetString("Email", roleType.ToLower() + "@tanglat.com");
            HttpContext.Session.SetString("Role", roleType == "Customer" ? "customer" : (roleType == "Admin" ? "admin" : "employee"));
            HttpContext.Session.SetString("Department", mappedDept);
        }

        if (roleType == "Support") return RedirectToAction("Index", "Support");
        if (roleType == "Dev") return RedirectToAction("Dashboard", "Dev");
        if (roleType == "Marketing") return RedirectToAction("Dashboard", "Marketing");
        if (roleType == "Accounting") return RedirectToAction("Dashboard", "Accounting");
        if (roleType == "Admin") return RedirectToAction("UserList", "Dev");
        return RedirectToAction("Home", "Customer");
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login", "Account");
    }

    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Register(RegisterViewModel data)
    {
        if (ModelState.IsValid)
        {
            if (!string.IsNullOrEmpty(data.Password) && data.Password == data.ConfirmPassword)
            {
                var checkUser = (from u in _db.Users
                                 where u.Email == data.Email || u.UserName == data.Username
                                 select u).FirstOrDefault();

                if (checkUser == null)
                {
                    User u = new();
                    u.Email = data.Email;
                    u.UserName = data.Username;
                    u.Password = HashPassword(data.Password); // เข้ารหัสผ่านด้วย SHA-256 ก่อนบันทึก
                    u.PhoneNumber = data.PhoneNumber;
                    u.Role = "customer";
                    u.Status = "active";

                    _db.Add(u);
                    _db.SaveChanges();

                    return RedirectToAction("Login", "Account", new
                    {
                        source = "Register",
                        username = data.Username,
                        email = data.Email,
                        phoneNumber = data.PhoneNumber
                    });
                }
                else
                {
                    ViewBag.Status = "อีเมลหรือชื่อผู้ใช้นี้มีในระบบแล้ว";
                    return View(data);
                }
            }
            else
            {
                ViewBag.Status = "รหัสผ่านไม่ตรงกัน หรือยังไม่ได้กรอกรหัสผ่าน";
                return View(data);
            }
        }
        else
        {
            return View(data);
        }
    }

    public IActionResult Index()
    {
        return RedirectToAction("Login", "Account");
    }

    public IActionResult UserList(string? search) => RedirectToAction("UserList", "Dev", new { search = search });
    public IActionResult AddUser() => RedirectToAction("AddUser", "Dev");
    public IActionResult EditUser(int id) => RedirectToAction("EditUser", "Dev", new { id = id });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteUser(int id) => RedirectToAction("DeleteUser", "Dev", new { id = id });

    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}