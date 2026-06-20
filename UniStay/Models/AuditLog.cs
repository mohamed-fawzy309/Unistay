using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class AuditLog
{
    public int ID { get; set; }

    public int? UserID { get; set; }

    public string UserType { get; set; } = null!;

    public string Action { get; set; } = null!;

    public string? TableName { get; set; }

    public int? RecordID { get; set; }

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public string? IPAddress { get; set; }

    public int? DormitoryCityID { get; set; }

    public DateTime CreatedAt { get; set; }
}
