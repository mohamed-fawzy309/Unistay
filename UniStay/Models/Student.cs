using System;
using System.Collections.Generic;

namespace UniStay.Models;

public partial class Student
{
    public int ID { get; set; }

    public string NationalID { get; set; } = null!;

    public string? StudentCode { get; set; }

    public string FullName { get; set; } = null!;

    public string Gender { get; set; } = null!;

    public DateOnly BirthDate { get; set; }

    public string Religion { get; set; } = null!;

    public string Nationality { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Faculty { get; set; }

    public string? Department { get; set; }

    public byte? AcademicYear { get; set; }

    public decimal? GradePercentage { get; set; }

    public string? GradeText { get; set; }

    public bool? IsEnrolled { get; set; }

    public string? Governorate { get; set; }

    public string? Markaz { get; set; }

    public string? City { get; set; }

    public string? Address { get; set; }

    public decimal? DistanceFromUniv { get; set; }

    public string? Photo { get; set; }

    public bool? HasDisability { get; set; }

    public bool? IsOrphan { get; set; }

    public bool? IsLowIncome { get; set; }

    public bool? HasFamilyAbroad { get; set; }

    public bool? HasMedicalCondition { get; set; }

    public string? MedicalDescription { get; set; }

    public bool? IsForeign { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsDeleted { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? LastUpdatedAt { get; set; }

    public int? LastUpdatedBy { get; set; }

    public virtual ICollection<Absence> Absences { get; set; } = new List<Absence>();

    public virtual ICollection<Allocation> Allocations { get; set; } = new List<Allocation>();

    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();

    public virtual ICollection<CardPrintQueue> CardPrintQueues { get; set; } = new List<CardPrintQueue>();

    public virtual ICollection<CoordinationResult> CoordinationResults { get; set; } = new List<CoordinationResult>();

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

    public virtual ICollection<EmailLog> EmailLogs { get; set; } = new List<EmailLog>();

    public virtual ICollection<EvictionNotice> EvictionNotices { get; set; } = new List<EvictionNotice>();

    public virtual ICollection<Guardian> Guardians { get; set; } = new List<Guardian>();

    public virtual ICollection<IDCard> IDCards { get; set; } = new List<IDCard>();

    public virtual SystemUser? LastUpdatedByNavigation { get; set; }

    public virtual ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();

    public virtual ICollection<MealBlock> MealBlocks { get; set; } = new List<MealBlock>();

    public virtual ICollection<MealCancellation> MealCancellations { get; set; } = new List<MealCancellation>();

    public virtual ICollection<MealConsumption> MealConsumptions { get; set; } = new List<MealConsumption>();

    public virtual ICollection<Meal> Meals { get; set; } = new List<Meal>();

    public virtual ICollection<PaymentGatewayLog> PaymentGatewayLogs { get; set; } = new List<PaymentGatewayLog>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<SocialCase> SocialCases { get; set; } = new List<SocialCase>();

    public virtual ICollection<SpecialCase> SpecialCases { get; set; } = new List<SpecialCase>();

    public virtual ICollection<StudentDownloadLog> StudentDownloadLogs { get; set; } = new List<StudentDownloadLog>();

    public virtual ICollection<StudentInventory> StudentInventories { get; set; } = new List<StudentInventory>();

    public virtual StudentLogin? StudentLogin { get; set; }

    public virtual ICollection<StudentValidationLog> StudentValidationLogs { get; set; } = new List<StudentValidationLog>();

    public virtual ICollection<Violation> Violations { get; set; } = new List<Violation>();
}
