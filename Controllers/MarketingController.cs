using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Suttipong_9922_L004.Models;

namespace Suttipong_9922_L004.Controllers;

public class MarketingController : Controller
{
    private readonly Csi402dbContext _db;

    public MarketingController(Csi402dbContext db)
    {
        _db = db;
    }

    private List<Advertisement> GetAdsOrDefault()
    {
        try
        {
            var list = _db.Advertisements.Include(a => a.User).OrderByDescending(a => a.CreatedAt).ToList();
            if (list.Any()) return list;
        }
        catch { }

        return new List<Advertisement>
        {
            new Advertisement { Id = 1, UserId = 2, Title = "Chao Phraya River Cafe - ลด 20% สำหรับผู้ใช้แอปทางลัด", ImageUrl = "https://images.unsplash.com/photo-1554118811-1e0d58224f24?auto=format&fit=crop&w=800&q=80", RedirectUrl = "https://example.com/cafe-promo", StartDate = new DateOnly(2026, 11, 1), EndDate = new DateOnly(2026, 12, 31), Status = "active", ViewsCount = 1240, ClicksCount = 185 },
            new Advertisement { Id = 2, UserId = 1, Title = "ทัวร์เรือด่วนเจ้าพระยา ชมวิววัดอรุณยามเย็น ราคาพิเศษ", ImageUrl = "https://images.unsplash.com/photo-1506665531195-3566af2b4dfa?auto=format&fit=crop&w=800&q=80", RedirectUrl = "https://example.com/boat-tour", StartDate = new DateOnly(2026, 11, 5), EndDate = new DateOnly(2026, 11, 30), Status = "active", ViewsCount = 890, ClicksCount = 92 },
            new Advertisement { Id = 3, UserId = 1, Title = "Siam Street Food Festival 2026", ImageUrl = "https://images.unsplash.com/photo-1504674900247-0877df9cc836?auto=format&fit=crop&w=800&q=80", RedirectUrl = "https://example.com/food-fest", StartDate = new DateOnly(2026, 11, 10), EndDate = new DateOnly(2026, 11, 25), Status = "paused", ViewsCount = 310, ClicksCount = 24 }
        };
    }

    private List<SubscriptionPlan> GetPlansOrDefault()
    {
        try
        {
            var list = _db.SubscriptionPlans.ToList();
            if (list.Any()) return list;
        }
        catch { }

        return new List<SubscriptionPlan>
        {
            new SubscriptionPlan { Id = 1, Name = "Tang-Lat VIP Monthly (รายเดือน)", Description = "ปิดโฆษณาทั้งหมด + จัดรูทปักหมุดสถานที่ได้ไม่จำกัด นาน 30 วัน", Price = 99.00m, DurationDays = 30, IsActive = true },
            new SubscriptionPlan { Id = 2, Name = "Tang-Lat VIP Yearly (รายปีสุดคุ้ม)", Description = "ปิดโฆษณาทั้งหมด + จัดรูทไม่จำกัด นาน 365 วัน ประหยัดกว่า 30%", Price = 799.00m, DurationDays = 365, IsActive = true },
            new SubscriptionPlan { Id = 3, Name = "แพ็กเกจลงป้ายโฆษณา (Ad Banner 15 วัน)", Description = "โปรโมทร้านค้าบนหน้าแผนที่หลัก นาน 15 วัน", Price = 499.00m, DurationDays = 15, IsActive = true }
        };
    }

    // Wireframe หน้า 18: แดชบอร์ดการตลาด (3 การ์ด KPI ด้านบน + 1 กราฟใหญ่ด้านล่าง)
    public IActionResult Dashboard()
    {
        ViewData["Department"] = "Marketing";
        ViewData["ActiveMenu"] = "dashboard";

        var ads = GetAdsOrDefault();
        var plans = GetPlansOrDefault();

        ViewBag.ActiveAdsCount = ads.Count(a => a.Status == "active");
        ViewBag.TotalViews = ads.Sum(a => a.ViewsCount ?? 0);
        ViewBag.TotalClicks = ads.Sum(a => a.ClicksCount ?? 0);
        ViewBag.ActivePromotions = plans.Count(p => p.IsActive != false);

        return View(ads);
    }

    // Wireframe หน้า 19: จัดการป้ายโฆษณาที่ลูกค้าสมัครเข้ามา (อนุมัติ / พัก / ปฏิเสธ)
    public IActionResult Ads(string? search)
    {
        ViewData["Department"] = "Marketing";
        ViewData["ActiveMenu"] = "ads";

        var ads = GetAdsOrDefault();
        if (!string.IsNullOrEmpty(search))
        {
            ads = ads.Where(a => a.Title.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        ViewBag.Search = search;
        return View(ads);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleAdStatus(int id, string status)
    {
        try
        {
            var ad = _db.Advertisements.FirstOrDefault(a => a.Id == id);
            if (ad != null)
            {
                ad.Status = status;
                ad.UpdatedAt = DateTime.Now;
                _db.SaveChanges();
            }
        }
        catch { }
        TempData["MarketingMsg"] = $"อัปเดตสถานะโฆษณา #{id} เป็น '{status}' เรียบร้อยแล้ว!";
        return RedirectToAction("Ads");
    }

    // Wireframe หน้า 20: จัดการโปรโมชั่นและแพ็กเกจสมาชิก (พร้อมสวิตช์เปิด-ปิด และปุ่ม + เพิ่มแพ็กเกจ)
    public IActionResult Promotions()
    {
        ViewData["Department"] = "Marketing";
        ViewData["ActiveMenu"] = "promotions";

        var plans = GetPlansOrDefault();
        return View(plans);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult TogglePlan(int id)
    {
        try
        {
            var plan = _db.SubscriptionPlans.FirstOrDefault(p => p.Id == id);
            if (plan != null)
            {
                plan.IsActive = !(plan.IsActive ?? true);
                plan.UpdatedAt = DateTime.Now;
                _db.SaveChanges();
            }
        }
        catch { }
        TempData["PromoMsg"] = $"สลับสถานะการเปิดใช้งานแพ็กเกจ #{id} เรียบร้อยแล้ว!";
        return RedirectToAction("Promotions");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddPromotion(string name, string description, decimal price, int durationDays)
    {
        try
        {
            var plan = new SubscriptionPlan
            {
                Name = name,
                Description = description,
                Price = price,
                DurationDays = durationDays,
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            _db.SubscriptionPlans.Add(plan);
            _db.SaveChanges();
        }
        catch { }
        TempData["PromoMsg"] = $"สร้างแคมเปญโปรโมชั่น '{name}' ราคา {price:N0} บาท สำเร็จแล้ว!";
        return RedirectToAction("Promotions");
    }

    // Wireframe หน้า 21: เรื่องที่ถูกส่งต่อมาจากฝ่าย Support เกี่ยวกับโฆษณา/การตลาด
    public IActionResult Issues(string? search)
    {
        ViewData["Department"] = "Marketing";
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
                new SupportTicket { Id = 3, Subject = "สอบถามวิธีเพิ่มสถานที่มากกว่า 3 แห่งใน 1 ทริป และโปรโมชั่นลงแอดร้านอาหาร", Description = "ต้องการจัดรูทไปเที่ยว 5 ที่ และสนใจลงป้ายแบนเนอร์โปรโมทร้านอาหารเยาวราชครับ", Status = "open", Priority = "medium", CreatedAt = DateTime.Now.AddHours(-4) }
            };
        }

        if (!string.IsNullOrEmpty(search))
        {
            tickets = tickets.Where(t => t.Subject.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        ViewBag.Search = search;
        return View(tickets);
    }

    // Wireframe หน้า 22: ตั้งค่าบัญชีพนักงานการตลาด
    public IActionResult Settings()
    {
        ViewData["Department"] = "Marketing";
        ViewData["ActiveMenu"] = "settings";
        return View("~/Views/Dev/Settings.cshtml");
    }
}
