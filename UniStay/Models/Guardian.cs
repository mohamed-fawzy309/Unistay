using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class Guardian
{
    public int ID { get; set; }

    public int StudentID { get; set; }

    public string GuardianType { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? NationalID { get; set; }

    public string? Phone { get; set; }

    public string? Phone2 { get; set; }

    public string? Job { get; set; }

    public string? Address { get; set; }

    public bool? IsDeceased { get; set; }

    public virtual Student Student { get; set; } = null!;
}
