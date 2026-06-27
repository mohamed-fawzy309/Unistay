using System.ComponentModel.DataAnnotations;

namespace UniStay.ViewModels.Account
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "اسم المستخدم أو البريد الإلكتروني مطلوب")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "يجب أن يكون الإدخال بين 2 و 200 حرف")]
        [Display(Name = "اسم المستخدم أو البريد الإلكتروني")]
        public string Identifier { get; set; } = string.Empty;
    }
}