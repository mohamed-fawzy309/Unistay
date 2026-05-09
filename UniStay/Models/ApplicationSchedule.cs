using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class ApplicationSchedule
{
    public int ID { get; set; }

    public int DormitoryCityID { get; set; }

    public string AcademicYear { get; set; } = null!;

    public DateOnly? NewStudentsOpenDate { get; set; }

    public DateOnly? NewStudentsCloseDate { get; set; }

    public DateOnly? ReturningStudentsOpenDate { get; set; }

    public DateOnly? ReturningStudentsCloseDate { get; set; }

    public bool IsOpen { get; set; } = true;

    public virtual DormitoryCity DormitoryCity { get; set; } = null!;
}
