using System.Collections.Generic;

namespace FertilizerShop.ViewModels
{
    public class CheckoutViewModel
    {
        public List<CartItemViewModel> CartItems { get; set; }
        public string PaymentMethod { get; set; }
        public decimal DiscountAmount { get; set; }
    }
}