namespace UniStay.ViewModels.StudentProfiles;

public class StudentProfileListVM
{
    public IEnumerable<StudentProfileItem> Students { get; set; } = new List<StudentProfileItem>();
    public string SearchTerm { get; set; }
    public string StatusFilter { get; set; }
    public byte? AcademicYearFilter { get; set; }
    public string FacultyFilter { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; }
}

public class StudentProfileItem
{
    public int StudentId { get; set; }
    public string Name { get; set; }
    public string StudentCode { get; set; }
    public string NationalID { get; set; }
    public string Gender { get; set; }
    public bool IsActive { get; set; }
    public string Faculty { get; set; }
    public byte? AcademicYear { get; set; }
    public string Phone { get; set; }
    public bool HasActiveAllocation { get; set; }
    public int ActiveSpecialCases { get; set; }
}

public class StudentDetailsVM
{
    public StudentBasicInfo BasicInfo { get; set; }
    public StudentContactInfo ContactInfo { get; set; }
    public StudentAcademicInfo AcademicInfo { get; set; }
    public StudentHousingInfo HousingInfo { get; set; }
    public IEnumerable<GuardianInfo> Guardians { get; set; } = new List<GuardianInfo>();
    public IEnumerable<StudentDocumentInfo> Documents { get; set; } = new List<StudentDocumentInfo>();
}

public class StudentBasicInfo
{
    public int StudentId { get; set; }
    public string Name { get; set; }
    public string StudentCode { get; set; }
    public string NationalID { get; set; }
    public DateOnly BirthDate { get; set; }
    public string Gender { get; set; }
    public string Photo { get; set; }
    public bool HasDisability { get; set; }
    public bool IsOrphan { get; set; }
    public bool IsLowIncome { get; set; }
    public bool HasFamilyAbroad { get; set; }
    public bool HasMedicalCondition { get; set; }
    public bool IsForeign { get; set; }
    public bool IsActive { get; set; }
}

public class StudentContactInfo
{
    public string Phone { get; set; }
    public string Email { get; set; }
    public string Address { get; set; }
}

public class StudentAcademicInfo
{
    public string Faculty { get; set; }
    public byte? AcademicYear { get; set; }
    public string Department { get; set; }
    public decimal? GradePercentage { get; set; }
    public string GradeText { get; set; }
}

public class StudentHousingInfo
{
    public bool IsAllocated { get; set; }
    public string BuildingName { get; set; }
    public string RoomNumber { get; set; }
    public byte? BedNumber { get; set; }
    public string AllocationStatus { get; set; }
}

public class GuardianInfo
{
    public int GuardianId { get; set; }
    public string GuardianType { get; set; }
    public string FullName { get; set; }
    public string NationalID { get; set; }
    public string Phone { get; set; }
    public string Job { get; set; }
    public string Address { get; set; }
    public bool IsDeceased { get; set; }
}

public class StudentDocumentInfo
{
    public int DocumentId { get; set; }
    public string DocumentType { get; set; }
    public string FilePath { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? UploadDate { get; set; }
}

public class StudentStatusVM
{
    public int StudentId { get; set; }
    public string StudentName { get; set; }
    public string StudentCode { get; set; }
    public bool IsActive { get; set; }

    public IEnumerable<AllocationInfo> Allocations { get; set; } = new List<AllocationInfo>();
    public IEnumerable<PaymentInfo> Payments { get; set; } = new List<PaymentInfo>();
    public IEnumerable<ViolationInfo> Violations { get; set; } = new List<ViolationInfo>();
    public IEnumerable<AbsenceInfo> Absences { get; set; } = new List<AbsenceInfo>();
    public IEnumerable<MealInfo> Meals { get; set; } = new List<MealInfo>();
    public IEnumerable<DocumentInfo> Documents { get; set; } = new List<DocumentInfo>();

    public int TotalAllocations { get; set; }
    public int TotalPayments { get; set; }
    public decimal TotalPaid { get; set; }
    public int TotalViolations { get; set; }
    public int TotalAbsences { get; set; }
    public int TotalMeals { get; set; }
    public int TotalDocuments { get; set; }
}

public class AllocationInfo
{
    public int AllocationId { get; set; }
    public string BuildingName { get; set; }
    public string RoomNumber { get; set; }
    public byte BedNumber { get; set; }
    public string Status { get; set; }
    public string AcademicYear { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}

public class PaymentInfo
{
    public int PaymentId { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public string Status { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string PaymentType { get; set; }
}

public class ViolationInfo
{
    public int ViolationId { get; set; }
    public string ViolationType { get; set; }
    public string Description { get; set; }
    public DateTime? ViolationDate { get; set; }
    public string Status { get; set; }
    public string Severity { get; set; }
}

public class AbsenceInfo
{
    public int AbsenceId { get; set; }
    public DateOnly AbsenceDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string AbsenceType { get; set; }
    public string Status { get; set; }
    public string Reason { get; set; }
}

public class MealInfo
{
    public int ConsumptionId { get; set; }
    public string MealType { get; set; }
    public DateOnly MealDate { get; set; }
}

public class DocumentInfo
{
    public int DocumentId { get; set; }
    public string DocumentType { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? UploadDate { get; set; }
}

public class StudentSpecialCasesVM
{
    public int StudentId { get; set; }
    public string StudentName { get; set; }
    public string StudentCode { get; set; }

    public IEnumerable<SpecialCaseItem> SpecialCases { get; set; } = new List<SpecialCaseItem>();
    public IEnumerable<SocialCaseItem> SocialCases { get; set; } = new List<SocialCaseItem>();
}

public class SpecialCaseItem
{
    public int CaseId { get; set; }
    public string CaseType { get; set; }
    public string Description { get; set; }
    public string Status { get; set; }
    public string ReviewNotes { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class SocialCaseItem
{
    public int CaseId { get; set; }
    public string CaseType { get; set; }
    public string Description { get; set; }
    public string Priority { get; set; }
    public string Status { get; set; }
    public int? AssignedTo { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class StudentProfilePrintVM
{
    public string Title { get; set; }
    public StudentBasicInfo BasicInfo { get; set; }
    public StudentContactInfo ContactInfo { get; set; }
    public StudentAcademicInfo AcademicInfo { get; set; }
    public StudentHousingInfo HousingInfo { get; set; }
    public IEnumerable<GuardianInfo> Guardians { get; set; } = new List<GuardianInfo>();
    public IEnumerable<StudentDocumentInfo> Documents { get; set; } = new List<StudentDocumentInfo>();
    public string PrintedAt { get; set; }
}
