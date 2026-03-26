using System.ComponentModel.DataAnnotations;

namespace FertilizerShop.ViewModels
{
    public class EditEmployeeViewModel
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "กรุณากรอกรหัสพนักงาน")]
        public string EmployeeId { get; set; }

        public string? PasswordHash { get; set; }

        [Required(ErrorMessage = "กรุณากรอกชื่อ")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "กรุณากรอกนามสกุล")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "กรุณาเลือกตำแหน่ง")]
        public int RoleId { get; set; }

        public bool IsActive { get; set; }
    }
}