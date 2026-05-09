using System.ComponentModel.DataAnnotations;

namespace UniStay.ViewModels.Inventory
{
    public class InventoryItemsViewModel
    {
        public List<InventoryItemRowViewModel> Items { get; set; } = new();
        public CreateInventoryItemViewModel NewItem { get; set; } = new();
        public int Page { get; set; }
        public int TotalPages { get; set; }
    }

    public class InventoryItemRowViewModel
    {
        public int ID { get; set; }
        public string ItemName { get; set; } = null!;
        public string ItemCode { get; set; } = null!;
        public decimal ItemValue { get; set; }
        public int TotalStock { get; set; }
        public int AvailableStock { get; set; }
        public bool IsActive { get; set; }
        public int AssignedCount => TotalStock - AvailableStock;
    }

    public class CreateInventoryItemViewModel
    {
        [Required(ErrorMessage = "اسم الصنف مطلوب")]
        [StringLength(200)]
        public string ItemName { get; set; } = null!;

        [Required(ErrorMessage = "كود الصنف مطلوب")]
        [StringLength(50)]
        public string ItemCode { get; set; } = null!;

        [Required(ErrorMessage = "قيمة الصنف مطلوبة")]
        [Range(0.01, 999999)]
        public decimal ItemValue { get; set; }

        [Required]
        [Range(1, 100000)]
        public int TotalStock { get; set; } = 1;
    }

    public class AssignItemViewModel
    {
        [Required(ErrorMessage = "الطالب مطلوب")]
        public int StudentID { get; set; }

        [Required(ErrorMessage = "الصنف مطلوب")]
        public int InventoryItemID { get; set; }

        [Required]
        [Range(1, 100)]
        public int Quantity { get; set; } = 1;

        public int? AllocationID { get; set; }

        public string? Condition { get; set; }

        // Lookup
        public string StudentName { get; set; } = null!;
        public string ItemName { get; set; } = null!;
        public int AvailableStock { get; set; }
    }

    public class ReturnItemViewModel
    {
        [Required]
        public int StudentInventoryID { get; set; }

        public string? Condition { get; set; }

        [Range(0, 999999)]
        public decimal? DeductionAmount { get; set; }

        // Lookup
        public string ItemName { get; set; } = null!;
        public string StudentName { get; set; } = null!;
        public int Quantity { get; set; }
    }

    public class InventoryReportViewModel
    {
        public int TotalItems { get; set; }
        public int TotalAssigned { get; set; }
        public int TotalAvailable { get; set; }
        public decimal TotalValue { get; set; }
        public decimal TotalDeductions { get; set; }
        public int? DormitoryCityID { get; set; }
        public string CityName { get; set; } = null!;

        public List<InventoryAssignmentRowViewModel> Assignments { get; set; } = new();
        public int Page { get; set; }
        public int TotalPages { get; set; }
    }

    public class InventoryAssignmentRowViewModel
    {
        public int ID { get; set; }
        public string StudentName { get; set; } = null!;
        public string NationalID { get; set; } = null!;
        public string ItemName { get; set; } = null!;
        public int Quantity { get; set; }
        public string? Condition { get; set; }
        public bool IsReturned { get; set; }
        public decimal? DeductionAmount { get; set; }
        public DateTime? AssignedAt { get; set; }
    }
}
