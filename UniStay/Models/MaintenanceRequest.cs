using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class MaintenanceRequest
{
    public int ID { get; set; }

    public int StudentID { get; set; }

    public int CityRoomID { get; set; }

    public int DormitoryCityID { get; set; }

    public string? Category { get; set; }

    public string? Description { get; set; }

    public string Priority { get; set; } = null!;

    public string Status { get; set; } = null!;

    public int? AssignedTo { get; set; }

    public DateTime? AssignedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual SystemUser? AssignedToNavigation { get; set; }

    public virtual CityRoom CityRoom { get; set; } = null!;

    public virtual DormitoryCity DormitoryCity { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}
