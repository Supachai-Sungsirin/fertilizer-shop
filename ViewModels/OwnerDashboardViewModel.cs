namespace FertilizerShop.ViewModels
{
    public class OwnerDashboardViewModel
    {
        public decimal TodaySales { get; set; }
        public decimal MonthSales { get; set; }
        public int TotalOrdersMonth { get; set; }
        public int LowStockCount { get; set; }
        
        public List<TopSellingProduct> TopProducts { get; set; }
        public List<FertilizerShop.Models.Product> LowStockProductsList { get; set; }
        public List<TopEmployeeViewModel> TopEmployees { get; set; }
    }

    public class TopSellingProduct
    {
        public string ProductName { get; set; }
        public int TotalQty { get; set; }
        public decimal TotalAmount { get; set; }
    }
    public class TopEmployeeViewModel
    {
        public string EmployeeName { get; set; }
        public int TotalBills { get; set; } 
        public decimal TotalSales { get; set; } 
    }
}