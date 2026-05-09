using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class UniversityPhoto
{
    public int ID { get; set; }

    public int? DormitoryCityID { get; set; }

    public string? Title { get; set; }

    public string? PhotoType { get; set; }

    public string? FilePath { get; set; }

    public byte? SortOrder { get; set; }

    public bool? IsActive { get; set; }

    public virtual DormitoryCity? DormitoryCity { get; set; }
}
