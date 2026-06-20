using System.ComponentModel.DataAnnotations;

namespace UniStay.ViewModels.University;

public class SearchStaffRequest
{
    [Required]
    public string NationalID { get; set; } = null!;
}

public class BulkValidateViewModel
{
    [Display(Name = "أرقام قومية (واحد في كل سطر أو مفصول بفاصلة)")]
    public string NationalIDsText { get; set; } = null!;
}