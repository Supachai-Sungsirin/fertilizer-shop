using System.ComponentModel.DataAnnotations;

namespace FertilizerShop.ViewModels
{
    public class CategoryViewModel
    {
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "กรุณากรอกชื่อหมวดหมู่สินค้า")]
        [Display(Name = "ชื่อหมวดหมู่")]
        public string CategoryName { get; set; }
    }
}