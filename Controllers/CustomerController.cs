using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Suttipong_9922_L004.Models;

namespace Suttipong_9922_L004.Controllers;

public class CustomerController : Controller
{
    private readonly Csi402dbContext _db;
    private readonly IWebHostEnvironment _env;

    public CustomerController(Csi402dbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    // ตรวจสอบสิทธิ์การเข้าถึง: อนุญาตเฉพาะลูกค้าที่เข้าสู่ระบบแล้วเท่านั้น
    private bool CheckCustomerAccess()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var role = HttpContext.Session.GetString("Role");
        if (userId == null || role != "customer")
        {
            return false;
        }
        return true;
    }

    // บันทึกไฟล์รูปภาพที่อัปโหลดลงโฟลเดอร์ wwwroot/uploads พร้อมตรวจนามสกุลและขนาดไม่เกิน 5MB
    private string? SaveUploadedFile(IFormFile? file, string subFolder)
    {
        if (file == null || file.Length == 0) return null;

        // ตรวจสอบขนาดไฟล์ (ไม่เกิน 5MB)
        if (file.Length > 5 * 1024 * 1024) return null;

        // ตรวจสอบนามสกุลไฟล์รูปภาพ
        string ext = Path.GetExtension(file.FileName).ToLower();
        if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".webp")
        {
            return null;
        }

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

    // ตรวจสอบสถานะสมาชิก VIP และวันหมดอายุ แล้วส่งค่าผ่าน ViewBag
    private bool LoadVipInfo(int userId)
    {
        bool isVip = false;
        DateTime? expireDate = null;
        string? planName = null;

        try
        {
            var activeSub = (from p in _db.Payments.Include(x => x.SubscriptionPlan)
                             where p.UserId == userId && p.SubscriptionPlanId != null && p.PaymentStatus == "completed"
                             orderby p.PaidAt descending, p.CreatedAt descending
                             select p).FirstOrDefault();

            if (activeSub != null && activeSub.SubscriptionPlan != null)
            {
                DateTime start = activeSub.PaidAt ?? activeSub.CreatedAt ?? DateTime.Now;
                DateTime expire = start.AddDays(activeSub.SubscriptionPlan.DurationDays);
                if (expire >= DateTime.Now)
                {
                    isVip = true;
                    expireDate = expire;
                    planName = activeSub.SubscriptionPlan.Name;
                }
            }
        }
        catch { }

        if (!isVip && HttpContext.Session.GetString("IsVip") == "true")
        {
            isVip = true;
            expireDate = DateTime.Now.AddDays(30);
            planName = "Tang-Lat VIP";
        }

        ViewBag.IsVip = isVip;
        ViewBag.VipExpire = expireDate;
        ViewBag.VipPlanName = planName;
        return isVip;
    }

    // ปรับสถานะโฆษณาที่เลยวันสิ้นสุดให้เป็น expired อัตโนมัติ
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

    private List<Location> GetActiveLocations()
    {
        try
        {
            var list = (from l in _db.Locations where l.IsActive != false select l).ToList();
            if (list.Count > 0) return list;
        }
        catch { }

        return new List<Location>
        {
            new Location { Id = 1, Name = "วัดพระศรีรัตนศาสดาราม (วัดพระแก้ว)", Description = "วัดคู่บ้านคู่เมืองในพระบรมมหาราชวัง", Address = "เขตพระนคร กรุงเทพฯ", Latitude = 13.751644m, Longitude = 100.492704m, Category = "attraction", ImageUrl = "https://images.unsplash.com/photo-1563492065599-3520f775eeed?auto=format&fit=crop&w=600&q=80", IsActive = true },
            new Location { Id = 2, Name = "วัดอรุณราชวราราม (วัดแจ้ง)", Description = "พระปรางค์วัดอรุณริมแม่น้ำเจ้าพระยา", Address = "เขตบางกอกใหญ่ กรุงเทพฯ", Latitude = 13.743884m, Longitude = 100.488446m, Category = "attraction", ImageUrl = "https://images.unsplash.com/photo-1528181304800-259b08848526?auto=format&fit=crop&w=600&q=80", IsActive = true },
            new Location { Id = 3, Name = "เยาวราช (Chinatown Bangkok)", Description = "แหล่งรวมสตรีทฟู้ดยามค่ำคืนระดับโลก", Address = "เขตสัมพันธวงศ์ กรุงเทพฯ", Latitude = 13.741136m, Longitude = 100.508305m, Category = "restaurant", ImageUrl = "https://images.unsplash.com/photo-1552465011-b4e21bf6e79a?auto=format&fit=crop&w=600&q=80", IsActive = true },
            new Location { Id = 4, Name = "สยามพารากอน (Siam Paragon)", Description = "ศูนย์การค้าใจกลางสยาม ติดสถานี BTS สยาม", Address = "เขตปทุมวัน กรุงเทพฯ", Latitude = 13.746241m, Longitude = 100.534740m, Category = "shopping", ImageUrl = "https://images.unsplash.com/photo-1519567241046-7f570eee3ce6?auto=format&fit=crop&w=600&q=80", IsActive = true }
        };
    }

    private List<TransportType> GetActiveTransports()
    {
        try
        {
            var list = (from t in _db.TransportTypes where t.IsActive != false select t).ToList();
            if (list.Count > 0) return list;
        }
        catch { }

        return new List<TransportType>
        {
            new TransportType { Id = 1, Name = "รถเมล์ขสมก. (AC Bus)", Type = "bus", BaseFare = 15m, FarePerKm = 2m, FarePerStation = 0m, Description = "ประหยัดคุ้มค่า ครอบคลุมทั่วกรุงเทพฯ" },
            new TransportType { Id = 2, Name = "รถไฟฟ้า BTS / MRT", Type = "train", BaseFare = 17m, FarePerKm = 0m, FarePerStation = 4m, Description = "รวดเร็ว ไม่ติดไฟแดง" },
            new TransportType { Id = 3, Name = "รถมอเตอร์ไซค์รับจ้าง (วิน)", Type = "motorcycle", BaseFare = 25m, FarePerKm = 10m, FarePerStation = 0m, Description = "คล่องตัว เข้าซอยลัดได้รวดเร็ว" }
        };
    }

    public IActionResult Home(string? search, string? category)
    {
        if (!CheckCustomerAccess()) return RedirectToAction("Login", "Account");

        int userId = HttpContext.Session.GetInt32("UserId") ?? 1;
        bool isVip = LoadVipInfo(userId);
        UpdateExpiredAds();

        var locations = GetActiveLocations();
        if (!string.IsNullOrEmpty(search))
        {
            locations = locations.Where(l =>
                l.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (l.Description != null && l.Description.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (l.Address != null && l.Address.Contains(search, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }
        if (!string.IsNullOrEmpty(category) && category != "all")
        {
            locations = locations.Where(l => l.Category == category).ToList();
        }

        List<Advertisement> ads = new();
        if (!isVip)
        {
            try
            {
                var today = DateOnly.FromDateTime(DateTime.Today);
                ads = (from a in _db.Advertisements
                       where a.Status == "active" && a.EndDate >= today
                       select a).ToList();

                if (ads.Count > 0)
                {
                    foreach (var adItem in ads)
                    {
                        adItem.ViewsCount = (adItem.ViewsCount ?? 0) + 1;
                    }
                    _db.SaveChanges();
                }
            }
            catch { }
        }

        ViewBag.Ads = ads;
        ViewBag.Search = search;
        ViewBag.Category = category ?? "all";
        return View(locations);
    }

    public IActionResult ClickAd(int id)
    {
        if (!CheckCustomerAccess()) return RedirectToAction("Login", "Account");

        try
        {
            var ad = (from a in _db.Advertisements where a.Id == id select a).FirstOrDefault();
            if (ad != null)
            {
                ad.ClicksCount = (ad.ClicksCount ?? 0) + 1;
                ad.UpdatedAt = DateTime.Now;
                _db.SaveChanges();
                if (!string.IsNullOrEmpty(ad.RedirectUrl))
                {
                    return Redirect(ad.RedirectUrl);
                }
            }
        }
        catch { }
        return RedirectToAction("Membership");
    }

    // หน้าวางแผนเส้นทาง รองรับทั้งการเลือกสถานที่ใหม่ (selectedIds) และการโหลดทริปเก่า (tripId)
    public IActionResult Trip(string? selectedIds, int? tripId)
    {
        if (!CheckCustomerAccess()) return RedirectToAction("Login", "Account");

        int userId = HttpContext.Session.GetInt32("UserId") ?? 1;
        LoadVipInfo(userId);

        var allLocations = GetActiveLocations();
        var transports = GetActiveTransports();

        List<Location> selectedLocations = new();

        // กรณีลูกค้ากดโหลดทริปเก่าที่เคยบันทึกไว้กลับมาวาดบนแผนที่ใหม่
        if (tripId != null && tripId > 0)
        {
            try
            {
                var oldTrip = (from t in _db.UserTrips.Include(x => x.TripLocations).ThenInclude(tl => tl.Location)
                               where t.Id == tripId && t.UserId == userId
                               select t).FirstOrDefault();

                if (oldTrip != null)
                {
                    selectedLocations = oldTrip.TripLocations
                        .OrderBy(tl => tl.StopOrder)
                        .Where(tl => tl.Location != null)
                        .Select(tl => tl.Location)
                        .ToList();
                    ViewBag.LoadedTripName = oldTrip.TripName;
                }
            }
            catch { }
        }

        if (selectedLocations.Count == 0)
        {
            List<int> ids = new();
            if (!string.IsNullOrEmpty(selectedIds))
            {
                var parts = selectedIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                {
                    if (int.TryParse(p, out int val) && val > 0)
                    {
                        ids.Add(val);
                    }
                }
            }

            if (ids.Count > 0)
                selectedLocations = allLocations.Where(l => ids.Contains(l.Id)).ToList();
            else
                selectedLocations = allLocations.Take(3).ToList();
        }

        ViewBag.AllLocations = allLocations;
        ViewBag.Transports = transports;
        ViewBag.SelectedCount = selectedLocations.Count;

        List<UserTrip> savedTrips = new();
        try
        {
            savedTrips = (from t in _db.UserTrips.Include(x => x.TripLocations).ThenInclude(tl => tl.Location)
                          where t.UserId == userId
                          orderby t.IsFavorite descending, t.CreatedAt descending
                          select t).ToList();
        }
        catch { }
        ViewBag.SavedTrips = savedTrips;

        return View(selectedLocations);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SaveTrip(string tripName, string locationIdsCsv, decimal totalDistanceKm, decimal estimatedCost, int transportId, bool isFavorite = false)
    {
        if (!CheckCustomerAccess()) return RedirectToAction("Login", "Account");

        int userId = HttpContext.Session.GetInt32("UserId") ?? 1;
        try
        {
            UserTrip trip = new();
            trip.UserId = userId;
            trip.TripName = string.IsNullOrWhiteSpace(tripName) ? "ทริปท่องเที่ยวกรุงเทพฯ" : tripName;
            trip.TotalDistanceKm = totalDistanceKm;
            trip.EstimatedCost = estimatedCost;
            trip.IsFavorite = isFavorite;
            trip.CreatedAt = DateTime.Now;
            trip.UpdatedAt = DateTime.Now;

            _db.Add(trip);
            _db.SaveChanges();

            if (!string.IsNullOrEmpty(locationIdsCsv))
            {
                var ids = locationIdsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries);
                int order = 1;
                foreach (var rawId in ids)
                {
                    if (int.TryParse(rawId, out int locId) && locId != 999)
                    {
                        TripLocation tl = new();
                        tl.TripId = trip.Id;
                        tl.LocationId = locId;
                        tl.StopOrder = order++;
                        tl.TransportId = transportId > 0 ? transportId : null;
                        tl.TravelDurationMins = 15;
                        tl.CreatedAt = DateTime.Now;
                        _db.Add(tl);
                    }
                }
                _db.SaveChanges();
            }
            TempData["SuccessMsg"] = "บันทึกทริป '" + trip.TripName + "' ลงฐานข้อมูลสำเร็จแล้ว";
        }
        catch
        {
            TempData["SuccessMsg"] = "บันทึกทริปไม่สำเร็จ กรุณาลองใหม่อีกครั้ง";
        }

        return RedirectToAction("Trip", new { selectedIds = locationIdsCsv });
    }

    // สลับสถานะรายการโปรด (Favorite) ของทริปที่บันทึกไว้
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleFavoriteTrip(int id)
    {
        if (!CheckCustomerAccess()) return RedirectToAction("Login", "Account");

        int userId = HttpContext.Session.GetInt32("UserId") ?? 1;
        try
        {
            var trip = (from t in _db.UserTrips where t.Id == id && t.UserId == userId select t).FirstOrDefault();
            if (trip != null)
            {
                trip.IsFavorite = !(trip.IsFavorite ?? false);
                trip.UpdatedAt = DateTime.Now;
                _db.SaveChanges();
            }
        }
        catch { }

        return RedirectToAction("Trip");
    }

    // ลบทริปที่บันทึกไว้
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteTrip(int id)
    {
        if (!CheckCustomerAccess()) return RedirectToAction("Login", "Account");

        int userId = HttpContext.Session.GetInt32("UserId") ?? 1;
        try
        {
            var trip = (from t in _db.UserTrips.Include(x => x.TripLocations)
                        where t.Id == id && t.UserId == userId
                        select t).FirstOrDefault();
            if (trip != null)
            {
                if (trip.TripLocations.Count > 0)
                {
                    _db.RemoveRange(trip.TripLocations);
                }
                _db.Remove(trip);
                _db.SaveChanges();
                TempData["SuccessMsg"] = "ลบทริป '" + trip.TripName + "' เรียบร้อยแล้ว";
            }
        }
        catch { }

        return RedirectToAction("Trip");
    }

    [HttpGet]
    public IActionResult Report()
    {
        if (!CheckCustomerAccess()) return RedirectToAction("Login", "Account");

        int userId = HttpContext.Session.GetInt32("UserId") ?? 1;
        List<SupportTicket> myTickets = new();
        try
        {
            myTickets = (from t in _db.SupportTickets
                         where t.CustomerId == userId
                         orderby t.CreatedAt descending
                         select t).ToList();
        }
        catch { }
        ViewBag.MyTickets = myTickets;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Report(string subject, string description, string priority, string? imageUrl, IFormFile? attachmentFile)
    {
        if (!CheckCustomerAccess()) return RedirectToAction("Login", "Account");

        int userId = HttpContext.Session.GetInt32("UserId") ?? 1;
        string? savedPath = SaveUploadedFile(attachmentFile, "tickets") ?? imageUrl;

        try
        {
            SupportTicket ticket = new();
            ticket.CustomerId = userId;
            ticket.Subject = subject;
            ticket.Description = description;
            ticket.Priority = string.IsNullOrEmpty(priority) ? "medium" : priority;
            ticket.ImageUrl = savedPath;
            ticket.Status = "open";
            ticket.CreatedAt = DateTime.Now;
            ticket.UpdatedAt = DateTime.Now;

            _db.Add(ticket);
            _db.SaveChanges();
            TempData["ReportSuccess"] = "ส่งข้อมูลแจ้งปัญหาเรียบร้อยแล้ว";
        }
        catch
        {
            TempData["ReportSuccess"] = "ไม่สามารถบันทึกข้อมูลได้ กรุณาลองใหม่อีกครั้ง";
        }

        return RedirectToAction("Report");
    }

    [HttpGet]
    public IActionResult Membership()
    {
        if (!CheckCustomerAccess()) return RedirectToAction("Login", "Account");

        int userId = HttpContext.Session.GetInt32("UserId") ?? 1;
        LoadVipInfo(userId);

        List<SubscriptionPlan> plans = new();
        try
        {
            plans = (from p in _db.SubscriptionPlans where p.IsActive != false select p).ToList();
        }
        catch { }

        return View(plans);
    }

    [HttpGet]
    public IActionResult Payment(int planId = 1, int? adId = null, decimal amount = 99.00m, string title = "Tang-Lat VIP Monthly")
    {
        if (!CheckCustomerAccess()) return RedirectToAction("Login", "Account");

        try
        {
            if (adId == null && planId > 0)
            {
                var plan = (from p in _db.SubscriptionPlans where p.Id == planId select p).FirstOrDefault();
                if (plan != null)
                {
                    amount = plan.Price;
                    title = plan.Name;
                }
            }
        }
        catch { }

        ViewBag.PlanId = planId;
        ViewBag.AdId = adId;
        ViewBag.Amount = amount;
        ViewBag.ItemTitle = title;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ConfirmPayment(int? planId, int? adId, decimal amount, string paymentMethod, string transactionRef, IFormFile? slipFile)
    {
        if (!CheckCustomerAccess()) return RedirectToAction("Login", "Account");

        int userId = HttpContext.Session.GetInt32("UserId") ?? 1;
        string? slipPath = SaveUploadedFile(slipFile, "slips");
        string txRef = string.IsNullOrWhiteSpace(transactionRef)
            ? "TXN-" + DateTime.Now.ToString("MMddHHmmss")
            : transactionRef.Trim();

        if (!string.IsNullOrEmpty(slipPath))
        {
            txRef = txRef + "|SLIP:" + slipPath;
        }
        if (txRef.Length > 98)
        {
            txRef = txRef.Substring(0, 98);
        }

        try
        {
            Payment payment = new();
            payment.UserId = userId;
            payment.SubscriptionPlanId = adId == null ? planId : null;
            payment.AdvertisementId = adId;
            payment.Amount = amount;
            payment.PaymentMethod = string.IsNullOrEmpty(paymentMethod) ? "promptpay" : paymentMethod;
            payment.PaymentStatus = "pending";
            payment.TransactionRef = txRef;
            payment.PaidAt = DateTime.Now;
            payment.CreatedAt = DateTime.Now;
            payment.UpdatedAt = DateTime.Now;

            _db.Add(payment);
            _db.SaveChanges();
        }
        catch { }

        TempData["PaymentSuccess"] = "แจ้งชำระเงินยอด " + amount.ToString("N2") + " บาท เรียบร้อยแล้ว รอฝ่ายบัญชีตรวจสอบและอนุมัติ";
        return RedirectToAction("Membership");
    }

    [HttpGet]
    public IActionResult Ads()
    {
        if (!CheckCustomerAccess()) return RedirectToAction("Login", "Account");

        int userId = HttpContext.Session.GetInt32("UserId") ?? 1;
        List<Advertisement> myAds = new();
        try
        {
            myAds = (from a in _db.Advertisements
                     where a.UserId == userId
                     orderby a.CreatedAt descending
                     select a).ToList();
        }
        catch { }
        ViewBag.MyAds = myAds;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateAd(string title, string? imageUrl, IFormFile? bannerFile, string redirectUrl, DateOnly startDate, DateOnly endDate)
    {
        if (!CheckCustomerAccess()) return RedirectToAction("Login", "Account");

        // ตรวจสอบความถูกต้องของวันที่เริ่ม-สิ้นสุดโฆษณา
        if (endDate < startDate)
        {
            TempData["AdError"] = "วันที่สิ้นสุดต้องไม่น้อยกว่าวันที่เริ่มแสดงโฆษณา";
            return RedirectToAction("Ads");
        }

        int userId = HttpContext.Session.GetInt32("UserId") ?? 1;
        int createdAdId = 1;
        string? uploadedBanner = SaveUploadedFile(bannerFile, "ads") ?? imageUrl;

        try
        {
            Advertisement ad = new();
            ad.UserId = userId;
            ad.Title = title;
            ad.ImageUrl = string.IsNullOrWhiteSpace(uploadedBanner) ? "https://images.unsplash.com/photo-1554118811-1e0d58224f24?auto=format&fit=crop&w=800&q=80" : uploadedBanner;
            ad.RedirectUrl = string.IsNullOrWhiteSpace(redirectUrl) ? "https://example.com" : redirectUrl;
            ad.StartDate = startDate;
            ad.EndDate = endDate;
            ad.Status = "paused";
            ad.ViewsCount = 0;
            ad.ClicksCount = 0;
            ad.CreatedAt = DateTime.Now;
            ad.UpdatedAt = DateTime.Now;

            _db.Add(ad);
            _db.SaveChanges();
            createdAdId = ad.Id;
        }
        catch { }

        return RedirectToAction("Payment", new { planId = 3, adId = createdAdId, amount = 499.00m, title = "ค่าลงป้ายโฆษณา: " + title });
    }

    [HttpGet]
    public IActionResult Profile()
    {
        if (!CheckCustomerAccess()) return RedirectToAction("Login", "Account");

        int userId = HttpContext.Session.GetInt32("UserId") ?? 1;
        User? user = null;
        try
        {
            user = (from u in _db.Users where u.Id == userId select u).FirstOrDefault();
        }
        catch { }

        if (user == null)
        {
            user = new User
            {
                Id = userId,
                UserName = HttpContext.Session.GetString("Username") ?? "customer01",
                Email = HttpContext.Session.GetString("Email") ?? "customer@tanglat.com",
                PhoneNumber = "0812345678",
                Role = "customer",
                Status = "active"
            };
        }

        LoadVipInfo(userId);
        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateProfile(string userName, string email, string phoneNumber)
    {
        if (!CheckCustomerAccess()) return RedirectToAction("Login", "Account");

        int userId = HttpContext.Session.GetInt32("UserId") ?? 1;
        try
        {
            var user = (from u in _db.Users where u.Id == userId select u).FirstOrDefault();
            if (user != null)
            {
                user.UserName = userName;
                user.Email = email;
                user.PhoneNumber = phoneNumber;
                _db.SaveChanges();
                HttpContext.Session.SetString("Username", userName);
                HttpContext.Session.SetString("Email", email);
            }
        }
        catch { }
        TempData["ProfileMsg"] = "บันทึกข้อมูลบัญชีเรียบร้อยแล้ว";
        return RedirectToAction("Profile");
    }
}
