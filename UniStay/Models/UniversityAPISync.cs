using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class UniversityAPISync
{
    public int ID { get; set; }

    public string NationalID { get; set; } = null!;

    public string? StudentCode { get; set; }

    public string SyncType { get; set; } = null!;

    public string? APIData { get; set; }

    public string? LocalData { get; set; }

    public bool? IsMatch { get; set; }

    public string? DifferenceDetails { get; set; }

    public DateTime? SyncedAt { get; set; }

    public int? SyncedBy { get; set; }

    public virtual SystemUser? SyncedByNavigation { get; set; }
}
