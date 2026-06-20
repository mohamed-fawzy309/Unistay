namespace UniStay.ViewModels.Applications;

public class OnlineReviewIndexViewModel
{
    public List<OnlineReviewRowViewModel> Applications { get; set; } = new();
    public OnlineReviewFilterViewModel Filter { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public int PendingReview { get; set; }
    public int DocumentsVerified { get; set; }
    public int DocumentsRejected { get; set; }
    public int MissingDocuments { get; set; }
}

public class OnlineReviewFilterViewModel
{
    public string? Search { get; set; }
    public int? CityID { get; set; }
    public string? DocumentStatus { get; set; }
}

public class OnlineReviewRowViewModel
{
    public int ApplicationID { get; set; }
    public string StudentName { get; set; } = "";
    public string NationalID { get; set; } = "";
    public string? Faculty { get; set; }
    public string? CityName { get; set; }
    public string Status { get; set; } = "";
    public DateTime? SubmittedAt { get; set; }
    public int TotalDocs { get; set; }
    public int VerifiedDocs { get; set; }
    public int RejectedDocs { get; set; }
    public int PendingDocs { get; set; }
    public bool AllDocumentsVerified => TotalDocs > 0 && VerifiedDocs == TotalDocs;
    public bool HasMissingDocs => TotalDocs == 0;
}

public class OnlineReviewDetailViewModel
{
    public int ApplicationID { get; set; }
    public string StudentName { get; set; } = "";
    public string NationalID { get; set; } = "";
    public string? Faculty { get; set; }
    public string? CityName { get; set; }
    public string Status { get; set; } = "";
    public string? AdminNotes { get; set; }
    public List<DocumentReviewViewModel> Documents { get; set; } = new();
}

public class DocumentReviewViewModel
{
    public int DocumentID { get; set; }
    public string DocumentType { get; set; } = "";
    public string? FileName { get; set; }
    public string? FilePath { get; set; }
    public bool? IsVerified { get; set; }
    public DateTime? UploadedAt { get; set; }
    public string StatusBadge => IsVerified == true ? "verified" : IsVerified == false ? "rejected" : "pending";
}

public class CoordinationCenterViewModel
{
    public List<CoordinationResultRowViewModel> Results { get; set; } = new();
    public CoordinationFilterViewModel Filter { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public int AcceptedCount { get; set; }
    public int WaitlistCount { get; set; }
    public int PendingCount { get; set; }
    public int RejectedCount { get; set; }
    public List<CityLookup> Cities { get; set; } = new();
}

public class CoordinationFilterViewModel
{
    public int? CityID { get; set; }
    public string? AcademicYear { get; set; }
    public string? Status { get; set; }
}

public class CoordinationResultRowViewModel
{
    public int ID { get; set; }
    public int ApplicationID { get; set; }
    public string StudentName { get; set; } = "";
    public string NationalID { get; set; } = "";
    public string? Faculty { get; set; }
    public string? CityName { get; set; }
    public decimal? DistanceScore { get; set; }
    public decimal? GradeScore { get; set; }
    public decimal? AgeScore { get; set; }
    public decimal? SpecialBonus { get; set; }
    public decimal? TotalScore { get; set; }
    public int? Rank { get; set; }
    public string? Status { get; set; }
}
