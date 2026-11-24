using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Suttipong_9922_L004.Models;
using Suttipong_9922_L004.ViewModels;

namespace Suttipong_9922_L004.Controllers;

public class DevController : Controller
{
    private readonly Csi402dbContext _db;
    private readonly IWebHostEnvironment _env;

    public DevController(Csi402dbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    // ฟังก์ชันเข้ารหัสรหัสผ่านด้วย SHA-256
    private string HashPassword(string password)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLower();
    }

    // ตรวจสอบสิทธิ์การเข้าถึง: อนุญาตเฉพาะฝ่าย Dev หรือ Admin เท่านั้น
    private bool CheckDevAccess()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var role = HttpContext.Session.GetString("Role");
        var dept = HttpContext.Session.GetString("Department");
        if (userId == null) return false;
        if (role == "admin") return true;
        if (role == "employee" && dept == "Dev") return true;
        return false;
    }

    private string? SaveUploadedFile(IFormFile? file, string subFolder)
    {
        if (file == null || file.Length == 0) return null;
        if (file.Length > 5 * 1024 * 1024) return null;

        string ext = Path.GetExtension(file.FileName).ToLower();
        if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".webp") return null;

        string uploadsRoot = Path.Combine(_env.WebRootPath, "uploads", subFolder);
        Directory.CreateDirectory(uploadsRoot);

        string uniqueName = subFolder.Substring(0, 1) + "_" + DateTime.Now.ToString("MMddHHmmss") + "_" + Guid.NewGuid().ToString("N").Substring(0, 4) + ext;
        string fullPath = Path.Combine(uploadsRoot, uniqueName);

        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            file.CopyTo(stream);
        }

        return "/uploads/" + subFolder + "/" + uniqueName;
    }

    public IActionResult Dashboard()
    {
        if (!CheckDevAccess()) return RedirectToAction("Login", "Account");

        ViewData["Department"] = "Dev";
        ViewData["ActiveMenu"] = "dashboard";

        int locationCount = 0;
        int transportCount = 0;
        int openBugs = 0;
        int totalUsers = 0;
        int attractionCount = 0;
        int restaurantCount = 0;
        int shoppingCount = 0;
        int otherCount = 0;

        try
        {
            var locations = (from l in _db.Locations select l).ToList();
            locationCount = locations.Count;
            attractionCount = locations.Count(l => l.Category == "attraction");
            restaurantCount = locations.Count(l => l.Category == "restaurant");
            shoppingCount = locations.Count(l => l.Category == "shopping");
            otherCount = locations.Count(l => l.Category != "attraction" && l.Category != "restaurant" && l.Category != "shopping");

            transportCount = _db.TransportTypes.Count();
            totalUsers = _db.Users.Count();
            openBugs = (from t in _db.SupportTickets.Include(x => x.Employee)
                        where t.Status != "resolved" &&
                              ((t.Employee != null && t.Employee.Department == "Dev") || t.Subject.Contains("[ส่งต่อแผนก Dev]"))
                        select t).Count();
        }
        catch { }

        ViewBag.LocationCount = locationCount;
        ViewBag.TransportCount = transportCount;
        ViewBag.OpenBugs = openBugs;
        ViewBag.TotalUsers = totalUsers;
        ViewBag.CategoryChartJson = JsonSerializer.Serialize(new int[] { attractionCount, restaurantCount, shoppingCount, otherCount });

        return View();
    }

    public IActionResult Places(string? search)
    {
        if (!CheckDevAccess()) return RedirectToAction("Login", "Account");

        ViewData["Department"] = "Dev";
        ViewData["ActiveMenu"] = "places";

        var locations = (from l in _db.Locations orderby l.Id descending select l).ToList();
        if (!string.IsNullOrEmpty(search))
        {
            locations = locations.Where(l =>
                l.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (l.Address != null && l.Address.Contains(search, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }
        ViewBag.Search = search;
        return View(locations);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddPlace(string name, string description, string address, decimal latitude, decimal longitude, string category, string? imageUrl, IFormFile? placeImage)
    {
        if (!CheckDevAccess()) return RedirectToAction("Login", "Account");

        string? savedImg = SaveUploadedFile(placeImage, "places") ?? imageUrl;
        try
        {
            Location loc = new();
            loc.Name = name;
            loc.Description = description;
            loc.Address = address;
            loc.Latitude = latitude;
            loc.Longitude = longitude;
            loc.Category = string.IsNullOrEmpty(category) ? "attraction" : category;
            loc.ImageUrl = string.IsNullOrEmpty(savedImg) ? "https://images.unsplash.com/photo-1563492065599-3520f775eeed?auto=format&fit=crop&w=600&q=80" : savedImg;
            loc.IsActive = true;
            loc.CreatedAt = DateTime.Now;
            loc.UpdatedAt = DateTime.Now;

            _db.Add(loc);
            _db.SaveChanges();
            TempData["DevMsg"] = "เพิ่มสถานที่ '" + name + "' สำเร็จแล้ว";
        }
        catch (Exception ex)
        {
            TempData["DevMsg"] = "เกิดข้อผิดพลาด: " + ex.Message;
        }
        return RedirectToAction("Places");
    }

    [HttpGet]
    public IActionResult EditPlace(int id)
    {
        if (!CheckDevAccess()) return RedirectToAction("Login", "Account");

        ViewData["Department"] = "Dev";
        ViewData["ActiveMenu"] = "places";

        var loc = (from l in _db.Locations where l.Id == id select l).FirstOrDefault();
        if (loc == null) return RedirectToAction("Places");

        return View(loc);
    }

    // แก้บัค checkbox isActive: ตั้งค่าเริ่มต้นเป็น false เพื่อรองรับกรณีเอาติ๊กออก
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditPlace(int id, string name, string? description, string? address, decimal latitude, decimal longitude, string category, string? imageUrl, IFormFile? placeImage, bool isActive = false)
    {
        if (!CheckDevAccess()) return RedirectToAction("Login", "Account");

        string? uploadedImg = SaveUploadedFile(placeImage, "places");

        try
        {
            var loc = (from l in _db.Locations where l.Id == id select l).FirstOrDefault();
            if (loc != null)
            {
                loc.Name = name;
                loc.Description = description;
                loc.Address = address;
                loc.Latitude = latitude;
                loc.Longitude = longitude;
                loc.Category = string.IsNullOrEmpty(category) ? "attraction" : category;
                loc.IsActive = isActive;

                if (!string.IsNullOrEmpty(uploadedImg))
                {
                    loc.ImageUrl = uploadedImg;
                }
                else if (!string.IsNullOrWhiteSpace(imageUrl))
                {
                    loc.ImageUrl = imageUrl;
                }

                loc.UpdatedAt = DateTime.Now;
                _db.Update(loc);
                _db.SaveChanges();
                TempData["DevMsg"] = "แก้ไขข้อมูลสถานที่ '" + loc.Name + "' เรียบร้อยแล้ว";
            }
        }
        catch (Exception ex)
        {
            TempData["DevMsg"] = "เกิดข้อผิดพลาด: " + ex.Message;
        }

        return RedirectToAction("Places");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeletePlace(int id)
    {
        if (!CheckDevAccess()) return RedirectToAction("Login", "Account");

        try
        {
            var loc = (from l in _db.Locations where l.Id == id select l).FirstOrDefault();
            if (loc != null)
            {
                // ลบจุดแวะพักที่อ้างอิงสถานที่นี้ก่อนเพื่อป้องกัน Foreign Key Error
                var refs = (from tl in _db.TripLocations where tl.LocationId == id select tl).ToList();
                if (refs.Count > 0) _db.RemoveRange(refs);

                _db.Remove(loc);
                _db.SaveChanges();
                TempData["DevMsg"] = "ลบสถานที่รหัส #" + id + " เรียบร้อยแล้ว";
            }
        }
        catch (Exception ex)
        {
            TempData["DevMsg"] = "ไม่สามารถลบสถานที่ได้: " + ex.Message;
        }
        return RedirectToAction("Places");
    }

    [HttpGet]
    public IActionResult Pricing()
    {
        if (!CheckDevAccess()) return RedirectToAction("Login", "Account");

        ViewData["Department"] = "Dev";
        ViewData["ActiveMenu"] = "pricing";

        var transports = (from t in _db.TransportTypes select t).ToList();
        return View(transports);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdatePricing(int id, string name, decimal baseFare, decimal farePerKm, decimal farePerStation, string? description)
    {
        if (!CheckDevAccess()) return RedirectToAction("Login", "Account");

        try
        {
            var t = (from x in _db.TransportTypes where x.Id == id select x).FirstOrDefault();
            if (t != null)
            {
                t.Name = name;
                t.BaseFare = baseFare;
                t.FarePerKm = farePerKm;
                t.FarePerStation = farePerStation;
                t.Description = description;
                t.UpdatedAt = DateTime.Now;
                _db.SaveChanges();
                TempData["PricingMsg"] = "บันทึกอัตราค่าโดยสาร '" + name + "' เรียบร้อยแล้ว";
            }
        }
        catch { }
        return RedirectToAction("Pricing");
    }

    public IActionResult Issues(string? search)
    {
        if (!CheckDevAccess()) return RedirectToAction("Login", "Account");

        ViewData["Department"] = "Dev";
        ViewData["ActiveMenu"] = "issues";

        var tickets = (from t in _db.SupportTickets.Include(x => x.Customer).Include(x => x.Employee)
                       where (t.Employee != null && t.Employee.Department == "Dev") ||
                             t.Subject.Contains("[ส่งต่อแผนก Dev]")
                       orderby t.CreatedAt descending
                       select t).ToList();

        if (!string.IsNullOrEmpty(search))
        {
            tickets = tickets.Where(t =>
                t.Subject.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                t.Description.Contains(search, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        ViewBag.Search = search;
        return View(tickets);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ResolveIssue(int id)
    {
        if (!CheckDevAccess()) return RedirectToAction("Login", "Account");

        try
        {
            var t = (from x in _db.SupportTickets where x.Id == id select x).FirstOrDefault();
            if (t != null)
            {
                t.Status = "resolved";
                t.UpdatedAt = DateTime.Now;
                _db.SaveChanges();
                TempData["DevIssueMsg"] = "ปิดงานรายการ #" + id + " เรียบร้อยแล้ว";
            }
        }
        catch { }
        return RedirectToAction("Issues");
    }

    public IActionResult UserList(string? search, string? roleFilter)
    {
        if (!CheckDevAccess()) return RedirectToAction("Login", "Account");

        ViewData["Department"] = "Dev";
        ViewData["ActiveMenu"] = "users";

        var users = (from u in _db.Users.Include(x => x.Employee)
                     select new UserListViewModel
                     {
                         Id = u.Id,
                         FullName = u.Employee != null ? (u.Employee.FirstName + " " + u.Employee.LastName) : u.UserName,
                         Username = u.UserName,
                         Email = u.Email,
                         PhoneNumber = u.PhoneNumber ?? "-",
                         Role = u.Role,
                         Status = u.Status,
                         Department = u.Employee != null ? u.Employee.Department : "-",
                         Position = u.Employee != null ? u.Employee.Position : "-",
                         Salary = u.Employee != null ? u.Employee.Salary : null
                     }).ToList();

        if (!string.IsNullOrEmpty(search))
        {
            users = users.Where(u =>
                (u.Username != null && u.Username.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (u.Email != null && u.Email.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (u.FullName != null && u.FullName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (u.Department != null && u.Department.Contains(search, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }

        if (!string.IsNullOrEmpty(roleFilter) && roleFilter != "all")
        {
            users = users.Where(u => u.Role == roleFilter).ToList();
        }

        ViewBag.Search = search;
        ViewBag.RoleFilter = roleFilter ?? "all";
        return View("~/Views/Account/UserList.cshtml", users);
    }

    [HttpGet]
    public IActionResult AddUser()
    {
        if (!CheckDevAccess()) return RedirectToAction("Login", "Account");

        ViewData["Department"] = "Dev";
        ViewData["ActiveMenu"] = "users";
        return View("~/Views/Account/AddUser.cshtml", new AddUserViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddUser(AddUserViewModel data)
    {
        if (!CheckDevAccess()) return RedirectToAction("Login", "Account");

        ViewData["Department"] = "Dev";
        ViewData["ActiveMenu"] = "users";

        if (string.IsNullOrEmpty(data.Username) || string.IsNullOrEmpty(data.Email) || string.IsNullOrEmpty(data.Password))
        {
            ViewBag.Status = "กรุณากรอก ชื่อผู้ใช้, อีเมล และรหัสผ่าน ให้ครบถ้วน";
            return View("~/Views/Account/AddUser.cshtml", data);
        }

        var checkUser = (from u in _db.Users where u.Email == data.Email || u.UserName == data.Username select u).FirstOrDefault();
        if (checkUser != null)
        {
            ViewBag.Status = "อีเมลหรือชื่อผู้ใช้นี้มีในระบบแล้ว";
            return View("~/Views/Account/AddUser.cshtml", data);
        }

        User newUser = new();
        newUser.UserName = data.Username;
        newUser.Email = data.Email;
        newUser.PhoneNumber = data.PhoneNumber;
        newUser.Password = HashPassword(data.Password);
        newUser.Role = (data.Role == "admin") ? "admin" : (data.Role == "employee" ? "employee" : "customer");
        newUser.Status = data.IsActive ? "active" : "pending";
        newUser.LastActive = DateTime.Now;

        _db.Add(newUser);
        _db.SaveChanges();

        if (newUser.Role == "employee" || newUser.Role == "admin")
        {
            Employee emp = new();
            emp.UserId = newUser.Id;
            emp.FirstName = !string.IsNullOrWhiteSpace(data.FirstName) ? data.FirstName : data.Username;
            emp.LastName = !string.IsNullOrWhiteSpace(data.LastName) ? data.LastName : "";
            emp.Department = !string.IsNullOrWhiteSpace(data.Department) ? data.Department : "Dev";
            emp.Position = !string.IsNullOrWhiteSpace(data.Position) ? data.Position : "Staff";
            emp.Salary = data.Salary ?? 25000m;
            emp.HireDate = DateOnly.FromDateTime(DateTime.Today);

            _db.Add(emp);
            _db.SaveChanges();
        }

        TempData["UserMsg"] = "เพิ่มบัญชี '" + newUser.UserName + "' เรียบร้อยแล้ว";
        return RedirectToAction("UserList");
    }

    [HttpGet]
    public IActionResult EditUser(int id)
    {
        if (!CheckDevAccess()) return RedirectToAction("Login", "Account");

        ViewData["Department"] = "Dev";
        ViewData["ActiveMenu"] = "users";

        var user = (from u in _db.Users.Include(x => x.Employee) where u.Id == id select u).FirstOrDefault();
        if (user == null) return RedirectToAction("UserList");

        var model = new EditUserViewModel
        {
            Id = user.Id,
            Username = user.UserName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Password = "",
            ConfirmPassword = "",
            Role = user.Role,
            IsActive = (user.Status == "active"),
            FirstName = user.Employee?.FirstName,
            LastName = user.Employee?.LastName,
            Department = user.Employee?.Department ?? "Dev",
            Position = user.Employee?.Position,
            Salary = user.Employee?.Salary ?? 25000m
        };

        return View("~/Views/Account/EditUser.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditUser(EditUserViewModel data)
    {
        if (!CheckDevAccess()) return RedirectToAction("Login", "Account");

        ViewData["Department"] = "Dev";
        ViewData["ActiveMenu"] = "users";

        if (!string.IsNullOrEmpty(data.Password) && data.Password != data.ConfirmPassword)
        {
            ViewBag.Status = "รหัสผ่านใหม่และยืนยันรหัสผ่านไม่ตรงกัน";
            return View("~/Views/Account/EditUser.cshtml", data);
        }

        var user = (from u in _db.Users.Include(x => x.Employee) where u.Id == data.Id select u).FirstOrDefault();
        if (user != null)
        {
            var checkDuplicate = (from u in _db.Users
                                  where (u.Email == data.Email || u.UserName == data.Username) && u.Id != data.Id
                                  select u).FirstOrDefault();

            if (checkDuplicate != null)
            {
                ViewBag.Status = "อีเมลหรือชื่อผู้ใช้นี้ซ้ำกับบัญชีอื่นในระบบ";
                return View("~/Views/Account/EditUser.cshtml", data);
            }

            user.UserName = data.Username!;
            user.Email = data.Email!;
            user.PhoneNumber = data.PhoneNumber;
            if (!string.IsNullOrEmpty(data.Password))
            {
                user.Password = HashPassword(data.Password);
            }
            user.Role = (data.Role == "admin") ? "admin" : (data.Role == "employee" ? "employee" : "customer");
            user.Status = data.IsActive ? "active" : "pending";

            if (user.Role == "employee" || user.Role == "admin")
            {
                if (user.Employee == null)
                {
                    Employee emp = new();
                    emp.UserId = user.Id;
                    emp.FirstName = !string.IsNullOrWhiteSpace(data.FirstName) ? data.FirstName : user.UserName;
                    emp.LastName = data.LastName ?? "";
                    emp.Department = data.Department ?? "Dev";
                    emp.Position = data.Position ?? "Staff";
                    emp.Salary = data.Salary ?? 25000m;
                    emp.HireDate = DateOnly.FromDateTime(DateTime.Today);
                    _db.Add(emp);
                }
                else
                {
                    user.Employee.FirstName = !string.IsNullOrWhiteSpace(data.FirstName) ? data.FirstName : user.UserName;
                    user.Employee.LastName = data.LastName ?? "";
                    user.Employee.Department = data.Department ?? "Dev";
                    user.Employee.Position = data.Position ?? "Staff";
                    user.Employee.Salary = data.Salary ?? 25000m;
                }
            }

            _db.Update(user);
            _db.SaveChanges();
            TempData["UserMsg"] = "แก้ไขข้อมูลบัญชี '" + user.UserName + "' เรียบร้อยแล้ว";
            return RedirectToAction("UserList");
        }

        return View("~/Views/Account/EditUser.cshtml", data);
    }

    // แก้บัค Foreign Key Error ตอนลบผู้ใช้: เคลียร์ข้อมูลลูกที่ผูกอยู่ก่อนลบ User
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteUser(int id)
    {
        if (!CheckDevAccess()) return RedirectToAction("Login", "Account");

        int currentUserId = HttpContext.Session.GetInt32("UserId") ?? 0;
        if (id == currentUserId)
        {
            TempData["UserMsg"] = "ไม่สามารถลบบัญชีที่กำลังใช้งานอยู่ได้";
            return RedirectToAction("UserList");
        }

        try
        {
            var user = (from u in _db.Users where u.Id == id select u).FirstOrDefault();
            if (user != null)
            {
                var trips = (from t in _db.UserTrips where t.UserId == id select t).ToList();
                if (trips.Count > 0) _db.RemoveRange(trips);

                var tickets = (from t in _db.SupportTickets where t.CustomerId == id select t).ToList();
                if (tickets.Count > 0) _db.RemoveRange(tickets);

                var payments = (from p in _db.Payments where p.UserId == id select p).ToList();
                if (payments.Count > 0) _db.RemoveRange(payments);

                var ads = (from a in _db.Advertisements where a.UserId == id select a).ToList();
                if (ads.Count > 0) _db.RemoveRange(ads);

                var emp = (from e in _db.Employees where e.UserId == id select e).FirstOrDefault();
                if (emp != null) _db.Remove(emp);

                _db.Remove(user);
                _db.SaveChanges();
                TempData["UserMsg"] = "ลบบัญชี '" + user.UserName + "' ออกจากระบบเรียบร้อยแล้ว";
            }
        }
        catch (Exception ex)
        {
            TempData["UserMsg"] = "ไม่สามารถลบบัญชีได้: " + ex.Message;
        }

        return RedirectToAction("UserList");
    }

    public IActionResult Settings()
    {
        if (!CheckDevAccess()) return RedirectToAction("Login", "Account");

        ViewData["Department"] = "Dev";
        ViewData["ActiveMenu"] = "settings";
        return View();
    }
}
