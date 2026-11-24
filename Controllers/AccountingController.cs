using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
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

    // ตรวจสอบสิทธิ์การเข้าถึง: อนุญาตเฉพาะฝ่าย Accounting เท่านั้น
    private bool CheckAccountingAccess()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var role = HttpContext.Session.GetString("Role");
        var dept = HttpContext.Session.GetString("Department");
        return userId != null && role == "employee" && dept == "Accounting";
    }

    public IActionResult Dashboard()
    {
        if (!CheckAccountingAccess()) return RedirectToAction("Login", "Account");

        ViewData["Department"] = "Accounting";
        ViewData["ActiveMenu"] = "dashboard";

        var payments = (from p in _db.Payments.Include(x => x.User).Include(x => x.SubscriptionPlan).Include(x => x.Advertisement)
                        orderby p.CreatedAt descending
                        select p).ToList();
        var employees = (from e in _db.Employees select e).ToList();

        decimal totalRevenue = payments.Where(p => p.PaymentStatus == "completed").Sum(p => p.Amount);
        decimal pendingRevenue = payments.Where(p => p.PaymentStatus == "pending").Sum(p => p.Amount);
        decimal totalPayroll = employees.Sum(e => e.Salary ?? 25000m);

        decimal vipRev = payments.Where(p => p.PaymentStatus == "completed" && p.SubscriptionPlanId != null && p.AdvertisementId == null).Sum(p => p.Amount);
        decimal adsRev = payments.Where(p => p.PaymentStatus == "completed" && p.AdvertisementId != null).Sum(p => p.Amount);

        ViewBag.TotalRevenue = totalRevenue;
        ViewBag.PendingRevenue = pendingRevenue;
        ViewBag.TotalPayroll = totalPayroll;
        ViewBag.PendingCount = payments.Count(p => p.PaymentStatus == "pending");
        ViewBag.FinanceChartJson = JsonSerializer.Serialize(new decimal[] { vipRev, adsRev, pendingRevenue });

        return View(payments);
    }

    public IActionResult Payroll(string? search, string? department)
    {
        if (!CheckAccountingAccess()) return RedirectToAction("Login", "Account");

        ViewData["Department"] = "Accounting";
        ViewData["ActiveMenu"] = "payroll";

        var employees = (from e in _db.Employees.Include(x => x.User) select e).ToList();
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
        if (!CheckAccountingAccess()) return RedirectToAction("Login", "Account");

        try
        {
            var emp = (from e in _db.Employees where e.Id == id select e).FirstOrDefault();
            if (emp != null)
            {
                emp.Salary = salary;
                _db.SaveChanges();
                TempData["PayrollMsg"] = "อัปเดตฐานเงินเดือนพนักงานรหัส #" + id + " เป็น " + salary.ToString("N2") + " บาท เรียบร้อยแล้ว";
            }
        }
        catch { }
        return RedirectToAction("Payroll");
    }

    [HttpGet]
    public IActionResult Payslip(int id = 1)
    {
        if (!CheckAccountingAccess()) return RedirectToAction("Login", "Account");

        var emp = (from e in _db.Employees where e.Id == id select e).FirstOrDefault();
        if (emp == null) return RedirectToAction("Payroll");

        return View(emp);
    }

    [HttpGet]
    public IActionResult ExportPayrollCsv()
    {
        if (!CheckAccountingAccess()) return RedirectToAction("Login", "Account");

        var employees = (from e in _db.Employees select e).ToList();
        var sb = new StringBuilder();
        sb.AppendLine("รหัสพนักงาน,ชื่อ-นามสกุล,ตำแหน่ง,แผนก,วันที่เริ่มงาน,เงินเดือน (บาท),หักประกันสังคม (บาท),รับสุทธิ (บาท)");

        foreach (var e in employees)
        {
            decimal salary = e.Salary ?? 25000m;
            decimal sso = Math.Min(750m, Math.Round(salary * 0.05m, 2));
            decimal net = salary - sso;
            sb.AppendLine($"EMP-{e.Id:D3},\"{e.FirstName} {e.LastName}\",\"{e.Position}\",\"{e.Department}\",{e.HireDate:yyyy-MM-dd},{salary:F2},{sso:F2},{net:F2}");
        }

        byte[] preamble = Encoding.UTF8.GetPreamble();
        byte[] body = Encoding.UTF8.GetBytes(sb.ToString());
        byte[] content = preamble.Concat(body).ToArray();

        return File(content, "text/csv; charset=utf-8", "TangLat_Payroll_" + DateTime.Now.ToString("yyyyMMdd") + ".csv");
    }

    [HttpGet]
    public IActionResult ExportPaymentsCsv()
    {
        if (!CheckAccountingAccess()) return RedirectToAction("Login", "Account");

        var payments = (from p in _db.Payments.Include(x => x.User).Include(x => x.SubscriptionPlan).Include(x => x.Advertisement)
                        orderby p.CreatedAt descending
                        select p).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("รหัสรายการ,ผู้ชำระเงิน,ประเภทบริการ,รหัสอ้างอิง,ช่องทางชำระเงิน,จำนวนเงิน (บาท),สถานะ,วันที่ทำรายการ");

        foreach (var p in payments)
        {
            string serviceName = p.SubscriptionPlan != null
                ? "VIP: " + p.SubscriptionPlan.Name
                : (p.Advertisement != null ? "Ads: " + p.Advertisement.Title : "บริการทั่วไป");
            string cleanRef = (p.TransactionRef ?? "-").Split('|')[0].Trim();
            sb.AppendLine($"PAY-{p.Id:D4},\"{p.User?.UserName ?? "customer"}\",\"{serviceName}\",\"{cleanRef}\",{p.PaymentMethod},{p.Amount:F2},{p.PaymentStatus},{p.PaidAt:yyyy-MM-dd HH:mm}");
        }

        byte[] preamble = Encoding.UTF8.GetPreamble();
        byte[] body = Encoding.UTF8.GetBytes(sb.ToString());
        byte[] content = preamble.Concat(body).ToArray();

        return File(content, "text/csv; charset=utf-8", "TangLat_Financial_Report_" + DateTime.Now.ToString("yyyyMMdd") + ".csv");
    }

    public IActionResult Payments(string? search, string? status)
    {
        if (!CheckAccountingAccess()) return RedirectToAction("Login", "Account");

        ViewData["Department"] = "Accounting";
        ViewData["ActiveMenu"] = "payments";

        var payments = (from p in _db.Payments.Include(x => x.User).Include(x => x.SubscriptionPlan).Include(x => x.Advertisement)
                        orderby p.CreatedAt descending
                        select p).ToList();

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
        if (!CheckAccountingAccess()) return RedirectToAction("Login", "Account");

        string autoActionDetail = "";
        try
        {
            var payment = (from p in _db.Payments.Include(x => x.SubscriptionPlan)
                           where p.Id == id
                           select p).FirstOrDefault();

            if (payment != null)
            {
                payment.PaymentStatus = newStatus;
                payment.PaidAt = DateTime.Now;
                payment.UpdatedAt = DateTime.Now;

                if (newStatus == "completed")
                {
                    if (payment.AdvertisementId != null)
                    {
                        var ad = (from a in _db.Advertisements where a.Id == payment.AdvertisementId select a).FirstOrDefault();
                        if (ad != null)
                        {
                            ad.Status = "active";
                            if (ad.EndDate < DateOnly.FromDateTime(DateTime.Today))
                            {
                                ad.StartDate = DateOnly.FromDateTime(DateTime.Today);
                                ad.EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(15));
                            }
                            ad.UpdatedAt = DateTime.Now;
                            autoActionDetail = " และเปิดแสดงผลป้ายโฆษณา '" + ad.Title + "' อัตโนมัติแล้ว";
                        }
                    }
                    else if (payment.SubscriptionPlanId != null)
                    {
                        int days = payment.SubscriptionPlan?.DurationDays ?? 30;
                        autoActionDetail = " และเปิดสิทธิ์สมาชิก VIP นาน " + days + " วันอัตโนมัติแล้ว";
                    }
                }
                else if (newStatus == "failed" && payment.AdvertisementId != null)
                {
                    var ad = (from a in _db.Advertisements where a.Id == payment.AdvertisementId select a).FirstOrDefault();
                    if (ad != null) ad.Status = "rejected";
                }

                _db.SaveChanges();
            }
        }
        catch { }

        TempData["PaymentVerifyMsg"] = "อัปเดตสถานะการชำระเงินรายการ #" + id + " เป็น '" + newStatus + "'" + autoActionDetail;
        return RedirectToAction("Payments");
    }

    public IActionResult Issues(string? search)
    {
        if (!CheckAccountingAccess()) return RedirectToAction("Login", "Account");

        ViewData["Department"] = "Accounting";
        ViewData["ActiveMenu"] = "issues";

        var tickets = (from t in _db.SupportTickets.Include(x => x.Customer).Include(x => x.Employee)
                       where (t.Employee != null && t.Employee.Department == "Accounting") ||
                             t.Subject.Contains("[ส่งต่อแผนก Accounting]")
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
    public IActionResult ResolveIssue(int id, string? resolutionNote)
    {
        if (!CheckAccountingAccess()) return RedirectToAction("Login", "Account");

        try
        {
            var t = (from x in _db.SupportTickets where x.Id == id select x).FirstOrDefault();
            if (t != null)
            {
                if (!string.IsNullOrWhiteSpace(resolutionNote))
                {
                    t.Description += "\n\n[ฝ่าย Accounting ดำเนินการแล้ว - " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + "]: " + resolutionNote;
                }
                t.Status = "resolved";
                t.UpdatedAt = DateTime.Now;
                _db.SaveChanges();
                TempData["AccountingIssueMsg"] = "ดำเนินการแก้ไขคำร้องฝ่ายบัญชี #" + id + " เสร็จสิ้นแล้ว";
            }
        }
        catch { }
        return RedirectToAction("Issues");
    }

    public IActionResult Settings()
    {
        if (!CheckAccountingAccess()) return RedirectToAction("Login", "Account");

        ViewData["Department"] = "Accounting";
        ViewData["ActiveMenu"] = "settings";
        return View("~/Views/Dev/Settings.cshtml");
    }
}
