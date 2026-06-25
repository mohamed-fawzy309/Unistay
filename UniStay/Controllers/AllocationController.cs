using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Helpers;
using UniStay.Models;
using UniStay.Services.Interfaces;
using UniStay.ViewModels.Allocation;

namespace UniStay.Controllers
{
    [Authorize(AuthenticationSchemes = "StaffCookie,AdminCookie")]
    public class AllocationController : Controller
    {
        private readonly AssuitDbContext _db;
        private readonly IAuditService _audit;
        private readonly IEmailService _email;

        public AllocationController(AssuitDbContext db, IAuditService audit, IEmailService email)
        {
            _db = db;
            _audit = audit;
            _email = email;
        }

        private int CurrentUserId => int.Parse(User.FindFirst("UserID")!.Value);

        private string GetCurrentAcademicYear()
        {
            var year = DateTime.Now.Year;
            return DateTime.Now.Month >= 6 ? $"{year}-{year + 1}" : $"{year - 1}-{year}";
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var year = GetCurrentAcademicYear();
            var pending = await _db.Applications
                .Include(a => a.Student)
                .Include(a => a.DormitoryCity)
                .Where(a => a.Status == "Accepted" && a.Allocation == null)
                .Select(a => new AllocationRequestRowViewModel
                {
                    ApplicationID = a.ID,
                    StudentName = a.Student!.FullName,
                    NationalID = a.Student.NationalID,
                    Faculty = a.Student.Faculty,
                    AcademicYear = a.AcademicYear,
                    CityName = a.DormitoryCity.Name,
                    Phone = a.Student.Phone
                })
                .ToListAsync();

            var totalBeds = await _db.CityRooms
                .Where(r => r.IsActive == true && r.IsDeleted != true
                    && r.RoomType != "إشراف" && r.RoomType != "مخزن")
                .SumAsync(r => (int)r.BedsCount);

            var occupiedBeds = await _db.CityRooms
                .Where(r => r.IsActive == true && r.IsDeleted != true
                    && r.RoomType != "إشراف" && r.RoomType != "مخزن")
                .SumAsync(r => (int)r.CurrentOccupancy);

            return View(new AllocationIndexViewModel
            {
                PendingAllocations = pending,
                TotalBeds = totalBeds,
                OccupiedBeds = occupiedBeds,
                AvailableBeds = totalBeds - occupiedBeds
            });
        }

        [HttpGet]
        public async Task<IActionResult> SelectBuilding(int appId)
        {
            var app = await _db.Applications
                .Include(a => a.Student)
                .Include(a => a.DormitoryCity)
                .FirstOrDefaultAsync(a => a.ID == appId);

            if (app == null) return NotFound();

            var buildingsRaw = await _db.CityBuildings
    .Where(b => b.DormitoryCityID == app.DormitoryCityID && b.IsActive == true && b.IsDeleted != true)
    .Include(b => b.CityRooms)
    .ToListAsync();

            var buildings = buildingsRaw
                .Select(b => new BuildingOptionViewModel
                {
                    ID = b.ID,
                    BuildingName = b.BuildingName,
                    BuildingType = b.BuildingType,
                    FloorCount = b.FloorCount,
                    AvailableBeds = b.CityRooms!
                        .Where(r => r.IsActive == true && r.IsDeleted != true
                            && r.RoomType != "إشراف" && r.RoomType != "مخزن")
                        .Sum(r => r.BedsCount - r.CurrentOccupancy)
                })
                .Where(b => b.AvailableBeds > 0)
                .ToList();

            return View(new AllocationBuildingViewModel
            {
                ApplicationID = appId,
                StudentName = app.Student?.FullName ?? "",
                AcademicYear = app.AcademicYear,
                CityName = app.DormitoryCity.Name,
                DormitoryCityID = app.DormitoryCityID,
                Buildings = buildings
            });
        }

        [HttpGet]
        public async Task<IActionResult> SelectFloor(int buildingId, int appId)
        {
            var building = await _db.CityBuildings
                .FirstOrDefaultAsync(b => b.ID == buildingId);

            if (building == null) return NotFound();

            var floors = await _db.CityRooms
                .Where(r => r.CityBuildingID == buildingId && r.IsActive == true && r.IsDeleted != true
                    && r.CurrentOccupancy < r.BedsCount
                    && r.RoomType != "إشراف" && r.RoomType != "مخزن")
                .Select(r => (int)r.FloorNumber)
                .Distinct()
                .OrderBy(f => f)
                .ToListAsync();

            return View(new AllocationFloorViewModel
            {
                ApplicationID = appId,
                BuildingID = buildingId,
                BuildingName = building.BuildingName,
                FloorNumbers = floors
            });
        }

        [HttpGet]
        public async Task<IActionResult> SelectRoom(int buildingId, int floor, int appId)
        {
            var building = await _db.CityBuildings
                .FirstOrDefaultAsync(b => b.ID == buildingId);

            if (building == null) return NotFound();

            var rooms = await _db.CityRooms
                .Where(r => r.CityBuildingID == buildingId && r.FloorNumber == floor
                    && r.IsActive == true && r.IsDeleted != true
                    && r.RoomType != "إشراف" && r.RoomType != "مخزن")
                .Select(r => new RoomOptionViewModel
                {
                    ID = r.ID,
                    RoomNumber = r.RoomNumber,
                    BedsCount = r.BedsCount,
                    CurrentOccupancy = r.CurrentOccupancy,
                    RoomType = r.RoomType,
                    HasAC = r.HasAC ?? false,
                    HasBalcony = r.HasBalcony ?? false,
                    HasPrivateBathroom = r.HasPrivateBathroom ?? false
                })
                .ToListAsync();

            return View(new AllocationRoomViewModel
            {
                ApplicationID = appId,
                BuildingID = buildingId,
                BuildingName = building.BuildingName,
                FloorNumber = (byte)floor,
                Rooms = rooms
            });
        }

        [HttpGet]
        public async Task<IActionResult> SelectBed(int roomId, int appId)
        {
            var room = await _db.CityRooms
                .Include(r => r.CityBuilding)
                .FirstOrDefaultAsync(r => r.ID == roomId);

            if (room == null) return NotFound();

            var allocations = await _db.Allocations
                .Where(a => a.CityRoomID == roomId && a.Status == "Active")
                .Include(a => a.Student)
                .ToListAsync();

            var beds = new List<BedStateViewModel>();
            for (byte i = 1; i <= room.BedsCount; i++)
            {
                var alloc = allocations.FirstOrDefault(a => a.BedNumber == i);
                beds.Add(new BedStateViewModel
                {
                    BedNumber = i,
                    IsOccupied = alloc != null,
                    OccupiedByStudentName = alloc?.Student?.FullName
                });
            }

            return View(new AllocationBedViewModel
            {
                ApplicationID = appId,
                BuildingID = room.CityBuildingID,
                RoomID = roomId,
                RoomNumber = room.RoomNumber,
                FloorNumber = room.FloorNumber,
                BuildingName = room.CityBuilding?.BuildingName ?? "",
                BedsCount = room.BedsCount,
                Beds = beds
            });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Confirm(ConfirmAllocationViewModel model)
        {
            if (!ModelState.IsValid) return Json(new { success = false, message = "بيانات غير صالحة" });

            var room = await _db.CityRooms.FindAsync(model.CityRoomID);
            if (room == null) return Json(new { success = false, message = "الغرفة غير موجودة" });

            if (room.CurrentOccupancy >= room.BedsCount)
                return Json(new { success = false, message = "الغرفة ممتلئة" });

            var bedTaken = await _db.Allocations.AnyAsync(a =>
                a.CityRoomID == model.CityRoomID && a.BedNumber == model.BedNumber && a.Status == "Active");
            if (bedTaken)
                return Json(new { success = false, message = "هذا السرير مشغول بالفعل" });

            var alreadyAllocated = await _db.Allocations
             .AnyAsync(a => a.ApplicationID == model.ApplicationID && a.Status == "Active");
            if (alreadyAllocated)
                return Json(new { success = false, message = "هذا الطالب مسكّن بالفعل" });

            var app = await _db.Applications.Include(a => a.Student).FirstOrDefaultAsync(a => a.ID == model.ApplicationID);
            if (app == null) return Json(new { success = false, message = "الطلب غير موجود" });

            model.StudentID = app.StudentID;

            var alloc = new Allocation
            {
                ApplicationID = model.ApplicationID,
                StudentID = app.StudentID,
                CityRoomID = model.CityRoomID,
                BedNumber = model.BedNumber,
                AcademicYear = app.AcademicYear,
                StartDate = DateOnly.FromDateTime(DateTime.Today),
                Status = "Active",
                AllocatedBy = CurrentUserId,
                AllocatedAt = DateTime.UtcNow,
                Notes = model.Notes
            };

            room.CurrentOccupancy++;

            _db.Allocations.Add(alloc);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Allocation.Confirm", "Allocation",
                alloc.ID, null, new { alloc.ApplicationID, alloc.StudentID, alloc.CityRoomID, alloc.BedNumber });

            var student = await _db.Students.FindAsync(model.StudentID);
            if (student?.Email != null)
            {
                await _email.SendAsync(student.Email, "تم تسكينك - UniStay",
                    $"مرحباً {student.FullName}، تم تسكينك في مبنى {room.CityBuilding?.BuildingName ?? ""} غرفة {room.RoomNumber} سرير {model.BedNumber}.",
                    Services.Interfaces.EmailType.ApplicationAccepted, student.ID);
            }

            return Json(new { success = true, message = "تم التسكين بنجاح" });
        }

        [HttpGet]
        public async Task<IActionResult> ManualAllocation(int? cityId = null)
        {
            ViewBag.Cities = await _db.DormitoryCities
                .Where(c => c.IsActive == true && c.IsDeleted != true)
                .Select(c => new CityLookupViewModel { ID = c.ID, Name = c.Name })
                .ToListAsync();

            ViewBag.Students = await _db.Students
                .Where(s => s.IsDeleted != true)
                .OrderBy(s => s.FullName)
                .Take(50)
                .Select(s => new StudentLookupViewModel { ID = s.ID, FullName = s.FullName, NationalID = s.NationalID })
                .ToListAsync();

            var model = new ManualAllocationViewModel();
            if (cityId.HasValue)
            {
                model.Buildings = await _db.CityBuildings
                    .Where(b => b.DormitoryCityID == cityId && b.IsActive == true && b.IsDeleted != true)
                    .Select(b => new BuildingLookupViewModel { ID = b.ID, BuildingName = b.BuildingName })
                    .ToListAsync();
            }
            model.AcademicYear = GetCurrentAcademicYear();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManualAllocation(ManualAllocationViewModel model)
        {
            async Task RefillViewBag()
            {
                ViewBag.Cities = await _db.DormitoryCities
                    .Where(c => c.IsActive == true && c.IsDeleted != true)
                    .Select(c => new CityLookupViewModel { ID = c.ID, Name = c.Name })
                    .ToListAsync();

                ViewBag.Students = await _db.Students
                    .Where(s => s.IsDeleted != true)
                    .OrderBy(s => s.FullName)
                    .Take(50)
                    .Select(s => new StudentLookupViewModel { ID = s.ID, FullName = s.FullName, NationalID = s.NationalID })
                    .ToListAsync();
            }

            if (!ModelState.IsValid)
            {
                await RefillViewBag();
                return View(model);
            }

            var app = await _db.Applications
                .FirstOrDefaultAsync(a => a.ID == model.ApplicationID && a.StudentID == model.StudentID && a.Status == "Accepted");

            if (app == null)
            {
                ModelState.AddModelError("", "لا يوجد طلب مقبول لهذا الطالب");
                await RefillViewBag();
                return View(model);
            }

            var alreadyAllocated = await _db.Allocations
    .AnyAsync(a => a.StudentID == model.StudentID && a.Status == "Active");
            if (alreadyAllocated)
            {
                ModelState.AddModelError("", "هذا الطالب مسكّن بالفعل ولا يمكن تسكينه مرة أخرى");
                await RefillViewBag();
                return View(model);
            }

            var room = await _db.CityRooms.FindAsync(model.CityRoomID);
            if (room == null || room.CurrentOccupancy >= room.BedsCount)
            {
                ModelState.AddModelError("", "الغرفة ممتلئة أو غير موجودة");
                await RefillViewBag();
                return View(model);
            }

            var alloc = new Allocation
            {
                ApplicationID = model.ApplicationID,
                StudentID = model.StudentID,
                CityRoomID = model.CityRoomID,
                BedNumber = model.BedNumber,
                AcademicYear = model.AcademicYear,
                StartDate = DateOnly.FromDateTime(DateTime.Today),
                Status = "Active",
                AllocatedBy = CurrentUserId,
                AllocatedAt = DateTime.UtcNow,
                Notes = model.Notes
            };

            room.CurrentOccupancy++;
            _db.Allocations.Add(alloc);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Allocation.Manual", "Allocation",
                alloc.ID, null, new { alloc.ApplicationID, alloc.StudentID, alloc.CityRoomID, alloc.BedNumber });

            TempData["Success"] = "تم التسكين اليدوي بنجاح";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Transfer(int allocationId)
        {
            var alloc = await _db.Allocations
                .Include(a => a.Student)
                .Include(a => a.CityRoom).ThenInclude(r => r.CityBuilding)
                .FirstOrDefaultAsync(a => a.ID == allocationId);

            if (alloc == null) return NotFound();

            var availableRooms = await _db.CityRooms
                .Where(r => r.CityBuilding.DormitoryCityID == alloc.CityRoom.CityBuilding.DormitoryCityID
                    && r.IsActive == true && r.IsDeleted != true
                    && r.CurrentOccupancy < r.BedsCount
                    && r.RoomType != "إشراف" && r.RoomType != "مخزن")
                .Select(r => new RoomOptionViewModel
                {
                    ID = r.ID,
                    RoomNumber = r.RoomNumber,
                    BedsCount = r.BedsCount,
                    CurrentOccupancy = r.CurrentOccupancy,
                    RoomType = r.RoomType
                })
                .ToListAsync();

            return View(new TransferViewModel
            {
                AllocationID = allocationId,
                StudentName = alloc.Student?.FullName ?? "",
                CurrentRoom = $"{alloc.CityRoom?.CityBuilding?.BuildingName ?? ""} - غرفة {alloc.CityRoom?.RoomNumber ?? ""}",
                CurrentBed = alloc.BedNumber,
                AcademicYear = alloc.AcademicYear,
                AvailableRooms = availableRooms
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Transfer(TransferViewModel model)
        {
            if (!ModelState.IsValid) return Json(new { success = false, message = "بيانات غير صالحة" });

            var alloc = await _db.Allocations
                .Include(a => a.CityRoom)
                .FirstOrDefaultAsync(a => a.ID == model.AllocationID);
            if (alloc == null) return Json(new { success = false, message = "التسكين غير موجود" });

            if (alloc.CityRoomID == model.NewCityRoomID && alloc.BedNumber == model.NewBedNumber)
                return Json(new { success = false, message = "نفس الغرفة والسرير" });

            var newRoom = await _db.CityRooms.FindAsync(model.NewCityRoomID);
            if (newRoom == null || newRoom.CurrentOccupancy >= newRoom.BedsCount)
                return Json(new { success = false, message = "الغرفة الجديدة ممتلئة" });

            var oldValues = new { alloc.CityRoomID, alloc.BedNumber };
            var oldRoomId = alloc.CityRoomID;

            alloc.CityRoomID = model.NewCityRoomID;
            alloc.BedNumber = model.NewBedNumber;
            alloc.Notes = model.Reason;

            var oldRoom = await _db.CityRooms.FindAsync(oldRoomId);
            if (oldRoom != null && oldRoom.CurrentOccupancy > 0) oldRoom.CurrentOccupancy--;

            newRoom.CurrentOccupancy++;

            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Allocation.Transfer", "Allocation",
                alloc.ID, oldValues, new { alloc.CityRoomID, alloc.BedNumber });

            return Json(new { success = true, message = "تم النقل بنجاح" });
        }

        [HttpGet]
        public async Task<IActionResult> GetBuildingsByCity(int cityId)
        {
            var buildings = await _db.CityBuildings
                .Where(b => b.DormitoryCityID == cityId && b.IsActive == true && b.IsDeleted != true)
                .Select(b => new { b.ID, b.BuildingName })
                .ToListAsync();
            return Json(buildings);
        }

        [HttpGet]
        public async Task<IActionResult> GetRoomsByBuilding(int buildingId)
        {
            var rooms = await _db.CityRooms
                .Where(r => r.CityBuildingID == buildingId && r.IsActive == true && r.IsDeleted != true
                    && r.RoomType != "إشراف" && r.RoomType != "مخزن")
                .Select(r => new { r.ID, r.RoomNumber, r.FloorNumber, r.BedsCount, r.CurrentOccupancy, Available = (int)r.BedsCount - (int)r.CurrentOccupancy })
                .ToListAsync();
            return Json(rooms);
        }

        [HttpGet]
        public async Task<IActionResult> GetApplicationByStudent(int studentId)
        {
            var app = await _db.Applications
                .Where(a => a.StudentID == studentId && a.Status == "Accepted" && a.Allocation == null)
                .OrderByDescending(a => a.ID)
                .Select(a => new { a.ID, a.AcademicYear })
                .FirstOrDefaultAsync();

            if (app == null)
                return Json(new { found = false, message = "لا يوجد طلب مقبول لهذا الطالب بدون تسكين" });

            return Json(new { found = true, applicationId = app.ID, academicYear = app.AcademicYear });
        }

        [HttpGet]
        public async Task<IActionResult> GetStudentIdByApplication(int appId)
        {
            var app = await _db.Applications.Select(a => new { a.ID, a.StudentID }).FirstOrDefaultAsync(a => a.ID == appId);
            return Json(app);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Evict(int allocationId, string reason, string? evictionType)
        {
            var alloc = await _db.Allocations
                .Include(a => a.Student)
                .Include(a => a.CityRoom)
                .FirstOrDefaultAsync(a => a.ID == allocationId);

            if (alloc == null) return Json(new { success = false, message = "التسكين غير موجود" });

            if (alloc.Status != "Active")
                return Json(new { success = false, message = "هذا التسكين ليس نشطاً" });

            var oldValues = new { alloc.Status, alloc.EndDate };
            alloc.Status = "Evicted";
            alloc.EndDate = DateOnly.FromDateTime(DateTime.Today);

            if (alloc.CityRoom != null && alloc.CityRoom.CurrentOccupancy > 0)
                alloc.CityRoom.CurrentOccupancy--;

            _db.EvictionNotices.Add(new EvictionNotice
            {
                StudentID = alloc.StudentID,
                AllocationID = allocationId,
                Reason = reason,
                EvictionType = evictionType ?? "Administrative",
                Status = "Executed",
                IssuedBy = CurrentUserId,
                IssuedAt = DateTime.UtcNow,
                ExecutedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Allocation.Evict", "Allocation",
                allocationId, oldValues, new { alloc.Status, Reason = reason });

            if (alloc.Student?.Email != null)
            {
                await _email.SendAsync(alloc.Student.Email, "إخلاء تسكين - UniStay",
                    $"تم إخلاء تسكينك في {alloc.CityRoom?.CityBuilding?.BuildingName ?? ""} بسبب: {reason}",
                    Services.Interfaces.EmailType.Eviction, alloc.StudentID);
            }

            return Json(new { success = true, message = "تم الإخلاء بنجاح" });
        }
    }
}
