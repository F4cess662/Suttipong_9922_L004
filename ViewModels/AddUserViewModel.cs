using Microsoft.AspNetCore.Http;

namespace Suttipong_9922_L004.ViewModels
{
    public class AddUserViewModel
    {
        public IFormFile? ProfileImage { get; set; }
        public string? FullName { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Password { get; set; }
        public string? Role { get; set; } = "customer"; // ค่าเริ่มต้นเป็น ผู้ใช้งานทั่วไป
        public bool IsActive { get; set; } = true;      // ค่าเริ่มต้นเปิดใช้งานทันที
    }
}