using System.ComponentModel.DataAnnotations;

namespace UniStay.ViewModels.Account
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "اسم المستخدم مطلوب")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "اسم المستخدم يجب أن يكون بين 2 و 100 حرف")]
        [Display(Name = "اسم المستخدم")]
        public string Name { get; set; } = string.Empty;

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