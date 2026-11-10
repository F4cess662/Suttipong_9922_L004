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
        public string? Role { get; set; } = "customer";
        public bool IsActive { get; set; } = true;

        // ข้อมูลสำหรับกรณีเพิ่มเป็นพนักงาน (Employee)
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Department { get; set; } = "Dev";
        public string? Position { get; set; }
        public decimal? Salary { get; set; } = 25000m;
    }
}