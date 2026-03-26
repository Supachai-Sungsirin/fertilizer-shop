using System;
using System.Collections.Generic;

namespace FertilizerShop.Models;

public partial class Customer
{
    public int CustomerId { get; set; }

    public string Phone { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public int? RewardPoints { get; set; }

    public decimal? TotalWeightBought { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
