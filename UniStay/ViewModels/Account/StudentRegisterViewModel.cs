using System.ComponentModel.DataAnnotations;

namespace UniStay.ViewModels.Account
{
    public class StudentRegisterViewModel
    {
        [Required(ErrorMessage = "الرقم القومي مطلوب")]
        [StringLength(14, MinimumLength = 14, ErrorMessage = "يجب أن يكون الرقم القومي 14 رقماً بالضبط")]
        [RegularExpression(@"^\d{14}$", ErrorMessage = "الرقم القومي يجب أن يحتوي على أرقام فقط")]
        [Display(Name = "الرقم القومي")]
        public string NationalID { get; set; } = string.Empty;

        [Required(ErrorMessage = "الاسم الرباعي مطلوب")]
        [StringLength(200, ErrorMessage = "الاسم طويل جداً")]
        [Display(Name = "الاسم الرباعي")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [MinLength(8, ErrorMessage = "كلمة المرور يجب أن تكون 8 أحرف على الأقل")]
        [MaxLength(100)]
        [DataType(DataType.Password)]
        [Display(Name = "كلمة المرور")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "تأكيد كلمة المرور مطلوب")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "كلمة المرور غير متطابقة")]
        [Display(Name = "تأكيد كلمة المرور")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
    }
}
