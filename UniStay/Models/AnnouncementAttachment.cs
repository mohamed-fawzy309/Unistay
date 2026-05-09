using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class AnnouncementAttachment
{
    public int ID { get; set; }

    public int AnnouncementID { get; set; }

    public string? FileName { get; set; }

    public string? FilePath { get; set; }

    public virtual Announcement Announcement { get; set; } = null!;
}
