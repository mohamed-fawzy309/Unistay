using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class BulkOperationLog
{
    public int ID { get; set; }

    public string OperationType { get; set; } = null!;

    public int? AffectedCount { get; set; }

    public int? SuccessCount { get; set; }

    public int? FailedCount { get; set; }

    public string? Details { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual SystemUser? CreatedByNavigation { get; set; }
}
