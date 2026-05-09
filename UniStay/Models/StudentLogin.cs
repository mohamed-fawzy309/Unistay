using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class StudentLogin
{
    public int ID { get; set; }

    public int StudentID { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public bool? IsActive { get; set; }

    public byte? FailedAttempts { get; set; }

    public DateTime? LockedUntil { get; set; }

    public bool? MustChangePassword { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Student Student { get; set; } = null!;
}
