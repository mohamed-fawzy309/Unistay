using System.ComponentModel.DataAnnotations;

namespace UniStay.ViewModels.Allocation
{
    public class AllocationIndexViewModel
    {
        public List<AllocationRequestRowViewModel> PendingAllocations { get; set; } = new();
        public int TotalBeds { get; set; }
        public int OccupiedBeds { get; set; }
        public int AvailableBeds { get; set; }
    }

    public class AllocationRequestRowViewModel
    {
        public int ApplicationID { get; set; }
        public string StudentName { get; set; } = null!;
        public string NationalID { get; set; } = null!;
        public string? Faculty { get; set; }
        public string AcademicYear { get; set; } = null!;
        public string CityName { get; set; } = null!;
        public string? Phone { get; set; }
    }

    public class AllocationBuildingViewModel
    {
        public int ApplicationID { get; set; }
        public string StudentName { get; set; } = null!;
        public string AcademicYear { get; set; } = null!;
        public string CityName { get; set; } = null!;
        public int DormitoryCityID { get; set; }
        public List<BuildingOptionViewModel> Buildings { get; set; } = new();
    }

    public class BuildingOptionViewModel
    {
        public int ID { get; set; }
        public string BuildingName { get; set; } = null!;
        public string BuildingType { get; set; } = null!;
        public byte FloorCount { get; set; }
        public int AvailableBeds { get; set; }
    }

    public class AllocationFloorViewModel
    {
        public int ApplicationID { get; set; }
        public int BuildingID { get; set; }
        public string BuildingName { get; set; } = null!;
        public List<int> FloorNumbers { get; set; } = new();
    }

    public class AllocationRoomViewModel
    {
        public int ApplicationID { get; set; }
        public int BuildingID { get; set; }
        public string BuildingName { get; set; } = null!;
        public byte FloorNumber { get; set; }
        public List<RoomOptionViewModel> Rooms { get; set; } = new();
    }

    public class RoomOptionViewModel
    {
        public int ID { get; set; }
        public string RoomNumber { get; set; } = null!;
        public byte BedsCount { get; set; }
        public byte CurrentOccupancy { get; set; }
        public int AvailableBeds => BedsCount - CurrentOccupancy;
        public bool IsFull => CurrentOccupancy >= BedsCount;
        public string? RoomType { get; set; }
        public bool HasAC { get; set; }
        public bool HasBalcony { get; set; }
        public bool HasPrivateBathroom { get; set; }
    }

    public class AllocationBedViewModel
    {
        public int ApplicationID { get; set; }
        public int BuildingID { get; set; }
        public int RoomID { get; set; }
        public string RoomNumber { get; set; } = null!;
        public byte FloorNumber { get; set; }
        public string BuildingName { get; set; } = null!;
        public byte BedsCount { get; set; }
        public List<BedStateViewModel> Beds { get; set; } = new();
    }

    public class BedStateViewModel
    {
        public byte BedNumber { get; set; }
        public bool IsOccupied { get; set; }
        public string? OccupiedByStudentName { get; set; }
    }

    public class ConfirmAllocationViewModel
    {
        [Required]
        public int ApplicationID { get; set; }

        public int StudentID { get; set; }

        [Required]
        public int CityRoomID { get; set; }

        [Required]
        [Range(1, 8)]
        public byte BedNumber { get; set; }

        public string? Notes { get; set; }
    }

    public class ManualAllocationViewModel
    {
        [Required(ErrorMessage = "الطالب مطلوب")]
        public int StudentID { get; set; }

        [Required(ErrorMessage = "الغرفة مطلوبة")]
        public int CityRoomID { get; set; }

        [Required(ErrorMessage = "لا يوجد طلب مقبول لهذا الطالب")]
        public int ApplicationID { get; set; }

        public byte BedNumber { get; set; } = 1;

        [Required(ErrorMessage = "العام الدراسي مطلوب")]
        [StringLength(10)]
        public string AcademicYear { get; set; } = null!;

        public string? Notes { get; set; }

        // Lookup data
        public List<StudentLookupViewModel> Students { get; set; } = new();
        public List<CityLookupViewModel> Cities { get; set; } = new();
        public List<BuildingLookupViewModel> Buildings { get; set; } = new();
        public List<RoomLookupViewModel> Rooms { get; set; } = new();
    }

    public class StudentLookupViewModel
    {
        public int ID { get; set; }
        public string FullName { get; set; } = null!;
        public string NationalID { get; set; } = null!;
    }

    public class CityLookupViewModel
    {
        public int ID { get; set; }
        public string Name { get; set; } = null!;
    }

    public class BuildingLookupViewModel
    {
        public int ID { get; set; }
        public string BuildingName { get; set; } = null!;
    }

    public class RoomLookupViewModel
    {
        public int ID { get; set; }
        public string RoomNumber { get; set; } = null!;
        public byte FloorNumber { get; set; }
        public byte BedsCount { get; set; }
        public byte CurrentOccupancy { get; set; }
        public int AvailableBeds => BedsCount - CurrentOccupancy;
    }

    public class TransferViewModel
    {
        public int AllocationID { get; set; }
        public string StudentName { get; set; } = null!;
        public string CurrentRoom { get; set; } = null!;
        public byte CurrentBed { get; set; }
        public string AcademicYear { get; set; } = null!;

        [Required(ErrorMessage = "الغرفة الجديدة مطلوبة")]
        public int NewCityRoomID { get; set; }

        [Required(ErrorMessage = "السرير الجديد مطلوب")]
        [Range(1, 8)]
        public byte NewBedNumber { get; set; }

        public string? Reason { get; set; }

        public List<RoomOptionViewModel> AvailableRooms { get; set; } = new();
    }

    public class EvictViewModel
    {
        [Required]
        public int AllocationID { get; set; }

        [Required(ErrorMessage = "السبب مطلوب")]
        public string Reason { get; set; } = null!;

        public string? EvictionType { get; set; }
    }
}
