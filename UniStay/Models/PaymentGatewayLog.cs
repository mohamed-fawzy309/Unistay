using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class PaymentGatewayLog
{
    public int ID { get; set; }

    public int PaymentID { get; set; }

    public int StudentID { get; set; }

    public string? GatewayType { get; set; }

    public string? TransactionID { get; set; }

    public decimal? Amount { get; set; }

    public string? Status { get; set; }

    public string? GatewayResponse { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Payment Payment { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}
