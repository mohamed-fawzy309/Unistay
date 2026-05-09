using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace UniStay.ViewModels.Violation
{
    public class AddViolationViewModel
    {
        [Required(ErrorMessage = "الطالب مطلوب")]
        public int StudentID { get; set; }

        [Required(ErrorMessage = "المدينة الجامعية مطلوبة")]
        public int DormitoryCityID { get; set; }

        [Required(ErrorMessage = "نوع المخالفة مطلوب")]
        [StringLength(100)]
        public string ViolationType { get; set; } = null!;

        [Required(ErrorMessage = "درجة الخطورة مطلوبة")]
        public string Severity { get; set; } = null!;

        [Range(0.01, 999999)]
        public decimal? FineAmount { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        public List<SelectListItem> ViolationTypes { get; set; } = new();
        public List<SelectListItem> Cities { get; set; } = new();
    }

    public class ViolationReportViewModel
    {
        public List<ViolationRowViewModel> Violations { get; set; } = new();
        public string? FilterStatus { get; set; }
        public string? FilterSeverity { get; set; }
        public int? DormitoryCityID { get; set; }
        public int Page { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public List<CityLookup> Cities { get; set; } = new();
    }

    public class ViolationRowViewModel
    {
        public int ID { get; set; }
        public string StudentName { get; set; } = null!;
        public string NationalID { get; set; } = null!;
        public string ViolationType { get; set; } = null!;
        public string Severity { get; set; } = null!;
        public decimal? FineAmount { get; set; }
        public decimal? FinePaid { get; set; }
        public string Status { get; set; } = null!;
        public bool IsOnBlacklist { get; set; }
        public DateTime? RecordedAt { get; set; }
        public string? RecordedByName { get; set; }
    }

    public class CityLookup
    {
        public int ID { get; set; }
        public string Name { get; set; } = null!;
    }
}
