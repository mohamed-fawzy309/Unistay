using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class SocialCase
{
    public int ID { get; set; }

    public int StudentID { get; set; }

    public string? CaseType { get; set; }

    public string? Description { get; set; }

    public string? Status { get; set; }

    public string? Priority { get; set; }

    public int? AssignedTo { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public virtual SystemUser? AssignedToNavigation { get; set; }

    public virtual Student Student { get; set; } = null!;
}
