using System;
using System.Collections.Generic;

namespace FertilizerShop.Models;

public partial class Purchaseorderdetail
{
    public int PodetailId { get; set; }

    public int PoId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitCost { get; set; }

    public decimal SubTotal { get; set; }

    public virtual Product Product { get; set; }

    public virtual Purchaseorder Po { get; set; }
}
