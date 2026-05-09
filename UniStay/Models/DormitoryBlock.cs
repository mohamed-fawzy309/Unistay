using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class DormitoryBlock
{
    public int ID { get; set; }

    public int CityBuildingID { get; set; }

    public byte? FloorNumber { get; set; }

    public string? Faculty { get; set; }

    public string? AcademicYear { get; set; }

    public int MaxStudents { get; set; }

    public virtual CityBuilding CityBuilding { get; set; } = null!;
}
