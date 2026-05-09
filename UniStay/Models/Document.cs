using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class Document
{
    public int ID { get; set; }

    public int StudentID { get; set; }

    public int? ApplicationID { get; set; }

    public string DocumentType { get; set; } = null!;

    public string? FileName { get; set; }

    public string? FilePath { get; set; }

    public bool? IsVerified { get; set; }

    public int? VerifiedBy { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public DateTime? UploadedAt { get; set; }

    public virtual Application? Application { get; set; }

    public virtual Student Student { get; set; } = null!;

    public virtual SystemUser? VerifiedByNavigation { get; set; }
}
