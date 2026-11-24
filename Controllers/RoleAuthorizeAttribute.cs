using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Suttipong_9922_L004.Controllers;

/// <summary>
/// ตัวล็อกสิทธิ์การเข้าถึง (Role & Department Guard):
/// บังคับให้แต่ละตำแหน่งเข้าได้เฉพาะหน้าของแผนกตัวเอง ห้ามพิมพ์ URL ข้ามไปดูของแผนกอื่น
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RoleAuthorizeAttribute : ActionFilterAttribute
{
    private readonly string _allowedTarget;

    public RoleAuthorizeAttribute(string allowedTarget)
    {
        _allowedTarget = allowedTarget;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;
        var userId = session.GetInt32("UserId");
        var role = session.GetString("Role") ?? "";
        var dept = session.GetString("Department") ?? "";

        // หากยังไม่ได้ล็อกอิน ให้กลับไปหน้า Login ทันที
        if (userId == null)
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }

        bool isAllowed = false;

        if (_allowedTarget.Equals("Customer", StringComparison.OrdinalIgnoreCase))
        {
            isAllowed = role.Equals("customer", StringComparison.OrdinalIgnoreCase);
        }
        else if (_allowedTarget.Equals("Dev", StringComparison.OrdinalIgnoreCase))
        {
            // Dev หรือ Admin เข้าจัดการระบบของฝ่าย Dev ได้
            isAllowed = role.Equals("admin", StringComparison.OrdinalIgnoreCase) ||
                        (role.Equals("employee", StringComparison.OrdinalIgnoreCase) && dept.Equals("Dev", StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            // พนักงานแผนก Support, Marketing, Accounting เข้าได้เฉพาะแผนกตัวเองเท่านั้น
            isAllowed = role.Equals("employee", StringComparison.OrdinalIgnoreCase) &&
                        dept.Equals(_allowedTarget, StringComparison.OrdinalIgnoreCase);
        }

        if (!isAllowed)
        {
            // ส่งกลับไปยังหน้าหลักของตำแหน่งที่ผู้ใช้ล็อกอินค้างไว้ ห้ามข้ามไปดูของคนอื่น
            context.Result = GetHomeRedirectForUser(role, dept);
        }
    }

    private static IActionResult GetHomeRedirectForUser(string role, string dept)
    {
        if (role.Equals("admin", StringComparison.OrdinalIgnoreCase))
            return new RedirectToActionResult("Dashboard", "Dev", null);

        if (role.Equals("employee", StringComparison.OrdinalIgnoreCase))
        {
            if (dept.Equals("Support", StringComparison.OrdinalIgnoreCase))
                return new RedirectToActionResult("Index", "Support", null);
            if (dept.Equals("Dev", StringComparison.OrdinalIgnoreCase))
                return new RedirectToActionResult("Dashboard", "Dev", null);
            if (dept.Equals("Marketing", StringComparison.OrdinalIgnoreCase))
                return new RedirectToActionResult("Dashboard", "Marketing", null);
            if (dept.Equals("Accounting", StringComparison.OrdinalIgnoreCase))
                return new RedirectToActionResult("Dashboard", "Accounting", null);
        }

        return new RedirectToActionResult("Home", "Customer", null);
    }
}
