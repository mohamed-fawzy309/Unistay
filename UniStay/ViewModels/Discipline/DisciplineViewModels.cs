using System.ComponentModel.DataAnnotations;

namespace UniStay.ViewModels.Discipline;

public class BulkPermissionViewModel
{
    [Required(ErrorMessage = "يجب اختيار طالب واحد على الأقل")]
    public List<int> StudentIDs { get; set; } = new();

    [Required(ErrorMessage = "تاريخ البداية مطلوب")]
    public DateOnly FromDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required(ErrorMessage = "تاريخ النهاية مطلوب")]
    public DateOnly ToDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required(ErrorMessage = "اسم ولي الأمر مطلوب")]
    [StringLength(100)]
    public string GuardianName { get; set; } = null!;

    [Required(ErrorMessage = "صلة القرابة مطلوبة")]
    [StringLength(50)]
    public string GuardianRelation { get; set; } = null!;

    [Required(ErrorMessage = "رقم هاتف ولي الأمر مطلوب")]
    [Phone(ErrorMessage = "رقم الهاتف غير صالح")]
    [StringLength(20)]
    public string GuardianPhone { get; set; } = null!;

    [StringLength(500)]
    public string? Reason { get; set; }

    [Required(ErrorMessage = "المدينة الجامعية مطلوبة")]
    public int DormitoryCityID { get; set; }
}
public class StudentLookupItem
{
    public int ID { get; set; }
    public string FullName { get; set; } = "";
    public string NationalID { get; set; } = "";
}
