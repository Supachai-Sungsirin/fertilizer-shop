using System.ComponentModel.DataAnnotations;

namespace FertilizerShop.ViewModels
{
    public class LoginViewModel
    {

        public string EmployeeId { get; set; }

        [DataType(DataType.Password)]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }
}