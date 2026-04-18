using FertilizerShop.Models;

namespace FertilizerShop.ViewModels
{
    public class ManagerDashboardViewModel
    {
        public decimal TodaySales { get; set; }
        public int TodayOrdersCount { get; set; }
        
        public List<Product> LowStockProducts { get; set; }
        
        public List<Product> ExpiringProducts { get; set; }
    }
}