using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class StudentInventory
{
    public int ID { get; set; }

    public int StudentID { get; set; }

    public int InventoryItemID { get; set; }

    public int? AllocationID { get; set; }

    public int Quantity { get; set; }

    public string? Condition { get; set; }

    public decimal? DeductionAmount { get; set; }

    public bool? IsReturned { get; set; }

    public DateTime? AssignedAt { get; set; }

    public int? AssignedBy { get; set; }

    public DateTime? ReturnedAt { get; set; }

    public int? ReturnedBy { get; set; }

    public virtual Allocation? Allocation { get; set; }

    public virtual SystemUser? AssignedByNavigation { get; set; }

    public virtual InventoryItem InventoryItem { get; set; } = null!;

    public virtual SystemUser? ReturnedByNavigation { get; set; }

    public virtual Student Student { get; set; } = null!;
}
