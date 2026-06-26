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

    /// <summary>When the payment was actually completed (nullable until paid).</summary>
    public DateTime? PaidAt { get; set; }

    /// <summary>Token/payload from the payment gateway for server-side verification.</summary>
    public string? VerificationToken { get; set; }

    /// <summary>Display month for monthly fees (e.g. "سبتمبر 2026"). Replaces Notes misuse.</summary>
    public string? MonthYear { get; set; }

    public string? Notes { get; set; }

    public virtual Allocation? Allocation { get; set; }

    public virtual Application? Application { get; set; }

    public virtual ICollection<PaymentGatewayLog> PaymentGatewayLogs { get; set; } = new List<PaymentGatewayLog>();

    public virtual SystemUser? RecordedByNavigation { get; set; }

    public virtual Student Student { get; set; } = null!;
}
