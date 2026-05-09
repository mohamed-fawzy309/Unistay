using System.ComponentModel.DataAnnotations;

public class VerifyStudentRequest
{
    [Required]
    [StringLength(14, MinimumLength = 14)]
    public string NationalID { get; set; } = null!;
    public int? ApplicationId { get; set; }
}
