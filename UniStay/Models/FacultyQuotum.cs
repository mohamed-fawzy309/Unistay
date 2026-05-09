using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class FacultyQuotum
{
    public int ID { get; set; }

    public int DormitoryCityID { get; set; }

    public string AcademicYear { get; set; } = null!;

    public string Faculty { get; set; } = null!;

    public int MaxQuota { get; set; }

    public int MinQuota { get; set; }

    public int CurrentCount { get; set; }

    public virtual DormitoryCity DormitoryCity { get; set; } = null!;
}
