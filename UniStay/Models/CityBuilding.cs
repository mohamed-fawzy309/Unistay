using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class CityBuilding
{
    public int ID { get; set; }

    public int DormitoryCityID { get; set; }

    public string BuildingName { get; set; } = null!;

    public string BuildingType { get; set; } = null!;

    public byte FloorCount { get; set; }

    public bool IsActive { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? LastUpdatedAt { get; set; }

    public int? LastUpdatedBy { get; set; }

    public virtual ICollection<CityRoom> CityRooms { get; set; } = new List<CityRoom>();

    public virtual SystemUser? CreatedByNavigation { get; set; }

    public virtual ICollection<DormitoryBlock> DormitoryBlocks { get; set; } = new List<DormitoryBlock>();

    public virtual DormitoryCity DormitoryCity { get; set; } = null!;

    public virtual SystemUser? LastUpdatedByNavigation { get; set; }
}
