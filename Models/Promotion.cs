using System;
using System.Collections.Generic;

namespace FertilizerShop.Models;

public partial class Promotion
{
    public int PromotionId { get; set; }

    public string Name { get; set; } = null!;

    public string ConditionType { get; set; } = null!;

    public decimal ConditionValue { get; set; }

    public string RewardType { get; set; } = null!;

    public decimal RewardValue { get; set; }

    public bool? IsActive { get; set; }
}
