using System.Diagnostics;
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

    public IActionResult Lab4()
    {
        return View();
    }

    public IActionResult Login(string username, string password, string email, string phoneNumber, string source)
    {
        var model = new LoginViewModel
        {
            Username = username,
            Password = password
        };

        ViewBag.Email = email;
        ViewBag.PhoneNumber = phoneNumber;
        ViewBag.Source = source;

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Login(LoginViewModel data, string email, string phoneNumber, string source)
    {
        var checkUser = _db.Users
            .Include(u => u.Employee)
            .FirstOrDefault(u => (u.UserName == data.Username || u.Email == data.Username)
                                 && u.Password == data.Password);

        if (checkUser != null)
        {
            checkUser.LastActive = DateTime.Now;
            _db.SaveChanges();

            HttpContext.Session.SetInt32("UserId", checkUser.Id);
            HttpContext.Session.SetString("Username", checkUser.UserName);
            HttpContext.Session.SetString("Email", checkUser.Email);
            HttpContext.Session.SetString("Role", checkUser.Role ?? "customer");

            string dept = checkUser.Employee?.Department ?? (checkUser.Role == "admin" ? "Dev" : "");
            HttpContext.Session.SetString("Department", dept);

            if (checkUser.Role == "admin")
            {
                return RedirectToAction("Dashboard", "Dev");
            }
            else if (checkUser.Role == "employee")
            {
                if (dept.Equals("Support", StringComparison.OrdinalIgnoreCase))
                    return RedirectToAction("Index", "Support");
                if (dept.Equals("Dev", StringComparison.OrdinalIgnoreCase))
                    return RedirectToAction("Dashboard", "Dev");
                if (dept.Equals("Marketing", StringComparison.OrdinalIgnoreCase))
                    return RedirectToAction("Dashboard", "Marketing");
                if (dept.Equals("Accounting", StringComparison.OrdinalIgnoreCase))
                    return RedirectToAction("Dashboard", "Accounting");

                return RedirectToAction("Dashboard", "Dev");
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
        var user = _db.Users.Include(u => u.Employee).FirstOrDefault(u =>
            (roleType == "Customer" && u.Role == "customer") ||
            (roleType == "Support" && u.Employee != null && u.Employee.Department == "Support") ||
            (roleType == "Dev" && u.Employee != null && u.Employee.Department == "Dev") ||
            (roleType == "Marketing" && u.Employee != null && u.Employee.Department == "Marketing") ||
            (roleType == "Accounting" && u.Employee != null && u.Employee.Department == "Accounting") ||
            (roleType == "Admin" && u.Role == "admin")
        );

        string mappedDept = roleType == "Admin" ? "Dev" : roleType;

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
            HttpContext.Session.SetString("Role", roleType == "Customer" ? "customer" : "employee");
            HttpContext.Session.SetString("Department", mappedDept);
        }

        return roleType switch
        {
            "Support" => RedirectToAction("Index", "Support"),
            "Dev" => RedirectToAction("Dashboard", "Dev"),
            "Marketing" => RedirectToAction("Dashboard", "Marketing"),
            "Accounting" => RedirectToAction("Dashboard", "Accounting"),
            "Admin" => RedirectToAction("UserList", "Dev"),
            _ => RedirectToAction("Home", "Customer")
        };
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
            if (data.Password != null && data.Password == data.ConfirmPassword)
            {
                var checkUser = (from u in _db.Users
                                 where u.Email == data.Email || u.UserName == data.Username
                                 select u).FirstOrDefault();

                if (checkUser == null)
                {
                    User u = new();
                    u.Email = data.Email;
                    u.UserName = data.Username;
                    u.Password = data.Password;
                    u.PhoneNumber = data.PhoneNumber;
                    u.Role = "customer";
                    u.Status = "active";

                    _db.Add(u);
                    _db.SaveChanges();

                    return RedirectToAction("Login", "Account", new
                    {
                        source = "Register",
                        username = data.Username,
                        password = data.Password,
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

    public IActionResult Index(string source, string username, bool rememberMe, string email, string phoneNumber)
    {
        return RedirectToAction("Dashboard", "Dev");
    }

    // ส่งต่อ URL เดิม (/Account/UserList, /Account/AddUser, /Account/EditUser) ไปให้ฝ่าย Dev จัดการทั้งหมด
    public IActionResult UserList(string? search) => RedirectToAction("UserList", "Dev", new { search });
    public IActionResult AddUser() => RedirectToAction("AddUser", "Dev");
    public IActionResult EditUser(int id) => RedirectToAction("EditUser", "Dev", new { id });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteUser(int id) => RedirectToAction("DeleteUser", "Dev", new { id });

    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}