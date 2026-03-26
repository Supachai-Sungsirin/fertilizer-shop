using System.ComponentModel.DataAnnotations;

namespace FertilizerShop.Models
{
    public class LoginViewModel
    {
        [Required]
        public string EmployeeID { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }

    public class RegisterViewModel
    {
        [Required]
        public string EmployeeID { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public int RoleID { get; set; } // เลือก Role
    }
}