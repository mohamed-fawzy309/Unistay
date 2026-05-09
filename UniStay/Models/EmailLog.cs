using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class EmailLog
{
    public int ID { get; set; }

    public string RecipientEmail { get; set; } = null!;

    public int? StudentID { get; set; }

    public string Subject { get; set; } = null!;

    public string? Body { get; set; }

    public string EmailType { get; set; } = null!;

    public string? Status { get; set; }

    public DateTime? SentAt { get; set; }

    public DateTime? FailedAt { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Student? Student { get; set; }
}
