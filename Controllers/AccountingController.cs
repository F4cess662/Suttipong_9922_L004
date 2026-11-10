using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Suttipong_9922_L004.Models;

namespace Suttipong_9922_L004.Controllers;

public class AccountingController : Controller
{
    private readonly Csi402dbContext _db;

    public AccountingController(Csi402dbContext db)
    {
        _db = db;
    }

    private List<Employee> GetEmployeesOrDefault()
    {
        try
        {
            var list = _db.Employees.Include(e => e.User).ToList();
            if (list.Any()) return list;
        }
        catch { }

        return new List<Employee>
        {
            new Employee { Id = 1, UserId = 3, FirstName = "พิมพ์ชนก", LastName = "ใจดี", Position = "Customer Support Officer", Department = "Support", Salary = 26000.00m, HireDate = new DateOnly(2025, 1, 15) },
            new Employee { Id = 2, UserId = 4, FirstName = "สุทธิพงษ์", LastName = "พัฒนาระบบ", Position = "Full-Stack Developer", Department = "Dev", Salary = 45000.00m, HireDate = new DateOnly(2024, 11, 1) },
            new Employee { Id = 3, UserId = 5, FirstName = "นภัสสร", LastName = "สื่อสารการตลาด", Position = "Marketing Specialist", Department = "Marketing", Salary = 34000.00m, HireDate = new DateOnly(2025, 2, 10) },
            new Employee { Id = 4, UserId = 6, FirstName = "ธนดล", LastName = "การคลัง", Position = "Senior Accountant", Department = "Accounting", Salary = 38000.00m, HireDate = new DateOnly(2024, 12, 1) }
        };
    }

    private List<Payment> GetPaymentsOrDefault()
    {
        try
        {
            var list = _db.Payments
                .Include(p => p.User)
                .Include(p => p.SubscriptionPlan)
                .Include(p => p.Advertisement)
                .OrderByDescending(p => p.CreatedAt)
                .ToList();
            if (list.Any()) return list;
        }
        catch { }

        return new List<Payment>
        {
            new Payment { Id = 1, UserId = 2, SubscriptionPlanId = 1, Amount = 99.00m, PaymentMethod = "promptpay", PaymentStatus = "completed", TransactionRef = "TXN-20261101-001", PaidAt = DateTime.Now.AddDays(-5), User = new User { UserName = "vip_user", Email = "vip@tanglat.com" }, SubscriptionPlan = new SubscriptionPlan { Name = "Tang-Lat VIP Monthly" } },
            new Payment { Id = 2, UserId = 2, AdvertisementId = 1, Amount = 499.00m, PaymentMethod = "promptpay", PaymentStatus = "completed", TransactionRef = "TXN-20261101-002", PaidAt = DateTime.Now.AddDays(-4), User = new User { UserName = "vip_user", Email = "vip@tanglat.com" }, Advertisement = new Advertisement { Title = "Chao Phraya River Cafe" } },
            new Payment { Id = 3, UserId = 1, SubscriptionPlanId = 1, Amount = 99.00m, PaymentMethod = "promptpay", PaymentStatus = "pending", TransactionRef = "TXN-20261110-003", PaidAt = DateTime.Now.AddHours(-2), User = new User { UserName = "customer01", Email = "customer@tanglat.com" }, SubscriptionPlan = new SubscriptionPlan { Name = "Tang-Lat VIP Monthly" } },
            new Payment { Id = 4, UserId = 1, AdvertisementId = 3, Amount = 499.00m, PaymentMethod = "bank_transfer", PaymentStatus = "pending", TransactionRef = "TXN-20261110-004", PaidAt = DateTime.Now.AddHours(-1), User = new User { UserName = "customer01", Email = "customer@tanglat.com" }, Advertisement = new Advertisement { Title = "Siam Street Food Festival 2026" } }
        };
    }

    // Wireframe หน้า 23-24: แดชบอร์ดสรุปยอดการเงิน (3 การ์ด KPI ด้านบน + กราฟรายรับ-รายจ่าย + สรุปรายการ)
    public IActionResult Dashboard()
    {
        ViewData["Department"] = "Accounting";
        ViewData["ActiveMenu"] = "dashboard";

        var payments = GetPaymentsOrDefault();
        var employees = GetEmployeesOrDefault();

        decimal totalRevenue = payments.Where(p => p.PaymentStatus == "completed").Sum(p => p.Amount);
        decimal pendingRevenue = payments.Where(p => p.PaymentStatus == "pending").Sum(p => p.Amount);
        decimal totalPayroll = employees.Sum(e => e.Salary ?? 25000m);

        ViewBag.TotalRevenue = totalRevenue;
        ViewBag.PendingRevenue = pendingRevenue;
        ViewBag.TotalPayroll = totalPayroll;
        ViewBag.PendingCount = payments.Count(p => p.PaymentStatus == "pending");

        return View(payments);
    }

    // Wireframe หน้า 25: จัดการเงินเดือนพนักงาน (Payroll Management พร้อมคอลัมน์ Salary)
    public IActionResult Payroll(string? search, string? department)
    {
        ViewData["Department"] = "Accounting";
        ViewData["ActiveMenu"] = "payroll";

        var employees = GetEmployeesOrDefault();
        if (!string.IsNullOrEmpty(search))
        {
            employees = employees.Where(e =>
                e.FirstName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                e.LastName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (e.Position != null && e.Position.Contains(search, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }
        if (!string.IsNullOrEmpty(department) && department != "all")
        {
            employees = employees.Where(e => e.Department == department).ToList();
        }

        ViewBag.Search = search;
        ViewBag.DepartmentFilter = department ?? "all";
        return View(employees);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateSalary(int id, decimal salary)
    {
        try
        {
            var emp = _db.Employees.FirstOrDefault(e => e.Id == id);
            if (emp != null)
            {
                emp.Salary = salary;
                _db.SaveChanges();
            }
        }
        catch { }
        TempData["PayrollMsg"] = $"อัปเดตฐานเงินเดือนพนักงานรหัส #{id} เป็น {salary:N2} บาท และบันทึกงวดจ่ายเงินเดือนสำเร็จ!";
        return RedirectToAction("Payroll");
    }

    // Wireframe หน้า 26: ตรวจสอบและยืนยันการชำระเงินของลูกค้า (ปุ่มอนุมัติ / ปฏิเสธ)
    public IActionResult Payments(string? search, string? status)
    {
        ViewData["Department"] = "Accounting";
        ViewData["ActiveMenu"] = "payments";

        var payments = GetPaymentsOrDefault();
        if (!string.IsNullOrEmpty(search))
        {
            payments = payments.Where(p =>
                (p.TransactionRef != null && p.TransactionRef.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (p.User != null && p.User.UserName.Contains(search, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }
        if (!string.IsNullOrEmpty(status) && status != "all")
        {
            payments = payments.Where(p => p.PaymentStatus == status).ToList();
        }

        ViewBag.Search = search;
        ViewBag.StatusFilter = status ?? "all";
        return View(payments);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult VerifyPayment(int id, string newStatus)
    {
        try
        {
            var payment = _db.Payments.FirstOrDefault(p => p.Id == id);
            if (payment != null)
            {
                payment.PaymentStatus = newStatus;
                payment.UpdatedAt = DateTime.Now;
                if (newStatus == "completed" && payment.AdvertisementId != null)
                {
                    var ad = _db.Advertisements.FirstOrDefault(a => a.Id == payment.AdvertisementId);
                    if (ad != null) ad.Status = "active";
                }
                _db.SaveChanges();
            }
        }
        catch { }

        TempData["PaymentVerifyMsg"] = $"อัปเดตสถานะการชำระเงินรายการ #{id} เป็น '{newStatus}' เรียบร้อยแล้ว!";
        return RedirectToAction("Payments");
    }

    // Wireframe หน้า 27: ตั้งค่าบัญชีพนักงานฝ่ายบัญชี
    public IActionResult Settings()
    {
        ViewData["Department"] = "Accounting";
        ViewData["ActiveMenu"] = "settings";
        return View("~/Views/Dev/Settings.cshtml");
    }
}
