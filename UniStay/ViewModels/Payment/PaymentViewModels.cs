using System.ComponentModel.DataAnnotations;

namespace UniStay.ViewModels.Payment
{
    public class StudentPaymentsViewModel
    {
        public int StudentID { get; set; }
        public string StudentName { get; set; } = null!;
        public string NationalID { get; set; } = null!;
        public string? Faculty { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalDue { get; set; }
        public decimal Balance => TotalDue - TotalPaid;
        public List<PaymentRowViewModel> Payments { get; set; } = new();
    }

    public class PaymentRowViewModel
    {
        public int ID { get; set; }
        public string PaymentType { get; set; } = null!;
        public decimal Amount { get; set; }
        public decimal PaidAmount { get; set; }
        public string Status { get; set; } = null!;
        public string? PaymentMethod { get; set; }
        public string? ReceiptNumber { get; set; }
        public DateTime? RecordedAt { get; set; }
        public string? RecordedByName { get; set; }
        public string? Notes { get; set; }
    }

    public class RecordPaymentViewModel
    {
        [Required(ErrorMessage = "الطالب مطلوب")]
        public int StudentID { get; set; }

        public int? ApplicationID { get; set; }

        public int? AllocationID { get; set; }

        [Required(ErrorMessage = "نوع الدفعة مطلوب")]
        public string PaymentType { get; set; } = null!;

        [Required(ErrorMessage = "المبلغ مطلوب")]
        [Range(0.01, 999999)]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "المبلغ المدفوع مطلوب")]
        [Range(0.01, 999999)]
        public decimal PaidAmount { get; set; }

        [Required(ErrorMessage = "طريقة الدفع مطلوبة")]
        public string PaymentMethod { get; set; } = null!;

        public string? ReceiptNumber { get; set; }

        [StringLength(10)]
        public string? AcademicYear { get; set; }

        public string? Notes { get; set; }

        // Lookup
        public string StudentName { get; set; } = null!;
    }

    public class ReceiptViewModel
    {
        public int PaymentID { get; set; }
        public int StudentID { get; set; }
        public string ReceiptNumber { get; set; } = null!;
        public string StudentName { get; set; } = null!;
        public string NationalID { get; set; } = null!;
        public string PaymentType { get; set; } = null!;
        public decimal Amount { get; set; }
        public decimal PaidAmount { get; set; }
        public string PaymentMethod { get; set; } = null!;
        public DateTime? RecordedAt { get; set; }
        public string? RecordedByName { get; set; }
        public string? Notes { get; set; }
        public string AcademicYear { get; set; } = null!;
    }

    public class PaymentGatewayLogViewModel
    {
        public List<GatewayLogRowViewModel> Logs { get; set; } = new();
        public string? FilterStatus { get; set; }
        public int Page { get; set; }
        public int TotalPages { get; set; }
    }

    public class GatewayLogRowViewModel
    {
        public int ID { get; set; }
        public string? TransactionID { get; set; }
        public string StudentName { get; set; } = null!;
        public string? GatewayType { get; set; }
        public decimal? Amount { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class PaymentReportViewModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? DormitoryCityID { get; set; }
        public string CityName { get; set; } = null!;

        public decimal TotalCollected { get; set; }
        public int TotalTransactions { get; set; }
        public int SuccessfulCount { get; set; }
        public int PendingCount { get; set; }
        public int OverdueCount { get; set; }

        public List<PaymentSummaryRowViewModel> Summary { get; set; } = new();
    }

    public class PaymentSummaryRowViewModel
    {
        public string PaymentType { get; set; } = null!;
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalPaid { get; set; }
    }
}
