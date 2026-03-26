using System;
using System.Collections.Generic;

namespace FertilizerShop.Models;

public partial class Product
{
    public int ProductId { get; set; }

    public string Sku { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int CategoryId { get; set; }

    public decimal WeightPerUnit { get; set; }

    public decimal Price { get; set; }

    public int StockQuantity { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<Orderdetail> Orderdetails { get; set; } = new List<Orderdetail>();
}
