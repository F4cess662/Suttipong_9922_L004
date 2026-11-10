using System;
using System.Collections.Generic;

namespace Suttipong_9922_L004.Models;

public partial class Payment
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int? SubscriptionPlanId { get; set; }

    public int? AdvertisementId { get; set; }

    public decimal Amount { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public string? PaymentStatus { get; set; }

    public string? TransactionRef { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Advertisement? Advertisement { get; set; }

    public virtual SubscriptionPlan? SubscriptionPlan { get; set; }

    public virtual User User { get; set; } = null!;
}
