using System;
using System.Collections.Generic;

namespace FertilizerShop.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public string ReceiptNo { get; set; } = null!;

    public DateTime? OrderDate { get; set; }

    public int CashierId { get; set; }

    public int? CustomerId { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal? DiscountAmount { get; set; }

    public decimal NetAmount { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public virtual User Cashier { get; set; } = null!;

    public virtual Customer? Customer { get; set; }

    public virtual ICollection<Orderdetail> Orderdetails { get; set; } = new List<Orderdetail>();
}
