using FertilizerShop.Models;

namespace FertilizerShop.ViewModels
{
    public class ManagerDashboardViewModel
    {
        public decimal TodaySales { get; set; }
        public int TodayOrdersCount { get; set; }
        
        // รายการสินค้าสต็อกเหลือน้อย
        public List<Product> LowStockProducts { get; set; }
        
        // รายการสินค้าใกล้หมดอายุ (ภายใน 30 วัน)
        public List<Product> ExpiringProducts { get; set; }
    }
}