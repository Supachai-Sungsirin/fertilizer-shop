using System.Collections.Generic;

namespace FertilizerShop.ViewModels
{
    public class POItemViewModel
    {
        public int ProductId { get; set; }
        public int Qty { get; set; }
        public decimal UnitCost { get; set; }
    }

    public class CreatePOViewModel
    {
        public int SupplierId { get; set; }
        public List<POItemViewModel> Items { get; set; }
    }
}