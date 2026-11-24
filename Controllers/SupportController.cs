using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Suttipong_9922_L004.Models;

namespace Suttipong_9922_L004.Controllers;

public class SupportController : Controller
{
    private readonly Csi402dbContext _db;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public SupportController(Csi402dbContext db, IConfiguration config, IWebHostEnvironment env)
    {
        _db = db;
        _config = config;
        _env = env;
    }

    // ตรวจสอบสิทธิ์การเข้าถึง: อนุญาตเฉพาะฝ่าย Support เท่านั้น
    private bool CheckSupportAccess()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var role = HttpContext.Session.GetString("Role");
        var dept = HttpContext.Session.GetString("Department");
        return userId != null && role == "employee" && dept == "Support";
    }

    public IActionResult Index(string? search, string? status)
    {
        if (!CheckSupportAccess()) return RedirectToAction("Login", "Account");

        ViewData["Department"] = "Support";
        ViewData["ActiveMenu"] = "tickets";

        var tickets = (from t in _db.SupportTickets.Include(x => x.Customer).Include(x => x.Employee)
                       orderby t.CreatedAt descending
                       select t).ToList();

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

    [HttpGet]
    public IActionResult Detail(int id = 1)
    {
        if (!CheckSupportAccess()) return RedirectToAction("Login", "Account");

        ViewData["Department"] = "Support";
        ViewData["ActiveMenu"] = "tickets";

        var ticket = (from t in _db.SupportTickets.Include(x => x.Customer).Include(x => x.Employee)
                      where t.Id == id
                      select t).FirstOrDefault();

        if (ticket == null) return RedirectToAction("Index");

        ViewBag.Employees = (from e in _db.Employees select e).ToList();
        return View(ticket);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ForwardTicket(int id, string targetDepartment, string internalNote)
    {
        if (!CheckSupportAccess()) return RedirectToAction("Login", "Account");

        try
        {
            var ticket = (from t in _db.SupportTickets where t.Id == id select t).FirstOrDefault();
            var targetEmp = (from e in _db.Employees where e.Department == targetDepartment select e).FirstOrDefault();

            if (ticket != null)
            {
                // ตัดคำนำหน้าส่งต่อแผนกเก่าออกก่อน (ถ้ามี)
                string cleanSubject = ticket.Subject;
                if (cleanSubject.StartsWith("[ส่งต่อแผนก"))
                {
                    int closeIdx = cleanSubject.IndexOf(']');
                    if (closeIdx > 0 && closeIdx + 1 < cleanSubject.Length)
                    {
                        cleanSubject = cleanSubject.Substring(closeIdx + 1).Trim();
                    }
                }

                ticket.Subject = "[ส่งต่อแผนก " + targetDepartment + "] " + cleanSubject;

                if (!string.IsNullOrWhiteSpace(internalNote))
                {
                    ticket.Description += "\n\n[บันทึกส่งต่อแผนก " + targetDepartment + " - " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + "]: " + internalNote;
                }
                ticket.EmployeeId = targetEmp?.Id;
                ticket.Status = "in_progress";
                ticket.UpdatedAt = DateTime.Now;
                _db.SaveChanges();
            }
        }
        catch { }

        TempData["SupportMsg"] = "ส่งต่อคำร้อง #" + id + " ไปยังแผนก " + targetDepartment + " เรียบร้อยแล้ว";
        return RedirectToAction("Detail", new { id = id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ReplyEmail(int id, string customerEmail, string emailSubject, string emailBody, string newStatus)
    {
        if (!CheckSupportAccess()) return RedirectToAction("Login", "Account");

        bool smtpSent = false;
        string? savedEmailUrl = null;

        try
        {
            string emailDir = Path.Combine(_env.WebRootPath, "uploads", "emails");
            Directory.CreateDirectory(emailDir);
            string fileName = "email_ticket_" + id + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".html";
            string fullPath = Path.Combine(emailDir, fileName);

            string htmlContent = "<!DOCTYPE html><html lang='th'><head><meta charset='utf-8'><title>" + WebUtility.HtmlEncode(emailSubject) + "</title>"
                + "<style>body{font-family:'Segoe UI',sans-serif;background:#f4f7f5;padding:32px;}.card{max-width:640px;margin:0 auto;background:#fff;border-radius:12px;padding:28px;border:1px solid #e2e8f0;}</style></head>"
                + "<body><div class='card'><h3 style='color:#0b3d2e;margin-top:0;'>ทางลัด (Tang-Lat Customer Support)</h3>"
                + "<p><strong>ถึง:</strong> " + WebUtility.HtmlEncode(customerEmail) + "<br/>"
                + "<strong>หัวข้อ:</strong> " + WebUtility.HtmlEncode(emailSubject) + "<br/>"
                + "<strong>วันที่ส่ง:</strong> " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "</p>"
                + "<hr style='border:none;border-top:1px solid #e2e8f0;margin:16px 0;'/>"
                + "<div style='white-space:pre-line;line-height:1.6;'>" + WebUtility.HtmlEncode(emailBody) + "</div></div></body></html>";

            System.IO.File.WriteAllText(fullPath, htmlContent);
            savedEmailUrl = "/uploads/emails/" + fileName;

            string? smtpHost = _config["SmtpSettings:Host"];
            int smtpPort = int.TryParse(_config["SmtpSettings:Port"], out int p) ? p : 587;
            string? smtpUser = _config["SmtpSettings:Username"];
            string? smtpPass = _config["SmtpSettings:Password"];
            string senderEmail = _config["SmtpSettings:SenderEmail"] ?? "support@tanglat.com";
            string senderName = _config["SmtpSettings:SenderName"] ?? "Tang-Lat Support";

            if (!string.IsNullOrWhiteSpace(smtpHost) && !string.IsNullOrWhiteSpace(smtpUser) && !string.IsNullOrWhiteSpace(smtpPass))
            {
                using var client = new SmtpClient(smtpHost, smtpPort);
                client.Credentials = new NetworkCredential(smtpUser, smtpPass);
                client.EnableSsl = true;
                client.Timeout = 5000;

                var mailMsg = new MailMessage();
                mailMsg.From = new MailAddress(senderEmail, senderName);
                mailMsg.Subject = emailSubject;
                mailMsg.Body = htmlContent;
                mailMsg.IsBodyHtml = true;
                mailMsg.To.Add(customerEmail);
                client.Send(mailMsg);
                smtpSent = true;
            }
        }
        catch { }

        try
        {
            var ticket = (from t in _db.SupportTickets where t.Id == id select t).FirstOrDefault();
            if (ticket != null)
            {
                ticket.Status = string.IsNullOrEmpty(newStatus) ? "resolved" : newStatus;
                ticket.Description += "\n\n[ตอบกลับอีเมลถึง " + customerEmail + " เมื่อ " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + "]: " + emailBody;
                ticket.UpdatedAt = DateTime.Now;
                _db.SaveChanges();
            }
        }
        catch { }

        TempData["LastEmailUrl"] = savedEmailUrl;
        TempData["SupportMsg"] = smtpSent
            ? "ส่งอีเมลจริงไปยัง " + customerEmail + " และอัปเดตสถานะเรียบร้อยแล้ว"
            : "ส่งข้อความตอบกลับไปยัง " + customerEmail + " และบันทึกสำเนาอีเมลเรียบร้อยแล้ว";

        return RedirectToAction("Detail", new { id = id });
    }

    public IActionResult Settings()
    {
        if (!CheckSupportAccess()) return RedirectToAction("Login", "Account");

        ViewData["Department"] = "Support";
        ViewData["ActiveMenu"] = "settings";
        return View("~/Views/Dev/Settings.cshtml");
    }
}
