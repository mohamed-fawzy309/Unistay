using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class Allocation
{
    public int ID { get; set; }

    public int ApplicationID { get; set; }

    public int StudentID { get; set; }

    public int CityRoomID { get; set; }

    public byte BedNumber { get; set; }

    public string AcademicYear { get; set; } = null!;

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string Status { get; set; } = null!;

    public int? AllocatedBy { get; set; }

    public DateTime? AllocatedAt { get; set; }

    public string? Notes { get; set; }

    public virtual SystemUser? AllocatedByNavigation { get; set; }

    public virtual Application Application { get; set; } = null!;

    public virtual CityRoom CityRoom { get; set; } = null!;

    public virtual ICollection<EvictionNotice> EvictionNotices { get; set; } = new List<EvictionNotice>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Student Student { get; set; } = null!;

    public virtual ICollection<StudentInventory> StudentInventories { get; set; } = new List<StudentInventory>();
}
