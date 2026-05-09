using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class Payment
{
    public int ID { get; set; }

    public int StudentID { get; set; }

    public int? ApplicationID { get; set; }

    public int? AllocationID { get; set; }

    public string PaymentType { get; set; } = null!;

    public decimal Amount { get; set; }

    public decimal PaidAmount { get; set; }

    public string Status { get; set; } = null!;

    public string? PaymentMethod { get; set; }

    public string? ReceiptNumber { get; set; }

    public string? AcademicYear { get; set; }

    public int? RecordedBy { get; set; }

    public DateTime? RecordedAt { get; set; }

    public string? Notes { get; set; }

    public virtual Allocation? Allocation { get; set; }

    public virtual Application? Application { get; set; }

    public virtual ICollection<PaymentGatewayLog> PaymentGatewayLogs { get; set; } = new List<PaymentGatewayLog>();

    public virtual SystemUser? RecordedByNavigation { get; set; }

    public virtual Student Student { get; set; } = null!;
}
