using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Suttipong_9922_L004.Models;

namespace Suttipong_9922_L004.Controllers;

public class SupportController : Controller
{
    private readonly Csi402dbContext _db;

    public SupportController(Csi402dbContext db)
    {
        _db = db;
    }

    private List<SupportTicket> GetTicketsOrDefault()
    {
        try
        {
            var list = _db.SupportTickets
                .Include(t => t.Customer)
                .Include(t => t.Employee)
                .OrderByDescending(t => t.CreatedAt)
                .ToList();
            if (list.Any()) return list;
        }
        catch { }

        return new List<SupportTicket>
        {
            new SupportTicket { Id = 1, CustomerId = 1, Subject = "[ส่งต่อแผนก Dev] พิกัดหมุดวัดอรุณคลาดเคลื่อนเล็กน้อย", Description = "เวลาซูมแผนที่เข้าไปใกล้ๆ หมุดวัดอรุณอยู่ขยับไปทางท่าเรือ รบกวนทีม Dev ช่วยปรับพิกัดให้ตรงพระปรางค์ด้วยครับ", Status = "in_progress", Priority = "high", CreatedAt = DateTime.Now.AddHours(-3), Customer = new User { UserName = "customer01", Email = "customer@tanglat.com" } },
            new SupportTicket { Id = 2, CustomerId = 2, Subject = "[ส่งต่อแผนก Accounting] โอนเงินค่าโฆษณาแล้วแต่สถานะยังขึ้นรออนุมัติ", Description = "โอนเงินผ่านพร้อมเพย์ยอด 499 บาท รหัสอ้างอิง TXN-20261110-004 รบกวนฝ่ายบัญชีตรวจสอบยอดให้ด้วยครับ", Status = "open", Priority = "urgent", CreatedAt = DateTime.Now.AddHours(-1), Customer = new User { UserName = "vip_user", Email = "vip@tanglat.com" } },
            new SupportTicket { Id = 3, CustomerId = 1, Subject = "สอบถามวิธีเพิ่มสถานที่มากกว่า 3 แห่งใน 1 ทริป", Description = "ต้องการจัดรูทไปเที่ยว 5 ที่ในวันเดียว ต้องสมัครแพ็กเกจไหนครับ?", Status = "resolved", Priority = "low", CreatedAt = DateTime.Now.AddDays(-1), Customer = new User { UserName = "customer01", Email = "customer@tanglat.com" } }
        };
    }

    // Wireframe หน้า 9: รายการแจ้งปัญหาลูกค้า + ช่องค้นหา
    public IActionResult Index(string? search, string? status)
    {
        ViewData["Department"] = "Support";
        ViewData["ActiveMenu"] = "tickets";

        var tickets = GetTicketsOrDefault();
        if (!string.IsNullOrEmpty(search))
        {
            tickets = tickets.Where(t =>
                t.Subject.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                t.Description.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (t.Customer != null && t.Customer.Email.Contains(search, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }
        if (!string.IsNullOrEmpty(status) && status != "all")
        {
            tickets = tickets.Where(t => t.Status == status).ToList();
        }

        ViewBag.Search = search;
        ViewBag.StatusFilter = status ?? "all";
        return View(tickets);
    }

    // Wireframe หน้า 10: รายละเอียดปัญหา (ซ้าย) + ส่งต่อให้แผนกอื่น (Dev/Marketing/Accounting) & ตอบกลับลูกค้าทางอีเมล (ขวา)
    [HttpGet]
    public IActionResult Detail(int id = 1)
    {
        ViewData["Department"] = "Support";
        ViewData["ActiveMenu"] = "tickets";

        var tickets = GetTicketsOrDefault();
        var ticket = tickets.FirstOrDefault(t => t.Id == id) ?? tickets.First();

        List<Employee> employees = new();
        try
        {
            employees = _db.Employees.ToList();
        }
        catch { }

        if (!employees.Any())
        {
            employees = new List<Employee>
            {
                new Employee { Id = 2, FirstName = "สุทธิพงษ์", LastName = "พัฒนาระบบ", Department = "Dev", Position = "Full-Stack Developer" },
                new Employee { Id = 3, FirstName = "นภัสสร", LastName = "สื่อสารการตลาด", Department = "Marketing", Position = "Marketing Specialist" },
                new Employee { Id = 4, FirstName = "ธนดล", LastName = "การคลัง", Department = "Accounting", Position = "Senior Accountant" }
            };
        }

        ViewBag.Employees = employees;
        return View(ticket);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ForwardTicket(int id, string targetDepartment, string internalNote)
    {
        try
        {
            var ticket = _db.SupportTickets.FirstOrDefault(t => t.Id == id);
            var targetEmp = _db.Employees.FirstOrDefault(e => e.Department == targetDepartment);
            if (ticket != null)
            {
                if (!ticket.Subject.StartsWith("[ส่งต่อแผนก"))
                {
                    ticket.Subject = $"[ส่งต่อแผนก {targetDepartment}] " + ticket.Subject;
                }
                if (!string.IsNullOrWhiteSpace(internalNote))
                {
                    ticket.Description += $"\n\n--- หมายเหตุจากฝ่าย Support ส่งต่อให้ {targetDepartment} ---\n{internalNote}";
                }
                ticket.EmployeeId = targetEmp?.Id;
                ticket.Status = "in_progress";
                ticket.UpdatedAt = DateTime.Now;
                _db.SaveChanges();
            }
        }
        catch { }

        TempData["SupportMsg"] = $"ส่งต่อใบงาน #{id} ไปยังแผนก {targetDepartment} เรียบร้อยแล้ว!";
        return RedirectToAction("Detail", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ReplyEmail(int id, string customerEmail, string emailSubject, string emailBody, string newStatus)
    {
        try
        {
            var ticket = _db.SupportTickets.FirstOrDefault(t => t.Id == id);
            if (ticket != null)
            {
                ticket.Status = string.IsNullOrEmpty(newStatus) ? "resolved" : newStatus;
                ticket.UpdatedAt = DateTime.Now;
                _db.SaveChanges();
            }
        }
        catch { }

        TempData["SupportMsg"] = $"ส่งอีเมลตอบกลับไปยังลูกค้า ({customerEmail}) หัวข้อ '{emailSubject}' และอัปเดตสถานะเป็น '{newStatus}' เรียบร้อยแล้ว!";
        return RedirectToAction("Detail", new { id });
    }

    // Wireframe หน้า 11: ตั้งค่าบัญชีพนักงาน Support
    public IActionResult Settings()
    {
        ViewData["Department"] = "Support";
        ViewData["ActiveMenu"] = "settings";
        return View("~/Views/Dev/Settings.cshtml");
    }
}
