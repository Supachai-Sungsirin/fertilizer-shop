using System;
using System.Collections.Generic;

namespace FertilizerShop.Models
{
    public partial class Claim
    {
        public int ClaimId { get; set; }
        public int CustomerId { get; set; }
        public int OrderId { get; set; }

        public int ProductId { get; set; } 
        public virtual Product? Product { get; set; }
        
        public string ProblemDescription { get; set; } = null!;
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }

        public virtual Customer? Customer { get; set; }
        public virtual Order? Order { get; set; }
    }
}