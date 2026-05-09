using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class Announcement
{
    public int ID { get; set; }

    public string Title { get; set; } = null!;

    public string Body { get; set; } = null!;

    public string AnnouncementType { get; set; } = null!;

    public int? DormitoryCityID { get; set; }

    public string? TargetAudience { get; set; }

    public bool? IsPublished { get; set; }

    public DateTime? PublishedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<AnnouncementAttachment> AnnouncementAttachments { get; set; } = new List<AnnouncementAttachment>();

    public virtual SystemUser? CreatedByNavigation { get; set; }

    public virtual DormitoryCity? DormitoryCity { get; set; }
}
