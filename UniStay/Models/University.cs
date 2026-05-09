using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class University
{
    public int ID { get; set; }

    public string Name { get; set; } = null!;

    public string? NameEn { get; set; }

    public string? Logo { get; set; }

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Website { get; set; }

    public string? APIBaseUrl { get; set; }

    public string? APIKey { get; set; }

    public virtual ICollection<DormitoryCity> DormitoryCities { get; set; } = new List<DormitoryCity>();
}
