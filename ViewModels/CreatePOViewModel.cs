using System.Collections.Generic;

namespace FertilizerShop.ViewModels
{
    // คลาสย่อยสำหรับเก็บข้อมูลสินค้าแต่ละแถว
    public class POItemViewModel
    {
        public int ProductId { get; set; }
        public int Qty { get; set; }
        public decimal UnitCost { get; set; } // ราคาต้นทุนต่อชิ้น
    }

    // คลาสหลักสำหรับรับข้อมูลทั้งบิล
    public class CreatePOViewModel
    {
        public int SupplierId { get; set; }
        public List<POItemViewModel> Items { get; set; }
    }
}