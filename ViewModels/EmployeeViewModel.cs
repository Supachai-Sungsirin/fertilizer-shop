using System.ComponentModel.DataAnnotations;

namespace FertilizerShop.ViewModels
{
    public class EmployeeViewModel
    {
        public string EmployeeId { get; set; }

        public string PasswordHash { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public int RoleId { get; set; }
    }
}