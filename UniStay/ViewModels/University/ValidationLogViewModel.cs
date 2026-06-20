namespace UniStay.ViewModels.University;

public class ValidationDetailViewModel
{
    public string NationalID { get; set; } = null!;
    public bool LocalExists { get; set; }
    public bool ServerFound { get; set; }
    public bool IsMatch { get; set; }
    public DateTime? LastSync { get; set; }
    public string Status { get; set; } = null!;
}