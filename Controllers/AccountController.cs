using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Suttipong_9922_L004.Models;
using Suttipong_9922_L004.ViewModels;

namespace Suttipong_9922_L004.Controllers;

public class AccountController : Controller
{
    // เชื่อมต่อ Database เหมือนใน HomeController
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
        // R (Read) : ดึงข้อมูลจากตาราง Users มาเช็คว่า Username (หรือ Email) และ Password ตรงกันไหม
        var checkUser = (from u in _db.Users
                         where (u.UserName == data.Username || u.Email == data.Username)
                               && u.Password == data.Password
                         select u).FirstOrDefault();

        if (checkUser != null)
        {
            return RedirectToAction("Index", "Account", new
            {
                source = source ?? "Login",
                username = checkUser.UserName,
                rememberMe = data.RememberMe,
                email = checkUser.Email,
                phoneNumber = phoneNumber
            });
        }
        else
        {
            ViewBag.Status = "ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง!!";
            return View(data);
        }
    }

    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Register(RegisterViewModel data)
    {
        // C (Create) : เพิ่มข้อมูลผู้ใช้ใหม่ลงตาราง Users
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
                    ViewBag.Status = "อีเมลหรือชื่อผู้ใช้นี้มีในระบบแล้ว!!";
                    return View(data);
                }
            }
            else
            {
                ViewBag.Status = "รหัสผ่านไม่ตรงกัน หรือยังไม่ได้กรอกรหัสผ่าน!!";
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
        ViewBag.Source = source;
        ViewBag.Username = username;
        ViewBag.RememberMe = rememberMe;
        ViewBag.Email = email;
        ViewBag.PhoneNumber = phoneNumber;

        return View();
    }

    // หน้าแสดงฟอร์มเพิ่มผู้ใช้งาน (GET)
    public IActionResult AddUser()
    {
        var model = new AddUserViewModel();
        return View(model);
    }

    // รับค่าจากฟอร์มมาบันทึกลง Database (POST) - C (Create)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddUser(AddUserViewModel data)
    {
        if (ModelState.IsValid)
        {
            // 1. ตรวจสอบว่ากรอกข้อมูลสำคัญ (Username, Email, Password) มาครบไหม
            if (!string.IsNullOrEmpty(data.Username) && !string.IsNullOrEmpty(data.Email) && !string.IsNullOrEmpty(data.Password))
            {
                // 2. เช็คว่า Email หรือ Username ซ้ำกับใน Database ไหม (เหมือน CheckID ใน Lab9)
                var checkUser = (from u in _db.Users
                                 where u.Email == data.Email || u.UserName == data.Username
                                 select u).FirstOrDefault();

                if (checkUser == null)
                {
                    // 3. สร้าง Object User แล้วนำค่าจาก AddUserViewModel ใส่เข้าไป
                    User u = new();
                    u.UserName = data.Username;
                    u.Email = data.Email;
                    u.PhoneNumber = data.PhoneNumber; // บันทึกเบอร์โทรลงคอลัมน์ phone_number ที่เพิ่งเพิ่ม
                    u.Password = data.Password;

                    // เช็คสิทธิ์จาก Radio Button (ถ้าเลือก Admin ให้เป็น "admin" นอกนั้นเป็น "customer")
                    u.Role = (data.Role == "Admin" || data.Role == "admin") ? "admin" : "customer";

                    // เช็คสวิตช์เปิดใช้งานบัญชี (ถ้าติ๊กเปิดให้เป็น "active" ถ้าปิดให้เป็น "pending")
                    u.Status = data.IsActive ? "active" : "pending";

                    _db.Add(u);
                    _db.SaveChanges();

                    // บันทึกสำเร็จ เด้งกลับไปหน้ารายชื่อผู้ใช้งาน (UserList)
                    return RedirectToAction("UserList", "Account");
                }
                else
                {
                    ViewBag.Status = "อีเมลหรือชื่อผู้ใช้นี้มีในระบบแล้ว!!";
                    return View(data);
                }
            }
            else
            {
                ViewBag.Status = "กรุณากรอก ชื่อผู้ใช้, อีเมล และรหัสผ่าน ให้ครบถ้วน!!";
                return View(data);
            }
        }
        else
        {
            return View(data);
        }
    }

    public IActionResult UserList(string? search)
    {
        var users = (from u in _db.Users
                     select new UserListViewModel
                     {
                         Id = u.Id,
                         FullName = u.UserName,
                         Username = u.UserName,
                         Email = u.Email,
                         PhoneNumber = u.PhoneNumber ?? "-",
                         Role = u.Role,
                         Status = u.Status
                     }).ToList();

        if (!string.IsNullOrEmpty(search))
        {
            users = users.Where(u =>
                (u.Username != null && u.Username.Contains(search)) ||
                (u.Email != null && u.Email.Contains(search))
            ).ToList();
        }

        ViewBag.Search = search;
        return View(users);
    }
  
    [HttpGet]
    public IActionResult EditUser(int id)
    {
        var user = (from u in _db.Users where u.Id == id select u).FirstOrDefault();

        if (user == null)
        {
            return RedirectToAction("UserList", "Account");
        }

        var model = new EditUserViewModel
        {
            Id = user.Id,
            Username = user.UserName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Password = user.Password,        
            ConfirmPassword = user.Password, 
            Role = user.Role,
            IsActive = (user.Status == "active")
        };

        return View(model);
    }

    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditUser(EditUserViewModel data)
    {
        if (ModelState.IsValid)
        {
           
            if (string.IsNullOrEmpty(data.Password) || data.Password != data.ConfirmPassword)
            {
                ViewBag.Status = "รหัสผ่านไม่ตรงกัน หรือยังไม่ได้กรอกรหัสผ่าน!!";
                return View(data);
            }

           
            var user = (from u in _db.Users where u.Id == data.Id select u).FirstOrDefault();

            if (user != null)
            {
                
                var checkDuplicate = (from u in _db.Users
                                      where (u.Email == data.Email || u.UserName == data.Username)
                                            && u.Id != data.Id
                                      select u).FirstOrDefault();

                if (checkDuplicate != null)
                {
                    ViewBag.Status = "อีเมลหรือชื่อผู้ใช้นี้ซ้ำกับบัญชีอื่นในระบบ!!";
                    return View(data);
                }

                
                user.UserName = data.Username !;
                user.Email = data.Email !;
                user.PhoneNumber = data.PhoneNumber;
                user.Password = data.Password; 
                user.Role = (data.Role == "admin") ? "admin" : "customer";
                user.Status = data.IsActive ? "active" : "pending";

                _db.Update(user);
                _db.SaveChanges();

                return RedirectToAction("UserList", "Account");
            }
        }

        return View(data);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteUser(int id)
    {
        
        var user = (from u in _db.Users where u.Id == id select u).FirstOrDefault();

        if (user != null)
        {
            _db.Remove(user);
            _db.SaveChanges();
        }

        return RedirectToAction("UserList", "Account");
    }

    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}