namespace Suttipong_9922_L004.ViewModels
{
    public class EditUserViewModel
    {
        public int Id { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Password { get; set; }
        public string? ConfirmPassword { get; set; }
        public string? Role { get; set; }
        public bool IsActive { get; set; }

        // ข้อมูลสำหรับแก้ไขพนักงาน (Employee)
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Department { get; set; }
        public string? Position { get; set; }
        public decimal? Salary { get; set; }
    }
}