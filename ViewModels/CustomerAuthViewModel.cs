using System.ComponentModel.DataAnnotations;

namespace FertilizerShop.ViewModels
{
    public class CustomerLoginViewModel
    {
        [Required(ErrorMessage = "กรุณากรอกเบอร์โทรศัพท์")]
        [Display(Name = "เบอร์โทรศัพท์")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "กรุณากรอกรหัสผ่าน")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }

    public class CustomerRegisterViewModel
    {
        [Required(ErrorMessage = "กรุณากรอกเบอร์โทรศัพท์")]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "เบอร์โทรศัพท์ต้องมี 10 หลัก")]
        [Display(Name = "เบอร์โทรศัพท์")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "กรุณากรอกชื่อ-นามสกุล")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "กรุณาตั้งรหัสผ่าน")]
        [MinLength(6, ErrorMessage = "รหัสผ่านต้องมีอย่างน้อย 6 ตัวอักษร")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Compare("Password", ErrorMessage = "รหัสผ่านไม่ตรงกัน")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }
    }
}