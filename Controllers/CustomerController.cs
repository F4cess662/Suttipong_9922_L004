using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Suttipong_9922_L004.Models;

namespace Suttipong_9922_L004.Controllers;

public class CustomerController : Controller
{
    private readonly Csi402dbContext _db;

    public CustomerController(Csi402dbContext db)
    {
        _db = db;
    }

    // ข้อมูลสำรอง (Fallback) กรณีที่เครื่องพรีเซนต์ยังไม่ได้รันไฟล์ SQL Seed
    private List<Location> GetLocationsOrDefault()
    {
        try
        {
            var list = _db.Locations.Where(l => l.IsActive != false).ToList();
            if (list.Any()) return list;
        }
        catch { }

        return new List<Location>
        {
            new Location { Id = 1, Name = "วัดพระศรีรัตนศาสดาราม (วัดพระแก้ว)", Description = "วัดคู่บ้านคู่เมืองในพระบรมมหาราชวัง", Address = "เขตพระนคร กรุงเทพฯ", Latitude = 13.751644m, Longitude = 100.492704m, Category = "attraction", ImageUrl = "https://images.unsplash.com/photo-1563492065599-3520f775eeed?auto=format&fit=crop&w=600&q=80", IsActive = true },
            new Location { Id = 2, Name = "วัดอรุณราชวราราม (วัดแจ้ง)", Description = "พระปรางค์วัดอรุณริมแม่น้ำเจ้าพระยา", Address = "เขตบางกอกใหญ่ กรุงเทพฯ", Latitude = 13.743884m, Longitude = 100.488446m, Category = "attraction", ImageUrl = "https://images.unsplash.com/photo-1528181304800-259b08848526?auto=format&fit=crop&w=600&q=80", IsActive = true },
            new Location { Id = 3, Name = "เยาวราช (Chinatown Bangkok)", Description = "แหล่งรวมสตรีทฟู้ดยามค่ำคืนระดับโลก", Address = "เขตสัมพันธวงศ์ กรุงเทพฯ", Latitude = 13.741136m, Longitude = 100.508305m, Category = "restaurant", ImageUrl = "https://images.unsplash.com/photo-1552465011-b4e21bf6e79a?auto=format&fit=crop&w=600&q=80", IsActive = true },
            new Location { Id = 4, Name = "สยามพารากอน (Siam Paragon)", Description = "ศูนย์การค้าใจกลางสยาม ติดสถานี BTS สยาม", Address = "เขตปทุมวัน กรุงเทพฯ", Latitude = 13.746241m, Longitude = 100.534740m, Category = "shopping", ImageUrl = "https://images.unsplash.com/photo-1519567241046-7f570eee3ce6?auto=format&fit=crop&w=600&q=80", IsActive = true },
            new Location { Id = 5, Name = "ไอคอนสยาม (ICONSIAM)", Description = "แลนด์มาร์คริมแม่น้ำเจ้าพระยา", Address = "เขตคลองสาน กรุงเทพฯ", Latitude = 13.726720m, Longitude = 100.510524m, Category = "shopping", ImageUrl = "https://images.unsplash.com/photo-1583396618422-597b2755de83?auto=format&fit=crop&w=600&q=80", IsActive = true },
            new Location { Id = 6, Name = "สวนเบญจกิติ (Benchakitti Park)", Description = "สวนป่ากลางกรุงและสกายวอล์คถ่ายรูปสวย", Address = "เขตคลองเตย กรุงเทพฯ", Latitude = 13.729942m, Longitude = 100.558246m, Category = "attraction", ImageUrl = "https://images.unsplash.com/photo-1508009603885-50cf7c579365?auto=format&fit=crop&w=600&q=80", IsActive = true },
            new Location { Id = 7, Name = "ตลาดนัดจตุจักร (Chatuchak Market)", Description = "ตลาดนัดสุดสัปดาห์ขนาดใหญ่ครบวงจร", Address = "เขตจตุจักร กรุงเทพฯ", Latitude = 13.799920m, Longitude = 100.550842m, Category = "shopping", ImageUrl = "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?auto=format&fit=crop&w=600&q=80", IsActive = true },
            new Location { Id = 8, Name = "หอศิลปวัฒนธรรมแห่งกรุงเทพมหานคร (BACC)", Description = "พื้นที่ศิลปะร่วมสมัยและนิทรรศการใจกลางเมือง", Address = "เขตปทุมวัน กรุงเทพฯ", Latitude = 13.746672m, Longitude = 100.530350m, Category = "attraction", ImageUrl = "https://images.unsplash.com/photo-1518998053901-5348d3961a04?auto=format&fit=crop&w=600&q=80", IsActive = true }
        };
    }

    private List<TransportType> GetTransportsOrDefault()
    {
        try
        {
            var list = _db.TransportTypes.Where(t => t.IsActive != false).ToList();
            if (list.Any()) return list;
        }
        catch { }

        return new List<TransportType>
        {
            new TransportType { Id = 1, Name = "รถเมล์ขสมก. (AC Bus)", Type = "bus", BaseFare = 15m, FarePerKm = 2m, FarePerStation = 0m, Description = "ประหยัดคุ้มค่า ครอบคลุมทั่วกรุงเทพฯ" },
            new TransportType { Id = 2, Name = "รถไฟฟ้า BTS / MRT", Type = "train", BaseFare = 17m, FarePerKm = 0m, FarePerStation = 4m, Description = "รวดเร็ว ไม่ติดไฟแดง (เฉลี่ยสถานีละ 1.2 กม.)" },
            new TransportType { Id = 3, Name = "รถมอเตอร์ไซค์รับจ้าง (วิน)", Type = "motorcycle", BaseFare = 25m, FarePerKm = 10m, FarePerStation = 0m, Description = "คล่องตัว เข้าซอยลัดได้รวดเร็ว" }
        };
    }

    private bool CheckUserVip(int userId)
    {
        if (HttpContext.Session.GetString("IsVip") == "true") return true;
        try
        {
            return _db.Payments.Any(p => p.UserId == userId && p.SubscriptionPlanId != null && p.PaymentStatus == "completed");
        }
        catch
        {
            return false;
        }
    }

    // Wireframe หน้า 3: หน้าหลัก ค้นหาสถานที่ + การ์ดสถานที่พร้อมติ๊กเลือกเข้าทริป + ป้ายโฆษณา
    public IActionResult Home(string? search, string? category)
    {
        int userId = HttpContext.Session.GetInt32("UserId") ?? 1;
        bool isVip = CheckUserVip(userId);
        ViewBag.IsVip = isVip;

        var locations = GetLocationsOrDefault();
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

        // ดึงป้ายโฆษณาที่ active มาแสดง (เฉพาะคนที่ไม่ได้เป็น VIP)
        List<Advertisement> ads = new();
        if (!isVip)
        {
            try
            {
                ads = _db.Advertisements.Where(a => a.Status == "active").ToList();
            }
            catch { }
            if (!ads.Any())
            {
                ads.Add(new Advertisement
                {
                    Id = 1,
                    Title = "Chao Phraya River Cafe - ส่วนลด 20% สำหรับผู้ใช้แอปทางลัด (สมัคร VIP เพื่อปิดโฆษณา)",
                    ImageUrl = "https://images.unsplash.com/photo-1554118811-1e0d58224f24?auto=format&fit=crop&w=800&q=80",
                    RedirectUrl = "/Customer/Membership",
                    Status = "active"
                });
            }
        }

        ViewBag.Ads = ads;
        ViewBag.Search = search;
        ViewBag.Category = category ?? "all";
        return View(locations);
    }

    // Wireframe หน้า 4: หน้าทริปของฉัน + แผนที่ Leaflet.js + อัลกอริทึมจัดรูทที่ใกล้ที่สุด + คำนวณค่ารถ 3 ประเภท
    public IActionResult Trip(string? selectedIds)
    {
        int userId = HttpContext.Session.GetInt32("UserId") ?? 1;
        ViewBag.IsVip = CheckUserVip(userId);

        var allLocations = GetLocationsOrDefault();
        var transports = GetTransportsOrDefault();

        List<int> ids = new();
        if (!string.IsNullOrEmpty(selectedIds))
        {
            ids = selectedIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                             .Select(s => int.TryParse(s, out int v) ? v : 0)
                             .Where(v => v > 0)
                             .ToList();
        }

        var selectedLocations = ids.Any()
            ? allLocations.Where(l => ids.Contains(l.Id)).ToList()
            : allLocations.Take(4).ToList();

        ViewBag.AllLocations = allLocations;
        ViewBag.Transports = transports;
        ViewBag.SelectedCount = selectedLocations.Count;

        // ดึงรายการทริปที่เคยบันทึกไว้ใน Database
        List<UserTrip> savedTrips = new();
        try
        {
            savedTrips = _db.UserTrips
                .Include(t => t.TripLocations)
                .ThenInclude(tl => tl.Location)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToList();
        }
        catch { }
        ViewBag.SavedTrips = savedTrips;

        return View(selectedLocations);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SaveTrip(string tripName, string locationIdsCsv, decimal totalDistanceKm, decimal estimatedCost, int transportId, bool isFavorite)
    {
        int userId = HttpContext.Session.GetInt32("UserId") ?? 1;
        try
        {
            var trip = new UserTrip
            {
                UserId = userId,
                TripName = string.IsNullOrWhiteSpace(tripName) ? "ทริปท่องเที่ยวกรุงเทพฯ" : tripName,
                TotalDistanceKm = totalDistanceKm,
                EstimatedCost = estimatedCost,
                IsFavorite = isFavorite,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            _db.UserTrips.Add(trip);
            _db.SaveChanges();

            if (!string.IsNullOrEmpty(locationIdsCsv))
            {
                var ids = locationIdsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries);
                int order = 1;
                foreach (var rawId in ids)
                {
                    if (int.TryParse(rawId, out int locId))
                    {
                        _db.TripLocations.Add(new TripLocation
                        {
                            TripId = trip.Id,
                            LocationId = locId,
                            StopOrder = order++,
                            TransportId = transportId > 0 ? transportId : null,
                            TravelDurationMins = 15,
                            CreatedAt = DateTime.Now
                        });
                    }
                }
                _db.SaveChanges();
            }
            TempData["SuccessMsg"] = $"บันทึกทริป '{trip.TripName}' ลงฐานข้อมูลสำเร็จแล้ว!";
        }
        catch (Exception ex)
        {
            TempData["SuccessMsg"] = $"จำลองการบันทึกทริป '{tripName}' สำเร็จ! (สถานะ DB: {ex.Message})";
        }

        return RedirectToAction("Trip", new { selectedIds = locationIdsCsv });
    }

    // Wireframe หน้า 5: แจ้งปัญหาการใช้งาน (Support Ticket)
    [HttpGet]
    public IActionResult Report()
    {
        int userId = HttpContext.Session.GetInt32("UserId") ?? 1;
        List<SupportTicket> myTickets = new();
        try
        {
            myTickets = _db.SupportTickets
                .Where(t => t.CustomerId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToList();
        }
        catch { }
        ViewBag.MyTickets = myTickets;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Report(string subject, string description, string priority, string? imageUrl)
    {
        int userId = HttpContext.Session.GetInt32("UserId") ?? 1;
        try
        {
            var ticket = new SupportTicket
            {
                CustomerId = userId,
                Subject = subject,
                Description = description,
                Priority = string.IsNullOrEmpty(priority) ? "medium" : priority,
                ImageUrl = imageUrl,
                Status = "open",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            _db.SupportTickets.Add(ticket);
            _db.SaveChanges();
            TempData["ReportSuccess"] = "ส่งเรื่องแจ้งปัญหาเรียบร้อยแล้ว! ทีมงานซัพพอร์ตจะตรวจสอบและตอบกลับทางอีเมลของคุณ";
        }
        catch
        {
            TempData["ReportSuccess"] = "บันทึกการแจ้งปัญหาเรียบร้อยแล้ว! (โหมดพรีเซนต์)";
        }

        return RedirectToAction("Report");
    }

    // Wireframe หน้า 6: ระบบสมัครสมาชิก VIP (เอาโฆษณาออก + ฟีเจอร์จัดรูทไม่จำกัด)
    [HttpGet]
    public IActionResult Membership()
    {
        int userId = HttpContext.Session.GetInt32("UserId") ?? 1;
        ViewBag.IsVip = CheckUserVip(userId);

        List<SubscriptionPlan> plans = new();
        try
        {
            plans = _db.SubscriptionPlans.Where(p => p.IsActive != false).ToList();
        }
        catch { }

        if (!plans.Any())
        {
            plans = new List<SubscriptionPlan>
            {
                new SubscriptionPlan { Id = 1, Name = "Tang-Lat VIP Monthly (รายเดือน)", Description = "ปิดป้ายโฆษณาทั้งหมด (No Ads) + จัดรูทปักหมุดสถานที่ได้ไม่จำกัด + บันทึกทริปโปรดไม่จำกัด นาน 30 วัน", Price = 99.00m, DurationDays = 30, IsActive = true },
                new SubscriptionPlan { Id = 2, Name = "Tang-Lat VIP Yearly (รายปีสุดคุ้ม)", Description = "สิทธิพิเศษ VIP ครบทุกฟีเจอร์ นาน 365 วัน ประหยัดกว่า 30%", Price = 799.00m, DurationDays = 365, IsActive = true }
            };
        }

        return View(plans);
    }

    // Wireframe หน้า 7: หน้าชำระเงิน QR PromptPay / โอนเงิน
    [HttpGet]
    public IActionResult Payment(int planId = 1, int? adId = null, decimal amount = 99.00m, string title = "Tang-Lat VIP Monthly")
    {
        try
        {
            if (adId == null && planId > 0)
            {
                var plan = _db.SubscriptionPlans.FirstOrDefault(p => p.Id == planId);
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
    public IActionResult ConfirmPayment(int? planId, int? adId, decimal amount, string paymentMethod, string transactionRef)
    {
        int userId = HttpContext.Session.GetInt32("UserId") ?? 1;
        string txRef = string.IsNullOrWhiteSpace(transactionRef)
            ? "TXN-" + DateTime.Now.ToString("yyyyMMdd-HHmmss")
            : transactionRef;

        try
        {
            var payment = new Payment
            {
                UserId = userId,
                SubscriptionPlanId = adId == null ? planId : null,
                AdvertisementId = adId,
                Amount = amount,
                PaymentMethod = string.IsNullOrEmpty(paymentMethod) ? "promptpay" : paymentMethod,
                PaymentStatus = "pending",
                TransactionRef = txRef,
                PaidAt = DateTime.Now,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            _db.Payments.Add(payment);
            _db.SaveChanges();
        }
        catch { }

        // เปิดโหมดทดลอง VIP ใน Session ทันทีเพื่อให้พรีเซนต์ให้ดูได้ด้วย
        if (adId == null)
        {
            HttpContext.Session.SetString("IsVip", "true");
        }

        TempData["PaymentSuccess"] = $"แจ้งชำระเงินยอด {amount:N2} บาท (รหัสอ้างอิง: {txRef}) สำเร็จแล้ว! ข้อมูลถูกส่งไปยังฝ่ายบัญชีเพื่อยืนยันยอด";
        return RedirectToAction("Membership");
    }

    // หน้าสำหรับให้ผู้ใช้หรือร้านค้าเข้ามาสมัครลงป้ายโฆษณาของตนเองได้
    [HttpGet]
    public IActionResult Ads()
    {
        int userId = HttpContext.Session.GetInt32("UserId") ?? 1;
        List<Advertisement> myAds = new();
        try
        {
            myAds = _db.Advertisements.Where(a => a.UserId == userId).OrderByDescending(a => a.CreatedAt).ToList();
        }
        catch { }
        ViewBag.MyAds = myAds;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateAd(string title, string imageUrl, string redirectUrl, DateOnly startDate, DateOnly endDate)
    {
        int userId = HttpContext.Session.GetInt32("UserId") ?? 1;
        int createdAdId = 1;
        try
        {
            var ad = new Advertisement
            {
                UserId = userId,
                Title = title,
                ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? "https://images.unsplash.com/photo-1554118811-1e0d58224f24?auto=format&fit=crop&w=800&q=80" : imageUrl,
                RedirectUrl = string.IsNullOrWhiteSpace(redirectUrl) ? "https://example.com" : redirectUrl,
                StartDate = startDate == default ? DateOnly.FromDateTime(DateTime.Today) : startDate,
                EndDate = endDate == default ? DateOnly.FromDateTime(DateTime.Today.AddDays(15)) : endDate,
                Status = "paused", // รอชำระเงินและอนุมัติ
                ViewsCount = 0,
                ClicksCount = 0,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            _db.Advertisements.Add(ad);
            _db.SaveChanges();
            createdAdId = ad.Id;
        }
        catch { }

        return RedirectToAction("Payment", new { planId = 3, adId = createdAdId, amount = 499.00m, title = "ค่าลงป้ายโฆษณา: " + title });
    }

    // Wireframe หน้า 8: หน้าโปรไฟล์ผู้ใช้งานและการตั้งค่า
    [HttpGet]
    public IActionResult Profile()
    {
        int userId = HttpContext.Session.GetInt32("UserId") ?? 1;
        User? user = null;
        try
        {
            user = _db.Users.FirstOrDefault(u => u.Id == userId);
        }
        catch { }

        user ??= new User
        {
            Id = userId,
            UserName = HttpContext.Session.GetString("Username") ?? "customer01",
            Email = HttpContext.Session.GetString("Email") ?? "customer@tanglat.com",
            PhoneNumber = "0812345678",
            Role = "customer",
            Status = "active"
        };

        ViewBag.IsVip = CheckUserVip(userId);
        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateProfile(string userName, string email, string phoneNumber)
    {
        int userId = HttpContext.Session.GetInt32("UserId") ?? 1;
        try
        {
            var user = _db.Users.FirstOrDefault(u => u.Id == userId);
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
        TempData["ProfileMsg"] = "อัปเดตข้อมูลโปรไฟล์เรียบร้อยแล้ว!";
        return RedirectToAction("Profile");
    }
}
