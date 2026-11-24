using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
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

    // ตรวจสอบสิทธิ์การเข้าถึง: อนุญาตเฉพาะฝ่าย Marketing เท่านั้น
    private bool CheckMarketingAccess()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var role = HttpContext.Session.GetString("Role");
        var dept = HttpContext.Session.GetString("Department");
        return userId != null && role == "employee" && dept == "Marketing";
    }

    private void UpdateExpiredAds()
    {
        try
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var expiredList = (from a in _db.Advertisements
                               where a.Status == "active" && a.EndDate < today
                               select a).ToList();

            if (expiredList.Count > 0)
            {
                foreach (var item in expiredList)
                {
                    item.Status = "expired";
                    item.UpdatedAt = DateTime.Now;
                }
                _db.SaveChanges();
            }
        }
        catch { }
    }

    public IActionResult Dashboard()
    {
        if (!CheckMarketingAccess()) return RedirectToAction("Login", "Account");

        ViewData["Department"] = "Marketing";
        ViewData["ActiveMenu"] = "dashboard";

        UpdateExpiredAds();

        // ดึงข้อมูลจริงทั้งหมดจากฐานข้อมูล MySQL
        var ads = (from a in _db.Advertisements.Include(x => x.User) orderby a.CreatedAt descending select a).ToList();
        var plans = (from p in _db.SubscriptionPlans select p).ToList();
        var payments = (from p in _db.Payments.Include(x => x.SubscriptionPlan) select p).ToList();

        int activeAds = ads.Count(a => a.Status == "active");
        int pausedAds = ads.Count(a => a.Status == "paused");
        int expiredAds = ads.Count(a => a.Status == "expired");
        int rejectedAds = ads.Count(a => a.Status == "rejected");

        int totalViews = ads.Sum(a => a.ViewsCount ?? 0);
        int totalClicks = ads.Sum(a => a.ClicksCount ?? 0);
        double avgCtr = totalViews > 0 ? Math.Round((double)totalClicks / totalViews * 100.0, 2) : 0.0;

        ViewBag.ActiveAdsCount = activeAds;
        ViewBag.TotalAdsCount = ads.Count;
        ViewBag.TotalViews = totalViews;
        ViewBag.TotalClicks = totalClicks;
        ViewBag.AvgCtr = avgCtr;
        ViewBag.ActivePromotions = plans.Count(p => p.IsActive != false);

        // กราฟที่ 1: เปรียบเทียบยอดมองเห็น (Views) และยอดคลิก (Clicks) ของแต่ละแคมเปญโฆษณา
        ViewBag.AdLabelsJson = JsonSerializer.Serialize(ads.Select(a => a.Title.Length > 22 ? a.Title.Substring(0, 22) + "..." : a.Title).ToList());
        ViewBag.AdViewsJson = JsonSerializer.Serialize(ads.Select(a => a.ViewsCount ?? 0).ToList());
        ViewBag.AdClicksJson = JsonSerializer.Serialize(ads.Select(a => a.ClicksCount ?? 0).ToList());

        // กราฟที่ 2 (เพิ่มใหม่): สัดส่วนสถานะป้ายโฆษณาทั้งหมดในระบบ
        ViewBag.AdStatusJson = JsonSerializer.Serialize(new int[] { activeAds, pausedAds, expiredAds, rejectedAds });

        // กราฟที่ 3 (เพิ่มใหม่): จำนวนผู้สมัครและยอดซื้อแยกตามแพ็กเกจสมาชิก (ดึงจาก subscription_plans + payments)
        var planLabels = new List<string>();
        var planCounts = new List<int>();
        foreach (var pl in plans)
        {
            planLabels.Add(pl.Name.Length > 20 ? pl.Name.Substring(0, 20) + "..." : pl.Name);
            int count = payments.Count(pay => pay.SubscriptionPlanId == pl.Id);
            planCounts.Add(count);
        }
        ViewBag.PlanLabelsJson = JsonSerializer.Serialize(planLabels);
        ViewBag.PlanCountsJson = JsonSerializer.Serialize(planCounts);

        return View(ads);
    }

    public IActionResult Ads(string? search)
    {
        if (!CheckMarketingAccess()) return RedirectToAction("Login", "Account");

        ViewData["Department"] = "Marketing";
        ViewData["ActiveMenu"] = "ads";

        UpdateExpiredAds();

        var ads = (from a in _db.Advertisements.Include(x => x.User) orderby a.CreatedAt descending select a).ToList();
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
        if (!CheckMarketingAccess()) return RedirectToAction("Login", "Account");

        try
        {
            var ad = (from a in _db.Advertisements where a.Id == id select a).FirstOrDefault();
            if (ad != null)
            {
                ad.Status = status;
                // หากฝ่ายการตลาดกดเปิดแสดงผลโฆษณาที่เลยวันหมดอายุไปแล้ว ให้ต่ออายุให้อัตโนมัติ 15 วัน
                if (status == "active" && ad.EndDate < DateOnly.FromDateTime(DateTime.Today))
                {
                    ad.StartDate = DateOnly.FromDateTime(DateTime.Today);
                    ad.EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(15));
                }
                ad.UpdatedAt = DateTime.Now;
                _db.SaveChanges();
                TempData["MarketingMsg"] = "อัปเดตสถานะโฆษณา #" + id + " เรียบร้อยแล้ว";
            }
        }
        catch { }
        return RedirectToAction("Ads");
    }

    public IActionResult Promotions()
    {
        if (!CheckMarketingAccess()) return RedirectToAction("Login", "Account");

        ViewData["Department"] = "Marketing";
        ViewData["ActiveMenu"] = "promotions";

        var plans = (from p in _db.SubscriptionPlans select p).ToList();
        return View(plans);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult TogglePlan(int id)
    {
        if (!CheckMarketingAccess()) return RedirectToAction("Login", "Account");

        try
        {
            var plan = (from p in _db.SubscriptionPlans where p.Id == id select p).FirstOrDefault();
            if (plan != null)
            {
                plan.IsActive = !(plan.IsActive ?? true);
                plan.UpdatedAt = DateTime.Now;
                _db.SaveChanges();
                TempData["PromoMsg"] = "สลับสถานะแพ็กเกจ #" + id + " เรียบร้อยแล้ว";
            }
        }
        catch { }
        return RedirectToAction("Promotions");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddPromotion(string name, string description, decimal price, int durationDays)
    {
        if (!CheckMarketingAccess()) return RedirectToAction("Login", "Account");

        try
        {
            SubscriptionPlan plan = new();
            plan.Name = name;
            plan.Description = description;
            plan.Price = price;
            plan.DurationDays = durationDays;
            plan.IsActive = true;
            plan.CreatedAt = DateTime.Now;
            plan.UpdatedAt = DateTime.Now;

            _db.Add(plan);
            _db.SaveChanges();
            TempData["PromoMsg"] = "สร้างแพ็กเกจโปรโมชั่น '" + name + "' สำเร็จแล้ว";
        }
        catch { }
        return RedirectToAction("Promotions");
    }

    public IActionResult Issues(string? search)
    {
        if (!CheckMarketingAccess()) return RedirectToAction("Login", "Account");

        ViewData["Department"] = "Marketing";
        ViewData["ActiveMenu"] = "issues";

        var tickets = (from t in _db.SupportTickets.Include(x => x.Customer).Include(x => x.Employee)
                       where (t.Employee != null && t.Employee.Department == "Marketing") ||
                             t.Subject.Contains("[ส่งต่อแผนก Marketing]")
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
        if (!CheckMarketingAccess()) return RedirectToAction("Login", "Account");

        try
        {
            var t = (from x in _db.SupportTickets where x.Id == id select x).FirstOrDefault();
            if (t != null)
            {
                if (!string.IsNullOrWhiteSpace(resolutionNote))
                {
                    t.Description += "\n\n[ฝ่าย Marketing ดำเนินการแล้ว - " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + "]: " + resolutionNote;
                }
                t.Status = "resolved";
                t.UpdatedAt = DateTime.Now;
                _db.SaveChanges();
                TempData["MarketingIssueMsg"] = "ดำเนินการแก้ไขคำร้อง #" + id + " เสร็จสิ้นแล้ว";
            }
        }
        catch { }
        return RedirectToAction("Issues");
    }

    public IActionResult Settings()
    {
        if (!CheckMarketingAccess()) return RedirectToAction("Login", "Account");

        ViewData["Department"] = "Marketing";
        ViewData["ActiveMenu"] = "settings";
        return View("~/Views/Dev/Settings.cshtml");
    }
}
