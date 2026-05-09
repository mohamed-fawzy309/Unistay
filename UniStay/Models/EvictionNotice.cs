using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class EvictionNotice
{
    public int ID { get; set; }

    public int StudentID { get; set; }

    public int AllocationID { get; set; }

    public string? Reason { get; set; }

    public string? EvictionType { get; set; }

    public string? Status { get; set; }

    public int? IssuedBy { get; set; }

    public DateTime? IssuedAt { get; set; }

    public DateTime? ExecutedAt { get; set; }

    public virtual Allocation Allocation { get; set; } = null!;

    public virtual SystemUser? IssuedByNavigation { get; set; }

    public virtual Student Student { get; set; } = null!;
}
