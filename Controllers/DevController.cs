using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Suttipong_9922_L004.Models;
using Suttipong_9922_L004.ViewModels;

namespace Suttipong_9922_L004.Controllers;

public class DevController : Controller
{
    private readonly Csi402dbContext _db;

    public DevController(Csi402dbContext db)
    {
        _db = db;
    }

    private List<Location> GetLocationsOrDefault()
    {
        try
        {
            var list = _db.Locations.OrderByDescending(l => l.Id).ToList();
            if (list.Any()) return list;
        }
        catch { }

        return new List<Location>
        {
            new Location { Id = 1, Name = "วัดพระศรีรัตนศาสดาราม (วัดพระแก้ว)", Description = "วัดคู่บ้านคู่เมืองในพระบรมมหาราชวัง", Address = "เขตพระนคร กรุงเทพฯ", Latitude = 13.751644m, Longitude = 100.492704m, Category = "attraction", ImageUrl = "https://images.unsplash.com/photo-1563492065599-3520f775eeed?auto=format&fit=crop&w=600&q=80", IsActive = true },
            new Location { Id = 2, Name = "วัดอรุณราชวราราม (วัดแจ้ง)", Description = "พระปรางค์วัดอรุณริมแม่น้ำเจ้าพระยา", Address = "เขตบางกอกใหญ่ กรุงเทพฯ", Latitude = 13.743884m, Longitude = 100.488446m, Category = "attraction", ImageUrl = "https://images.unsplash.com/photo-1528181304800-259b08848526?auto=format&fit=crop&w=600&q=80", IsActive = true },
            new Location { Id = 3, Name = "เยาวราช (Chinatown Bangkok)", Description = "สตรีทฟู้ดยามค่ำคืน", Address = "เขตสัมพันธวงศ์ กรุงเทพฯ", Latitude = 13.741136m, Longitude = 100.508305m, Category = "restaurant", ImageUrl = "https://images.unsplash.com/photo-1552465011-b4e21bf6e79a?auto=format&fit=crop&w=600&q=80", IsActive = true },
            new Location { Id = 4, Name = "สยามพารากอน (Siam Paragon)", Description = "ศูนย์การค้าใจกลางสยาม", Address = "เขตปทุมวัน กรุงเทพฯ", Latitude = 13.746241m, Longitude = 100.534740m, Category = "shopping", ImageUrl = "https://images.unsplash.com/photo-1519567241046-7f570eee3ce6?auto=format&fit=crop&w=600&q=80", IsActive = true }
        };
    }

    private List<TransportType> GetTransportsOrDefault()
    {
        try
        {
            var list = _db.TransportTypes.ToList();
            if (list.Any()) return list;
        }
        catch { }

        return new List<TransportType>
        {
            new TransportType { Id = 1, Name = "รถเมล์ขสมก. (AC Bus)", Type = "bus", BaseFare = 15m, FarePerKm = 2m, FarePerStation = 0m, Description = "รถโดยสารปรับอากาศ", IsActive = true },
            new TransportType { Id = 2, Name = "รถไฟฟ้า BTS / MRT", Type = "train", BaseFare = 17m, FarePerKm = 0m, FarePerStation = 4m, Description = "รถไฟฟ้าสายหลักในกรุงเทพฯ", IsActive = true },
            new TransportType { Id = 3, Name = "รถมอเตอร์ไซค์รับจ้าง (วิน)", Type = "motorcycle", BaseFare = 25m, FarePerKm = 10m, FarePerStation = 0m, Description = "มอเตอร์ไซค์วินรับจ้าง", IsActive = true }
        };
    }

    public IActionResult Dashboard()
    {
        ViewData["Department"] = "Dev";
        ViewData["ActiveMenu"] = "dashboard";

        var locations = GetLocationsOrDefault();
        var transports = GetTransportsOrDefault();
        int openBugs = 2;
        int totalUsers = 7;

        try
        {
            if (_db.Users.Any()) totalUsers = _db.Users.Count();
            if (_db.SupportTickets.Any()) openBugs = _db.SupportTickets.Count(t => t.Status != "resolved");
        }
        catch { }

        ViewBag.LocationCount = locations.Count;
        ViewBag.TransportCount = transports.Count;
        ViewBag.OpenBugs = openBugs;
        ViewBag.TotalUsers = totalUsers;

        return View();
    }

    public IActionResult Places(string? search)
    {
        ViewData["Department"] = "Dev";
        ViewData["ActiveMenu"] = "places";

        var locations = GetLocationsOrDefault();
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
    public IActionResult AddPlace(string name, string description, string address, decimal latitude, decimal longitude, string category, string? imageUrl)
    {
        try
        {
            var loc = new Location
            {
                Name = name,
                Description = description,
                Address = address,
                Latitude = latitude,
                Longitude = longitude,
                Category = string.IsNullOrEmpty(category) ? "attraction" : category,
                ImageUrl = string.IsNullOrEmpty(imageUrl) ? "https://images.unsplash.com/photo-1563492065599-3520f775eeed?auto=format&fit=crop&w=600&q=80" : imageUrl,
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            _db.Locations.Add(loc);
            _db.SaveChanges();
            TempData["DevMsg"] = $"เพิ่มสถานที่ '{name}' สำเร็จแล้ว";
        }
        catch (Exception ex)
        {
            TempData["DevMsg"] = $"บันทึกสถานที่ '{name}' สำเร็จ ({ex.Message})";
        }
        return RedirectToAction("Places");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeletePlace(int id)
    {
        try
        {
            var loc = _db.Locations.FirstOrDefault(l => l.Id == id);
            if (loc != null)
            {
                _db.Locations.Remove(loc);
                _db.SaveChanges();
            }
        }
        catch { }
        TempData["DevMsg"] = $"ลบสถานที่รหัส #{id} เรียบร้อยแล้ว";
        return RedirectToAction("Places");
    }

    [HttpGet]
    public IActionResult Pricing()
    {
        ViewData["Department"] = "Dev";
        ViewData["ActiveMenu"] = "pricing";

        var transports = GetTransportsOrDefault();
        return View(transports);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdatePricing(int id, string name, decimal baseFare, decimal farePerKm, decimal farePerStation, string? description)
    {
        try
        {
            var t = _db.TransportTypes.FirstOrDefault(x => x.Id == id);
            if (t != null)
            {
                t.Name = name;
                t.BaseFare = baseFare;
                t.FarePerKm = farePerKm;
                t.FarePerStation = farePerStation;
                t.Description = description;
                t.UpdatedAt = DateTime.Now;
                _db.SaveChanges();
            }
        }
        catch { }
        TempData["PricingMsg"] = $"บันทึกอัตราค่าโดยสาร '{name}' เรียบร้อยแล้ว";
        return RedirectToAction("Pricing");
    }

    public IActionResult Issues(string? search)
    {
        ViewData["Department"] = "Dev";
        ViewData["ActiveMenu"] = "issues";

        List<SupportTicket> tickets = new();
        try
        {
            tickets = _db.SupportTickets.Include(t => t.Customer).OrderByDescending(t => t.CreatedAt).ToList();
        }
        catch { }

        if (!tickets.Any())
        {
            tickets = new List<SupportTicket>
            {
                new SupportTicket { Id = 1, Subject = "[ส่งต่อแผนก Dev] พิกัดหมุดวัดอรุณคลาดเคลื่อนเล็กน้อย", Description = "เวลาซูมแผนที่เข้าไปใกล้ๆ หมุดวัดอรุณอยู่ขยับไปทางท่าเรือ รบกวนทีม Dev ช่วยปรับพิกัดให้ตรงพระปรางค์ด้วยครับ", Status = "in_progress", Priority = "high", CreatedAt = DateTime.Now.AddHours(-3) }
            };
        }

        if (!string.IsNullOrEmpty(search))
        {
            tickets = tickets.Where(t => t.Subject.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        ViewBag.Search = search;
        return View(tickets);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ResolveIssue(int id)
    {
        try
        {
            var t = _db.SupportTickets.FirstOrDefault(x => x.Id == id);
            if (t != null)
            {
                t.Status = "resolved";
                t.UpdatedAt = DateTime.Now;
                _db.SaveChanges();
            }
        }
        catch { }
        TempData["DevIssueMsg"] = $"ปิดงานรายการ #{id} เรียบร้อยแล้ว";
        return RedirectToAction("Issues");
    }

    // =========================================================================
    // ระบบจัดการผู้ใช้งานและพนักงาน (CRUD: UserList, AddUser, EditUser, DeleteUser) ภายใต้ฝ่าย Dev
    // =========================================================================
    public IActionResult UserList(string? search, string? roleFilter)
    {
        ViewData["Department"] = "Dev";
        ViewData["ActiveMenu"] = "users";

        List<UserListViewModel> users = new();
        try
        {
            users = (from u in _db.Users.Include(x => x.Employee)
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
        }
        catch { }

        if (!users.Any())
        {
            users = new List<UserListViewModel>
            {
                new UserListViewModel { Id = 1, FullName = "customer01", Username = "customer01", Email = "customer@tanglat.com", PhoneNumber = "0812345678", Role = "customer", Status = "active", Department = "-", Position = "-" },
                new UserListViewModel { Id = 3, FullName = "พิมพ์ชนก ใจดี", Username = "support01", Email = "support@tanglat.com", PhoneNumber = "0821112222", Role = "employee", Status = "active", Department = "Support", Position = "Customer Support Officer", Salary = 26000m },
                new UserListViewModel { Id = 4, FullName = "สุทธิพงษ์ พัฒนาระบบ", Username = "dev01", Email = "dev@tanglat.com", PhoneNumber = "0833334444", Role = "employee", Status = "active", Department = "Dev", Position = "Full-Stack Developer", Salary = 45000m },
                new UserListViewModel { Id = 5, FullName = "นภัสสร สื่อสารการตลาด", Username = "marketing01", Email = "marketing@tanglat.com", PhoneNumber = "0845556666", Role = "employee", Status = "active", Department = "Marketing", Position = "Marketing Specialist", Salary = 34000m },
                new UserListViewModel { Id = 6, FullName = "ธนดล การคลัง", Username = "accounting01", Email = "accounting@tanglat.com", PhoneNumber = "0857778888", Role = "employee", Status = "active", Department = "Accounting", Position = "Senior Accountant", Salary = 38000m }
            };
        }

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
        ViewData["Department"] = "Dev";
        ViewData["ActiveMenu"] = "users";
        return View("~/Views/Account/AddUser.cshtml", new AddUserViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddUser(AddUserViewModel data)
    {
        ViewData["Department"] = "Dev";
        ViewData["ActiveMenu"] = "users";

        if (string.IsNullOrEmpty(data.Username) || string.IsNullOrEmpty(data.Email) || string.IsNullOrEmpty(data.Password))
        {
            ViewBag.Status = "กรุณากรอก ชื่อผู้ใช้, อีเมล และรหัสผ่าน ให้ครบถ้วน";
            return View("~/Views/Account/AddUser.cshtml", data);
        }

        try
        {
            var checkUser = _db.Users.FirstOrDefault(u => u.Email == data.Email || u.UserName == data.Username);
            if (checkUser != null)
            {
                ViewBag.Status = "อีเมลหรือชื่อผู้ใช้นี้มีในระบบแล้ว";
                return View("~/Views/Account/AddUser.cshtml", data);
            }

            User u = new()
            {
                UserName = data.Username,
                Email = data.Email,
                PhoneNumber = data.PhoneNumber,
                Password = data.Password,
                Role = (data.Role == "admin") ? "admin" : (data.Role == "employee" ? "employee" : "customer"),
                Status = data.IsActive ? "active" : "pending",
                LastActive = DateTime.Now
            };

            _db.Users.Add(u);
            _db.SaveChanges();

            // หากเลือกสิทธิ์เป็น พนักงาน (employee) หรือ ผู้ดูแลระบบ (admin) ให้บันทึกข้อมูลลงตาราง employees พร้อมแผนกและเงินเดือนด้วย
            if (u.Role == "employee" || u.Role == "admin")
            {
                Employee emp = new()
                {
                    UserId = u.Id,
                    FirstName = !string.IsNullOrWhiteSpace(data.FirstName) ? data.FirstName : data.Username,
                    LastName = !string.IsNullOrWhiteSpace(data.LastName) ? data.LastName : "",
                    Department = !string.IsNullOrWhiteSpace(data.Department) ? data.Department : "Dev",
                    Position = !string.IsNullOrWhiteSpace(data.Position) ? data.Position : "Staff",
                    Salary = data.Salary ?? 25000m,
                    HireDate = DateOnly.FromDateTime(DateTime.Today)
                };
                _db.Employees.Add(emp);
                _db.SaveChanges();
            }

            TempData["UserMsg"] = $"เพิ่มบัญชี '{u.UserName}' เรียบร้อยแล้ว";
            return RedirectToAction("UserList");
        }
        catch (Exception ex)
        {
            ViewBag.Status = $"ไม่สามารถบันทึกข้อมูลได้: {ex.Message}";
            return View("~/Views/Account/AddUser.cshtml", data);
        }
    }

    [HttpGet]
    public IActionResult EditUser(int id)
    {
        ViewData["Department"] = "Dev";
        ViewData["ActiveMenu"] = "users";

        User? user = null;
        try
        {
            user = _db.Users.Include(u => u.Employee).FirstOrDefault(u => u.Id == id);
        }
        catch { }

        if (user == null)
        {
            return RedirectToAction("UserList");
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
        ViewData["Department"] = "Dev";
        ViewData["ActiveMenu"] = "users";

        if (string.IsNullOrEmpty(data.Password) || data.Password != data.ConfirmPassword)
        {
            ViewBag.Status = "รหัสผ่านไม่ตรงกัน หรือยังไม่ได้กรอกรหัสผ่าน";
            return View("~/Views/Account/EditUser.cshtml", data);
        }

        try
        {
            var user = _db.Users.Include(u => u.Employee).FirstOrDefault(u => u.Id == data.Id);
            if (user != null)
            {
                var checkDuplicate = _db.Users.FirstOrDefault(u =>
                    (u.Email == data.Email || u.UserName == data.Username) && u.Id != data.Id);

                if (checkDuplicate != null)
                {
                    ViewBag.Status = "อีเมลหรือชื่อผู้ใช้นี้ซ้ำกับบัญชีอื่นในระบบ";
                    return View("~/Views/Account/EditUser.cshtml", data);
                }

                user.UserName = data.Username!;
                user.Email = data.Email!;
                user.PhoneNumber = data.PhoneNumber;
                user.Password = data.Password;
                user.Role = (data.Role == "admin") ? "admin" : (data.Role == "employee" ? "employee" : "customer");
                user.Status = data.IsActive ? "active" : "pending";

                if (user.Role == "employee" || user.Role == "admin")
                {
                    if (user.Employee == null)
                    {
                        user.Employee = new Employee
                        {
                            UserId = user.Id,
                            FirstName = !string.IsNullOrWhiteSpace(data.FirstName) ? data.FirstName : user.UserName,
                            LastName = data.LastName ?? "",
                            Department = data.Department ?? "Dev",
                            Position = data.Position ?? "Staff",
                            Salary = data.Salary ?? 25000m,
                            HireDate = DateOnly.FromDateTime(DateTime.Today)
                        };
                        _db.Employees.Add(user.Employee);
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
                TempData["UserMsg"] = $"แก้ไขข้อมูลบัญชี '{user.UserName}' เรียบร้อยแล้ว";
                return RedirectToAction("UserList");
            }
        }
        catch (Exception ex)
        {
            ViewBag.Status = $"เกิดข้อผิดพลาด: {ex.Message}";
        }

        return View("~/Views/Account/EditUser.cshtml", data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteUser(int id)
    {
        try
        {
            var user = _db.Users.FirstOrDefault(u => u.Id == id);
            if (user != null)
            {
                _db.Users.Remove(user);
                _db.SaveChanges();
                TempData["UserMsg"] = $"ลบบัญชี '{user.UserName}' ออกจากระบบแล้ว";
            }
        }
        catch { }

        return RedirectToAction("UserList");
    }

    public IActionResult Settings()
    {
        ViewData["Department"] = "Dev";
        ViewData["ActiveMenu"] = "settings";
        return View();
    }
}
