using System.ComponentModel.DataAnnotations;

namespace UniStay.ViewModels.Account
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "الرقم القومي مطلوب")]
        [StringLength(14, MinimumLength = 14, ErrorMessage = "يجب أن يكون الرقم القومي 14 رقماً بالضبط")]
        [RegularExpression(@"^\d{14}$", ErrorMessage = "الرقم القومي يجب أن يحتوي على أرقام فقط")]
        [Display(Name = "الرقم القومي")]
        public string NationalID { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [MinLength(8, ErrorMessage = "كلمة المرور يجب أن تكون 8 أحرف على الأقل")]
        [MaxLength(100, ErrorMessage = "كلمة المرور طويلة جداً")]
        [DataType(DataType.Password)]
        [Display(Name = "كلمة المرور")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "تذكرني")]
        public bool RememberMe { get; set; } = false;

        public string? ErrorMessage { get; set; }
    }
}