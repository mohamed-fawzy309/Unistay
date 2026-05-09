using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class CityRoom
{
    public int ID { get; set; }

    public int CityBuildingID { get; set; }

    public string RoomNumber { get; set; } = null!;

    public byte FloorNumber { get; set; }

    public byte BedsCount { get; set; }

    public byte CurrentOccupancy { get; set; }

    public string? RoomType { get; set; }

    public bool? HasAC { get; set; }

    public bool? HasBalcony { get; set; }

    public bool? HasPrivateBathroom { get; set; }

    public bool? HasFridge { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsDeleted { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? LastUpdatedAt { get; set; }

    public int? LastUpdatedBy { get; set; }

    public virtual ICollection<Allocation> Allocations { get; set; } = new List<Allocation>();

    public virtual CityBuilding CityBuilding { get; set; } = null!;

    public virtual SystemUser? CreatedByNavigation { get; set; }

    public virtual SystemUser? LastUpdatedByNavigation { get; set; }

    public virtual ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();
}
