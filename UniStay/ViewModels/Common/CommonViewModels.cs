using System.ComponentModel.DataAnnotations;

namespace UniStay.ViewModels;

public class ReviewDecisionViewModel
{
    [Required(ErrorMessage = "يرجى اختيار القرار")]
    public string Decision { get; set; } = null!;

    public string? RejectionReason { get; set; }

    public string? AdminNotes { get; set; }
}
