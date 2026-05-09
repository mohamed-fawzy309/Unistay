using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class InventoryItem
{
    public int ID { get; set; }

    public string ItemName { get; set; } = null!;

    public string ItemCode { get; set; } = null!;

    public decimal ItemValue { get; set; }

    public int TotalStock { get; set; }

    public int AvailableStock { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<StudentInventory> StudentInventories { get; set; } = new List<StudentInventory>();
}
