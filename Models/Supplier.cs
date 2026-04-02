using System;
using System.Collections.Generic;

namespace FertilizerShop.Models;

public partial class Supplier
{
    public int SupplierId { get; set; }

    public string Name { get; set; } = null!;

    public string? ContactName { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }
}
