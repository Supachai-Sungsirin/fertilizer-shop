using System.ComponentModel.DataAnnotations;

namespace FertilizerShop.ViewModels
{
    public class PromotionViewModel
    {
        public int PromotionId { get; set; } 

        [Required(ErrorMessage = "กรุณากรอกชื่อโปรโมชั่น")]
        [Display(Name = "ชื่อโปรโมชั่น")]
        public string Name { get; set; }

        [Required(ErrorMessage = "กรุณาเลือกประเภทเงื่อนไข")]
        [Display(Name = "เงื่อนไขการได้สิทธิ์")]
        public string ConditionType { get; set; }

        [Required(ErrorMessage = "กรุณาระบุมูลค่าของเงื่อนไข")]
        [Display(Name = "มูลค่าเงื่อนไข")]
        public decimal ConditionValue { get; set; }

        [Required(ErrorMessage = "กรุณาเลือกประเภทส่วนลด")]
        [Display(Name = "ประเภทส่วนลด")]
        public string RewardType { get; set; }

        [Required(ErrorMessage = "กรุณาระบุมูลค่าส่วนลด")]
        [Display(Name = "มูลค่าส่วนลด")]
        public decimal RewardValue { get; set; }

        [Display(Name = "สถานะการใช้งาน")]
        public bool IsActive { get; set; }

        [Display(Name = "ชื่อของแถม / รายละเอียด (ถ้ามี)")]
        public string? RewardItemName { get; set; }
    }
}