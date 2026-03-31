using System.ComponentModel.DataAnnotations;

namespace FertilizerShop.ViewModels
{
    public class ReceiveStockViewModel
    {
        [Required(ErrorMessage = "กรุณาเลือกสินค้า")]
        [Display(Name = "เลือกสินค้าที่รับเข้า")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "กรุณาระบุจำนวนที่รับเข้า")]
        [Range(1, 999999, ErrorMessage = "จำนวนรับเข้าต้องมากกว่า 0")]
        [Display(Name = "จำนวน (ชิ้น/กระสอบ)")]
        public int QuantityAdded { get; set; }
    }
}