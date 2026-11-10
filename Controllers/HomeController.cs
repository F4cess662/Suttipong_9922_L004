using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Suttipong_9922_L004.Models;
using Suttipong_9922_L004.ViewModels;

namespace Suttipong_9922_L004.Controllers;

public class HomeController : Controller
{
    private readonly Csi402dbContext _db;
    public HomeController(Csi402dbContext db)
    {
        _db = db;
    }
    public IActionResult Lab9()
    {
        // create

        return View();
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Lab9(Labstudent data)
    {
        if (ModelState.IsValid)
        {
            if (data.StdPassword != null)
            {
                var CheckID = (from a in _db.Labstudents where a.StdId == data.StdId select a).FirstOrDefault();
                // == select * from Labstudents where StdId == "@StdId" 
                if (CheckID == null)
                {
                    Labstudent s = new();
                    s.StdId = data.StdId;
                    s.StdPassword = data.StdPassword;
                    s.StdName = data.StdName;
                    s.StdLastname = data.StdLastname;
                    _db.Add(s);
                    _db.SaveChanges();
                    return View(data);
                }
                else
                {
                    ViewBag.Status = "ID ซ้ำ!!";
                return View(data);
                }
            }
            else
            {
                ViewBag.Status = "Password Null";
                return View(data);
            }
        }
        else
        {
            return View(data);
        }
    }
    public IActionResult Lab9_1()
    {
        // Read
        var user = (from l in _db.Labstudents select l).ToList();
        // == select * form Labstudents
        return View(user);
    }
    public IActionResult Lab10(string? StdId)
    {
        var User = (from s in _db.Labstudents where s.StdId == StdId select s).FirstOrDefault();
        return View(User);
    }
    [HttpPost]
[ValidateAntiForgeryToken]
public IActionResult Lab10(Labstudent data) 
{
    var User = (from s in _db.Labstudents where s.StdId == data.StdId select s).FirstOrDefault();
    if (User != null)
    {
        User.StdPassword = data.StdPassword; // แก้เป็น StdPassword และใส่ ;
        User.StdName = data.StdName;         // (ถ้าต้องการอัปเดตชื่อด้วย)
        User.StdLastname = data.StdLastname; // (ถ้าต้องการอัปเดตนามสกุลด้วย)
        _db.SaveChanges();                   // บันทึกการแก้ไขลงฐานข้อมูล
    }
    return RedirectToAction("Lab9_1");
}
    public IActionResult Lab10_1(string? StdId)
    {
        var Check = (from s in _db.Labstudents where s.StdId == StdId select s).FirstOrDefault();
        if(Check != null)
        {
            _db.Remove(Check);
            _db.SaveChanges();
            return RedirectToAction("Lab9_1","Home",new{ Status = "ลบสำเร็จ"});
        }
        else
        {
            return RedirectToAction("Lab9_1","Home",new{ Status = "ลบไม่สำเร็จ"});    
        }
    }
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Lab5(string Name, string Lastname)
    {
        var Data = new Lab5ViewModel();
        Data.Name = Name;
        Data.Lastname = Lastname;
        return View(Data);
    }

    public IActionResult Lab5_2()
    {
        var User = new List<Lab5ViewModel>
        {
          new Lab5ViewModel{Name = "A" , Lastname = "B"},
          new Lab5ViewModel{Name = "C" , Lastname = "D"}
        };
        return View(User);
    }
    public IActionResult Lab5_3()
    {
        return View();
    }
    [HttpPost]

    public IActionResult Lab5_3(Lab5ViewModel data)
    {
        string a, b, c;
        a = data.Name;
        b = data.Lastname;
        c = a + " " + b;
        ViewBag.Name = c;
        // return RedirectToAction("Lab5","Home",data);
        return RedirectToAction("Lab5", "Home", new { Name = data.Name, Lastname = data.Lastname });
    }

    public IActionResult Admin()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
