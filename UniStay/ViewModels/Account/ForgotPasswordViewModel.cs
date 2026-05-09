using System.ComponentModel.DataAnnotations;

namespace UniStay.ViewModels.Account
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "الرقم القومي أو البريد الإلكتروني مطلوب")]
        [StringLength(200, MinimumLength = 10, ErrorMessage = "يجب أن يكون الإدخال بين 10 و 200 حرف")]
        [Display(Name = "الرقم القومي أو البريد الإلكتروني")]
        public string Identifier { get; set; } = string.Empty;
    }
}