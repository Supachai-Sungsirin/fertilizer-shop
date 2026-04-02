using System;
using System.Collections.Generic;

namespace FertilizerShop.Models;

public partial class Purchaseorder
{
    public int PoId { get; set; }

    public int SupplierId { get; set; }

    public int ManagerId { get; set; }

    public DateTime? OrderDate { get; set; }

    public string? Status { get; set; }

    public decimal TotalAmount { get; set; }
    public virtual Supplier Supplier { get; set; }
}
