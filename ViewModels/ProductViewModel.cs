using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FertilizerShop.ViewModels
{
    public class ProductViewModel
    {
        public int ProductId { get; set; } 
        public string Sku { get; set; }

        public string Name { get; set; }

        public int CategoryId { get; set; }

        public decimal WeightPerUnit { get; set; }

        public decimal Price { get; set; }

        public int StockQuantity { get; set; }

        public DateOnly? ExpiryDate { get; set; }

        public string? ImageUrl { get; set; } 
        
        public IFormFile? ImageUpload { get; set; }
    }
}