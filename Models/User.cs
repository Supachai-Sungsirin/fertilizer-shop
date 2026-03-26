using System;
using System.Collections.Generic;

namespace FertilizerShop.Models;

public partial class User
{
    public int UserId { get; set; }

    public string EmployeeId { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public int RoleId { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public bool? IsActive { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual Role Role { get; set; } = null!;
}
