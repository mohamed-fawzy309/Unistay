using System.ComponentModel.DataAnnotations;

namespace UniStay.ViewModels.Application
{
    public class ApplicationViewModel
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // قسم 1: بيانات الطالب الأساسية
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        [Required(ErrorMessage = "الرقم القومي مطلوب")]
        [StringLength(14, MinimumLength = 14, ErrorMessage = "يجب أن يكون الرقم القومي 14 رقماً بالضبط")]
        [RegularExpression(@"^\d{14}$", ErrorMessage = "الرقم القومي يجب أن يحتوي على أرقام فقط")]
        [Display(Name = "الرقم القومي")]
        public string NationalID { get; set; } = string.Empty;

        [Required(ErrorMessage = "الاسم الرباعي مطلوب")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "يجب أن يكون الاسم بين 5 و 200 حرف")]
        [Display(Name = "الاسم الرباعي")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "تاريخ الميلاد مطلوب")]
        [Display(Name = "تاريخ الميلاد")]
        public DateOnly BirthDate { get; set; }

        [Required(ErrorMessage = "النوع مطلوب")]
        [RegularExpression(@"^(Male|Female)$", ErrorMessage = "القيمة يجب أن تكون Male أو Female")]
        [Display(Name = "النوع")]
        public string Gender { get; set; } = string.Empty;

        [Required(ErrorMessage = "الديانة مطلوبة")]
        [RegularExpression(@"^(Muslim|Christian|Other)$", ErrorMessage = "قيمة غير صالحة")]
        [Display(Name = "الديانة")]
        public string Religion { get; set; } = string.Empty;

        [Required(ErrorMessage = "الجنسية مطلوبة")]
        [StringLength(50)]
        [Display(Name = "الجنسية")]
        public string Nationality { get; set; } = "Egyptian";

        [Required(ErrorMessage = "المحافظة مطلوبة")]
        [StringLength(100)]
        [Display(Name = "المحافظة")]
        public string Governorate { get; set; } = string.Empty;

        [Required(ErrorMessage = "المركز مطلوب")]
        [StringLength(100)]
        [Display(Name = "المركز")]
        public string Markaz { get; set; } = string.Empty;

        [Required(ErrorMessage = "القرية / المدينة مطلوبة")]
        [StringLength(100)]
        [Display(Name = "القرية / المدينة")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "العنوان مطلوب")]
        [StringLength(500)]
        [Display(Name = "العنوان بالتفصيل")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
        [StringLength(200)]
        [Display(Name = "البريد الإلكتروني")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "رقم التليفون مطلوب")]
        [RegularExpression(@"^(01)[0-2,5]{1}[0-9]{8}$", ErrorMessage = "رقم الهاتف يجب أن يكون رقماً مصرياً صحيحاً (11 رقم يبدأ بـ 01)")]
        [Display(Name = "رقم التليفون")]
        public string Phone { get; set; } = string.Empty;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // قسم 2: البيانات الأكاديمية
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        [Required(ErrorMessage = "الكلية مطلوبة")]
        [StringLength(100)]
        [Display(Name = "الكلية")]
        public string Faculty { get; set; } = string.Empty;

        [Range(1, 7, ErrorMessage = "الفرقة يجب أن تكون بين 1 و 7")]
        [Display(Name = "الفرقة")]
        public int? AcademicYear { get; set; }

        [StringLength(50)]
        [Display(Name = "رقم شئون الطالب")]
        public string? StudentCode { get; set; }

        [Required(ErrorMessage = "التقدير مطلوب")]
        [Range(0.0, 100.0, ErrorMessage = "التقدير يجب أن يكون بين 0 و 100")]
        [Display(Name = "نسبة التقدير (%)")]
        public decimal GradePercentage { get; set; }

        [Range(0.0, 9999.99, ErrorMessage = "المسافة يجب أن تكون قيمة موجبة")]
        [Display(Name = "البُعد عن الجامعة (كم)")]
        public decimal? DistanceFromUniv { get; set; }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // قسم 3: بيانات ولي الأمر
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        [Required(ErrorMessage = "اسم الأب مطلوب")]
        [StringLength(200)]
        [Display(Name = "اسم الأب")]
        public string FatherName { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "وظيفة الأب")]
        public string? FatherJob { get; set; }

        [StringLength(500)]
        [Display(Name = "عنوان الأب")]
        public string? FatherAddress { get; set; }

        [Display(Name = "الأب متوفى")]
        public bool IsFatherDeceased { get; set; }

        // حقول ولي الأمر — إلزامية فقط إذا الأب متوفى (يتحقق منها في Controller)
        [StringLength(200)]
        [Display(Name = "اسم ولي الأمر")]
        public string? GuardianName { get; set; }

        [StringLength(14)]
        [RegularExpression(@"^\d{14}$", ErrorMessage = "الرقم القومي يجب أن يحتوي على 14 رقماً")]
        [Display(Name = "الرقم القومي لولي الأمر")]
        public string? GuardianNationalID { get; set; }

        [StringLength(500)]
        [Display(Name = "عنوان ولي الأمر")]
        public string? GuardianAddress { get; set; }

        [StringLength(100)]
        [Display(Name = "صلة القرابة")]
        public string? GuardianRelation { get; set; }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // قسم 4: بيانات الطلب
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        [Display(Name = "سكنت في أعوام سابقة")]
        public bool HasPreviousHousing { get; set; }

        [Required(ErrorMessage = "نوع السكن مطلوب")]
        [RegularExpression(@"^(Standard|Premium|VIP)$", ErrorMessage = "قيمة نوع السكن غير صالحة")]
        [Display(Name = "نوع السكن")]
        public string HousingType { get; set; } = "Standard";

        [Display(Name = "لديّ أسرة بالخارج")]
        public bool HasFamilyAbroad { get; set; }

        [Display(Name = "أعاني من حالة مرضية")]
        public bool HasMedicalCondition { get; set; }

        [StringLength(500, ErrorMessage = "وصف الحالة المرضية يجب ألا يتجاوز 500 حرف")]
        [Display(Name = "وصف الحالة المرضية")]
        public string? MedicalDescription { get; set; }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // قسم 5: بيانات الحساب
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [MinLength(8, ErrorMessage = "يجب أن تكون كلمة المرور 8 أحرف على الأقل")]
        [MaxLength(100, ErrorMessage = "كلمة المرور طويلة جداً")]
        [DataType(DataType.Password)]
        [Display(Name = "كلمة المرور")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "تأكيد كلمة المرور مطلوب")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "كلمتا المرور غير متطابقتين")]
        [Display(Name = "تأكيد كلمة المرور")]
        public string ConfirmPassword { get; set; } = string.Empty;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // قسم 6: الإقرار
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        [Required(ErrorMessage = "يجب الموافقة على الإقرار للمتابعة")]
        [Range(typeof(bool), "true", "true", ErrorMessage = "يجب الموافقة على الإقرار للمتابعة")]
        [Display(Name = "أقر بصحة البيانات وأوافق على اللوائح")]
        public bool Declaration { get; set; }

        // للتمييز بين طالب جديد ومستمر
        [Display(Name = "طالب مستمر")]
        public bool IsReturningStudent { get; set; }
    }
}