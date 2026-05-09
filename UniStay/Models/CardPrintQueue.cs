using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class CardPrintQueue
{
    public int ID { get; set; }

    public int StudentID { get; set; }

    public int DormitoryCityID { get; set; }

    public string? Status { get; set; }

    public DateTime? QueuedAt { get; set; }

    public DateTime? PrintedAt { get; set; }

    public int? PrintedBy { get; set; }

    public virtual DormitoryCity DormitoryCity { get; set; } = null!;

    public virtual SystemUser? PrintedByNavigation { get; set; }

    public virtual Student Student { get; set; } = null!;
}
