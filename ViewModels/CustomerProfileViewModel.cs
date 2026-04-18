using FertilizerShop.Models;
using System.Collections.Generic;

namespace FertilizerShop.ViewModels
{
    public class CustomerProfileViewModel
    {
        public string FullName { get; set; }
        public string Phone { get; set; }
        public int RewardPoints { get; set; }
        public decimal TotalWeightBought { get; set; }
        public DateTime? MemberSince { get; set; }
        
        public List<Order> OrderHistory { get; set; }
    }
}