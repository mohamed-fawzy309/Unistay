namespace UniStay.ViewModels.Allocation
{
    public class BlueprintViewModel
    {
        public int BuildingID { get; set; }
        public string BuildingName { get; set; } = null!;
        public int FloorNumber { get; set; }
        public List<int> AvailableFloors { get; set; } = new();
        public List<FloorBlueprint> Floors { get; set; } = new();
    }

    public class FloorBlueprint
    {
        public int FloorNumber { get; set; }
        public List<RoomBlueprint> Rooms { get; set; } = new();
    }

    public class RoomBlueprint
    {
        public int RoomID { get; set; }
        public string RoomNumber { get; set; } = null!;
        public int BedsCount { get; set; }
        public List<BedBlueprint> Beds { get; set; } = new();
    }

    public class BedBlueprint
    {
        public int BedNumber { get; set; }
        public bool IsOccupied { get; set; }
        public string? OccupiedByName { get; set; }
        public int? OccupiedByStudentID { get; set; }
    }
}
