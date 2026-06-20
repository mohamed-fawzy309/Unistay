namespace UniStay.ViewModels.SocialCases;

public class SocialCaseListVM
{
    public IEnumerable<SocialCaseListItem> Cases { get; set; } = new List<SocialCaseListItem>();
    public string SearchTerm { get; set; }
    public string StatusFilter { get; set; }
    public string PriorityFilter { get; set; }
    public string CaseTypeFilter { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; }

    public IEnumerable<string> Statuses { get; set; } = new List<string> { "مفتوحة", "قيد التحقيق", "مغلقة", "مؤرشفة" };
    public IEnumerable<string> Priorities { get; set; } = new List<string> { "عاجلة", "عالية", "متوسطة", "منخفضة" };
}

public class SocialCaseListItem
{
    public int CaseId { get; set; }
    public string StudentName { get; set; }
    public string StudentCode { get; set; }
    public string CaseType { get; set; }
    public string Priority { get; set; }
    public string Status { get; set; }
    public int? AssignedTo { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class SocialCaseDetailsVM
{
    public int CaseId { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; }
    public string StudentCode { get; set; }
    public string NationalID { get; set; }
    public string StudentPhone { get; set; }
    public string CaseType { get; set; }
    public string Description { get; set; }
    public string Status { get; set; }
    public string Priority { get; set; }
    public int? AssignedTo { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
}

public class SocialCaseCreateVM
{
    public int StudentID { get; set; }
    public string CaseType { get; set; }
    public string Description { get; set; }
    public string Priority { get; set; }
}

public class SocialCaseEditVM
{
    public int CaseId { get; set; }
    public string Status { get; set; }
    public string Priority { get; set; }
}

public class SocialCasePrintVM
{
    public string Title { get; set; }
    public string LogoPath { get; set; }
    public string OrganizationName { get; set; }
    public IEnumerable<SocialCasePrintItem> Cases { get; set; } = new List<SocialCasePrintItem>();
    public string PrintedAt { get; set; }
}

public class SocialCasePrintItem
{
    public int CaseId { get; set; }
    public string StudentName { get; set; }
    public string StudentCode { get; set; }
    public string CaseType { get; set; }
    public string Priority { get; set; }
    public string Status { get; set; }
    public int? AssignedTo { get; set; }
    public DateTime? CreatedAt { get; set; }
}
