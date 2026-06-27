using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Helpers;
using UniStay.Models;
using UniStay.Services.Interfaces;
using UniStay.ViewModels.Reports;

namespace UniStay.Controllers;

[Authorize(AuthenticationSchemes = "StaffCookie,AdminCookie")]
public class ReportsController : Controller
{
    private readonly AssuitDbContext _db;
    private readonly IReportExportService _export;
    private readonly IAuditService _audit;

    public ReportsController(AssuitDbContext db, IReportExportService export, IAuditService audit)
    {
        _db = db;
        _export = export;
        _audit = audit;
    }

    private int CurrentUserId => int.Parse(User.FindFirst("UserID")!.Value);

    // ════════════════════════════════════════════════════════════════
    // 1. Student Lists Report
    // ════════════════════════════════════════════════════════════════

    [HttpGet]
    [RequirePermission("Students.Manage", "CanView")]
    public async Task<IActionResult> StudentLists(
        string? search = null, int? cityId = null, int? buildingId = null,
        string? gender = null, string? faculty = null, string? status = null,
        string? sortBy = null, string? sortDir = null, int page = 1)
    {
        var query = _db.Students
            .Include(s => s.Allocations.Where(a => a.Status == "Active"))
                .ThenInclude(a => a.CityRoom).ThenInclude(r => r.CityBuilding).ThenInclude(b => b.DormitoryCity)
            .Where(s => s.IsDeleted != true)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(s => s.FullName.Contains(search) || s.NationalID.Contains(search) || (s.StudentCode != null && s.StudentCode.Contains(search)));
        if (!string.IsNullOrEmpty(gender))
            query = query.Where(s => s.Gender == gender);
        if (!string.IsNullOrEmpty(faculty))
            query = query.Where(s => s.Faculty == faculty);
        if (cityId.HasValue)
            query = query.Where(s => s.Allocations.Any(a => a.CityRoom.CityBuilding.DormitoryCityID == cityId && a.Status == "Active"));
        if (buildingId.HasValue)
            query = query.Where(s => s.Allocations.Any(a => a.CityRoom.CityBuildingID == buildingId && a.Status == "Active"));
        if (!string.IsNullOrEmpty(status))
        {
            if (status == "Allocated")
                query = query.Where(s => s.Allocations.Any(a => a.Status == "Active"));
            else if (status == "Unallocated")
                query = query.Where(s => !s.Allocations.Any(a => a.Status == "Active"));
        }

        sortBy ??= "FullName";
        sortDir ??= "asc";
        query = (sortBy, sortDir) switch
        {
            ("FullName", "desc") => query.OrderByDescending(s => s.FullName),
            ("NationalID", "asc") => query.OrderBy(s => s.NationalID),
            ("NationalID", "desc") => query.OrderByDescending(s => s.NationalID),
            ("Faculty", "asc") => query.OrderBy(s => s.Faculty),
            ("Faculty", "desc") => query.OrderByDescending(s => s.Faculty),
            ("GradePercentage", "desc") => query.OrderByDescending(s => s.GradePercentage),
            ("GradePercentage", "asc") => query.OrderBy(s => s.GradePercentage),
            _ => query.OrderBy(s => s.FullName)
        };

        var total = await query.CountAsync();
        var students = await query.Skip((page - 1) * 30).Take(30).ToListAsync();

        var rows = students.Select(s =>
        {
            var activeAlloc = s.Allocations.FirstOrDefault(a => a.Status == "Active");
            return new StudentListRowViewModel
            {
                ID = s.ID,
                FullName = s.FullName,
                NationalID = s.NationalID,
                StudentCode = s.StudentCode,
                Gender = s.Gender,
                Faculty = s.Faculty,
                Department = s.Department,
                GradePercentage = s.GradePercentage,
                Phone = s.Phone,
                Email = s.Email,
                City = s.City,
                Markaz = s.Markaz,
                Governorate = s.Governorate,
                Status = activeAlloc != null ? "Allocated" : "Unallocated",
                AllocatedCity = activeAlloc?.CityRoom?.CityBuilding?.DormitoryCity?.Name,
                BuildingName = activeAlloc?.CityRoom?.CityBuilding?.BuildingName,
                RoomNumber = activeAlloc?.CityRoom?.RoomNumber,
                HasPhoto = !string.IsNullOrEmpty(s.Photo)
            };
        }).ToList();

        var vm = new StudentListsReportViewModel
        {
            Students = rows,
            Filter = new StudentListsFilterViewModel
            {
                Search = search, CityID = cityId, BuildingID = buildingId,
                Gender = gender, Faculty = faculty, Status = status,
                SortBy = sortBy, SortDir = sortDir
            },
            TotalCount = total,
            Page = page,
            TotalPages = (int)Math.Ceiling(total / 30.0),
            Cities = await _db.DormitoryCities.Where(c => c.IsActive && !c.IsDeleted)
                .Select(c => new CityLookup { ID = c.ID, Name = c.Name }).ToListAsync()
        };

        return View(vm);
    }

    [HttpGet]
    [RequirePermission("Students.Manage", "CanView")]
    public async Task<IActionResult> StudentListsExportExcel(
        string? search = null, int? cityId = null, int? buildingId = null,
        string? gender = null, string? faculty = null, string? status = null)
    {
        var rows = await BuildStudentListData(search, cityId, buildingId, gender, faculty, status);
        var columns = new[] { "الاسم", "الرقم القومي", "كود الطالب", "النوع", "الكلية", "التقدير", "الهاتف", "البريد", "المحافظة", "المركز", "المدينة", "الحالة", "المدينة الجامعية", "المبنى", "الغرفة", "صورة" };
        var data = _export.ExportToExcel("قوائم الطلاب", columns, rows, r => new object?[] {
            r.FullName, r.NationalID, r.StudentCode, r.Gender == "Male" ? "ذكر" : "أنثى",
            r.Faculty, r.GradePercentage, r.Phone, r.Email, r.Governorate, r.Markaz, r.City,
            r.Status == "Allocated" ? "مقيم" : "غير مقيم", r.AllocatedCity, r.BuildingName, r.RoomNumber,
            r.HasPhoto == true ? "نعم" : "لا"
        });
        return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "StudentLists.xlsx");
    }

    [HttpGet]
    [RequirePermission("Students.Manage", "CanView")]
    public async Task<IActionResult> StudentListsExportPdf(
        string? search = null, int? cityId = null, int? buildingId = null,
        string? gender = null, string? faculty = null, string? status = null)
    {
        var rows = await BuildStudentListData(search, cityId, buildingId, gender, faculty, status);
        var columns = new[] { "الاسم", "الرقم القومي", "النوع", "الكلية", "الهاتف", "الحالة" };
        var data = rows.Select(r => new[] {
            r.FullName, r.NationalID, r.Gender == "Male" ? "ذكر" : "أنثى",
            r.Faculty ?? "", r.Phone ?? "",
            r.Status == "Allocated" ? "مقيم" : "غير مقيم"
        }).ToArray();
        var pdf = _export.ExportToPdf("قوائم الطلاب", columns, data);
        return File(pdf, "application/pdf", "StudentLists.pdf");
    }

    private async Task<List<StudentListRowViewModel>> BuildStudentListData(
        string? search, int? cityId, int? buildingId,
        string? gender, string? faculty, string? status)
    {
        var query = _db.Students
            .Include(s => s.Allocations.Where(a => a.Status == "Active"))
                .ThenInclude(a => a.CityRoom).ThenInclude(r => r.CityBuilding).ThenInclude(b => b.DormitoryCity)
            .Where(s => s.IsDeleted != true).AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(s => s.FullName.Contains(search) || s.NationalID.Contains(search) || (s.StudentCode != null && s.StudentCode.Contains(search)));
        if (!string.IsNullOrEmpty(gender))
            query = query.Where(s => s.Gender == gender);
        if (!string.IsNullOrEmpty(faculty))
            query = query.Where(s => s.Faculty == faculty);
        if (cityId.HasValue)
            query = query.Where(s => s.Allocations.Any(a => a.CityRoom.CityBuilding.DormitoryCityID == cityId && a.Status == "Active"));
        if (buildingId.HasValue)
            query = query.Where(s => s.Allocations.Any(a => a.CityRoom.CityBuildingID == buildingId && a.Status == "Active"));
        if (!string.IsNullOrEmpty(status))
        {
            if (status == "Allocated")
                query = query.Where(s => s.Allocations.Any(a => a.Status == "Active"));
            else if (status == "Unallocated")
                query = query.Where(s => !s.Allocations.Any(a => a.Status == "Active"));
        }

        return (await query.OrderBy(s => s.FullName).ToListAsync()).Select(s =>
        {
            var activeAlloc = s.Allocations.FirstOrDefault(a => a.Status == "Active");
            return new StudentListRowViewModel
            {
                FullName = s.FullName, NationalID = s.NationalID, StudentCode = s.StudentCode,
                Gender = s.Gender, Faculty = s.Faculty, GradePercentage = s.GradePercentage,
                Phone = s.Phone, Email = s.Email, Governorate = s.Governorate, Markaz = s.Markaz, City = s.City,
                Status = activeAlloc != null ? "Allocated" : "Unallocated",
                AllocatedCity = activeAlloc?.CityRoom?.CityBuilding?.DormitoryCity?.Name,
                BuildingName = activeAlloc?.CityRoom?.CityBuilding?.BuildingName,
                RoomNumber = activeAlloc?.CityRoom?.RoomNumber,
                HasPhoto = !string.IsNullOrEmpty(s.Photo)
            };
        }).ToList();
    }

    // ════════════════════════════════════════════════════════════════
    // 5. Room Occupancy Report
    // ════════════════════════════════════════════════════════════════

    [HttpGet]
    [RequirePermission("Rooms.Manage", "CanView")]
    public async Task<IActionResult> RoomOccupancy(int? cityId = null, int? buildingId = null, string? floor = null, string? status = null, int page = 1)
    {
        var query = _db.CityRooms
            .Include(r => r.CityBuilding).ThenInclude(b => b.DormitoryCity)
            .Include(r => r.Allocations.Where(a => a.Status == "Active"))
            .Where(r => r.RoomType != "إشراف" && r.RoomType != "مخزن")
            .AsQueryable();

        if (cityId.HasValue)
            query = query.Where(r => r.CityBuilding.DormitoryCityID == cityId);
        if (buildingId.HasValue)
            query = query.Where(r => r.CityBuildingID == buildingId);
        if (!string.IsNullOrEmpty(floor))
            query = query.Where(r => r.FloorNumber.ToString() == floor);

        var total = await query.CountAsync();
        var rooms = await query.OrderBy(r => r.CityBuilding.DormitoryCity.Name).ThenBy(r => r.CityBuilding.BuildingName).ThenBy(r => r.FloorNumber).ThenBy(r => r.RoomNumber)
            .Skip((page - 1) * 30).Take(30).ToListAsync();

        var rows = rooms.Select(r =>
        {
            var occ = r.Allocations.Count(a => a.Status == "Active");
            return new RoomOccupancyRowViewModel
            {
                RoomID = r.ID,
                RoomType = r.RoomType,
                CityName = r.CityBuilding.DormitoryCity.Name,
                BuildingName = r.CityBuilding.BuildingName,
                RoomNumber = r.RoomNumber,
                FloorNumber = r.FloorNumber,
                BedsCount = r.BedsCount,
                CurrentOccupancy = occ,
                AvailableBeds = r.BedsCount - occ,
                OccupancyPercent = r.BedsCount > 0 ? Math.Round((double)occ / r.BedsCount * 100, 1) : 0,
                Gender = r.CityBuilding.DormitoryCity.CityType ?? "",
                Status = r.IsActive == true ? "نشط" : "غير نشط"
            };
        }).ToList();

        var allRooms = await query.ToListAsync();
        var totalBeds = allRooms.Sum(r => r.BedsCount);
        var occupiedBeds = allRooms.Sum(r => r.Allocations.Count(a => a.Status == "Active"));

        var vm = new RoomOccupancyReportViewModel
        {
            Rooms = rows,
            Filter = new RoomOccupancyFilterViewModel { CityID = cityId, BuildingID = buildingId, Floor = floor, Status = status },
            TotalRooms = total,
            TotalBeds = totalBeds,
            OccupiedBeds = occupiedBeds,
            AvailableBeds = totalBeds - occupiedBeds,
            OccupancyRate = totalBeds > 0 ? Math.Round((double)occupiedBeds / totalBeds * 100, 1) : 0,
            Page = page,
            TotalPages = (int)Math.Ceiling(total / 30.0),
            Cities = await _db.DormitoryCities.Where(c => c.IsActive && !c.IsDeleted)
                .Select(c => new CityLookup { ID = c.ID, Name = c.Name }).ToListAsync(),
            Buildings = await _db.CityBuildings.Where(b => b.IsActive != false && b.IsDeleted != true)
                .Select(b => new BuildingLookup { ID = b.ID, Name = b.BuildingName, CityID = b.DormitoryCityID }).ToListAsync()
        };

        return View(vm);
    }

    [HttpGet]
    [RequirePermission("Rooms.Manage", "CanView")]
    public async Task<IActionResult> RoomOccupancyExportExcel(int? cityId = null, int? buildingId = null)
    {
        var rows = await BuildRoomOccupancyData(cityId, buildingId);
        var columns = new[] { "المدينة", "المبنى", "رقم الغرفة", "الدور", "النوع", "عدد الأسرة", "المشغول", "المتاح", "نسبة الإشغال", "الحالة" };
        var data = _export.ExportToExcel("إشغال الغرف", columns, rows, r => new object?[] {
            r.CityName, r.BuildingName, r.RoomNumber, r.FloorNumber, r.RoomType,
            r.BedsCount, r.CurrentOccupancy, r.AvailableBeds, r.OccupancyPercent + "%", r.Status
        });
        return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "RoomOccupancy.xlsx");
    }

    [HttpGet]
    [RequirePermission("Rooms.Manage", "CanView")]
    public async Task<IActionResult> RoomOccupancyExportPdf(int? cityId = null, int? buildingId = null)
    {
        var rows = await BuildRoomOccupancyData(cityId, buildingId);
        var columns = new[] { "المدينة", "المبنى", "الغرفة", "الدور", "النوع", "الأسرة", "المشغول", "نسبة الإشغال" };
        var data = rows.Select(r => new[] {
            r.CityName, r.BuildingName, r.RoomNumber, r.FloorNumber.ToString(),
            r.RoomType ?? "", r.BedsCount.ToString(), r.CurrentOccupancy.ToString(), r.OccupancyPercent + "%"
        }).ToArray();
        var pdf = _export.ExportToPdf("تقرير إشغال الغرف", columns, data);
        return File(pdf, "application/pdf", "RoomOccupancy.pdf");
    }

    private async Task<List<RoomOccupancyRowViewModel>> BuildRoomOccupancyData(int? cityId, int? buildingId)
    {
        var query = _db.CityRooms
            .Include(r => r.CityBuilding).ThenInclude(b => b.DormitoryCity)
            .Include(r => r.Allocations.Where(a => a.Status == "Active"))
            .Where(r => r.RoomType != "إشراف" && r.RoomType != "مخزن")
            .AsQueryable();
        if (cityId.HasValue) query = query.Where(r => r.CityBuilding.DormitoryCityID == cityId);
        if (buildingId.HasValue) query = query.Where(r => r.CityBuildingID == buildingId);
        return (await query.OrderBy(r => r.CityBuilding.DormitoryCity.Name).ThenBy(r => r.CityBuilding.BuildingName).ThenBy(r => r.FloorNumber).ThenBy(r => r.RoomNumber).ToListAsync())
            .Select(r => { var occ = r.Allocations.Count(a => a.Status == "Active"); return new RoomOccupancyRowViewModel
            {
                RoomType = r.RoomType,
                CityName = r.CityBuilding.DormitoryCity.Name, BuildingName = r.CityBuilding.BuildingName,
                RoomNumber = r.RoomNumber, FloorNumber = r.FloorNumber,
                BedsCount = r.BedsCount, CurrentOccupancy = occ, AvailableBeds = r.BedsCount - occ,
                OccupancyPercent = r.BedsCount > 0 ? Math.Round((double)occ / r.BedsCount * 100, 1) : 0, Status = r.IsActive == true ? "نشط" : "غير نشط"
            }; }).ToList();
    }

    // ════════════════════════════════════════════════════════════════
    // 6. Printed Cards Report
    // ════════════════════════════════════════════════════════════════

    [HttpGet]
    [RequirePermission("SystemUsers.Manage", "CanView")]
    public async Task<IActionResult> PrintedCards(int? cityId = null, string? status = null, DateTime? fromDate = null, DateTime? toDate = null, string? search = null, int page = 1)
    {
        var query = _db.CardPrintQueues
            .Include(q => q.Student)
            .Include(q => q.DormitoryCity)
            .Include(q => q.PrintedByNavigation)
            .AsQueryable();

        if (cityId.HasValue) query = query.Where(q => q.DormitoryCityID == cityId);
        if (!string.IsNullOrEmpty(status)) query = query.Where(q => q.Status == status);
        if (fromDate.HasValue) query = query.Where(q => q.QueuedAt >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(q => q.QueuedAt <= toDate.Value.AddDays(1));
        if (!string.IsNullOrEmpty(search))
            query = query.Where(q => q.Student.FullName.Contains(search) || q.Student.NationalID.Contains(search));

        var total = await query.CountAsync();
        var queues = await query.OrderByDescending(q => q.QueuedAt).Skip((page - 1) * 30).Take(30).ToListAsync();

        var rows = queues.Select(q =>
        {
            var idCard = _db.IDCards.FirstOrDefault(c => c.StudentID == q.StudentID);
            return new PrintedCardRowViewModel
            {
                QueueID = q.ID, StudentID = q.StudentID,
                StudentName = q.Student.FullName, NationalID = q.Student.NationalID,
                CityName = q.DormitoryCity.Name, CardNumber = idCard?.CardNumber,
                Status = q.Status, QueuedAt = q.QueuedAt, PrintedAt = q.PrintedAt,
                PrintedByName = q.PrintedByNavigation?.Name
            };
        }).ToList();

        var vm = new PrintedCardsReportViewModel
        {
            Cards = rows,
            Filter = new PrintedCardsFilterViewModel { CityID = cityId, Status = status, FromDate = fromDate, ToDate = toDate, Search = search },
            TotalQueued = await query.CountAsync(q => q.Status == "Queued"),
            TotalPrinted = await query.CountAsync(q => q.Status == "Printed"),
            TotalPending = await query.CountAsync(q => q.Status == "Pending" || q.Status == null),
            Page = page, TotalPages = (int)Math.Ceiling(total / 30.0),
            Cities = await _db.DormitoryCities.Where(c => c.IsActive && !c.IsDeleted)
                .Select(c => new CityLookup { ID = c.ID, Name = c.Name }).ToListAsync()
        };

        return View(vm);
    }

    [HttpGet]
    [RequirePermission("SystemUsers.Manage", "CanView")]
    public async Task<IActionResult> PrintedCardsExportExcel(int? cityId = null, string? status = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var rows = await BuildPrintedCardsData(cityId, status, fromDate, toDate);
        var columns = new[] { "الطالب", "الرقم القومي", "المدينة", "رقم البطاقة", "الحالة", "تاريخ الطلب", "تاريخ الطباعة" };
        var data = _export.ExportToExcel("طباعة البطاقات", columns, rows, r => new object?[] {
            r.StudentName, r.NationalID, r.CityName, r.CardNumber,
            r.Status == "Printed" ? "مطبوع" : r.Status == "Queued" ? "بقائمة الانتظار" : "معلق",
            r.QueuedAt?.ToString("yyyy-MM-dd HH:mm"), r.PrintedAt?.ToString("yyyy-MM-dd HH:mm")
        });
        return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "PrintedCards.xlsx");
    }

    [HttpGet]
    [RequirePermission("SystemUsers.Manage", "CanView")]
    public async Task<IActionResult> PrintedCardsExportPdf(int? cityId = null, string? status = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var rows = await BuildPrintedCardsData(cityId, status, fromDate, toDate);
        var columns = new[] { "الطالب", "الرقم القومي", "المدينة", "الحالة", "تاريخ الطلب" };
        var data = rows.Select(r => new[] {
            r.StudentName, r.NationalID, r.CityName ?? "",
            r.Status == "Printed" ? "مطبوع" : r.Status == "Queued" ? "بقائمة الانتظار" : "معلق",
            r.QueuedAt?.ToString("yyyy-MM-dd") ?? ""
        }).ToArray();
        var pdf = _export.ExportToPdf("تقرير طباعة البطاقات", columns, data);
        return File(pdf, "application/pdf", "PrintedCards.pdf");
    }

    private async Task<List<PrintedCardRowViewModel>> BuildPrintedCardsData(int? cityId, string? status, DateTime? fromDate, DateTime? toDate)
    {
        var query = _db.CardPrintQueues.Include(q => q.Student).Include(q => q.DormitoryCity).AsQueryable();
        if (cityId.HasValue) query = query.Where(q => q.DormitoryCityID == cityId);
        if (!string.IsNullOrEmpty(status)) query = query.Where(q => q.Status == status);
        if (fromDate.HasValue) query = query.Where(q => q.QueuedAt >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(q => q.QueuedAt <= toDate.Value.AddDays(1));
        return (await query.OrderByDescending(q => q.QueuedAt).ToListAsync()).Select(q =>
        {
            var idCard = _db.IDCards.FirstOrDefault(c => c.StudentID == q.StudentID);
            return new PrintedCardRowViewModel
            {
                StudentName = q.Student.FullName, NationalID = q.Student.NationalID,
                CityName = q.DormitoryCity.Name, CardNumber = idCard?.CardNumber,
                Status = q.Status, QueuedAt = q.QueuedAt, PrintedAt = q.PrintedAt
            };
        }).ToList();
    }

    // ════════════════════════════════════════════════════════════════
    // 7. Students Without Photos Report
    // ════════════════════════════════════════════════════════════════

    [HttpGet]
    [RequirePermission("Students.Manage", "CanView")]
    public async Task<IActionResult> StudentsWithoutPhotos(int? cityId = null, string? gender = null, string? faculty = null, string? search = null, int page = 1)
    {
        var query = _db.Students
            .Include(s => s.Allocations.Where(a => a.Status == "Active"))
            .Where(s => s.IsDeleted != true && (s.Photo == null || s.Photo == ""))
            .AsQueryable();

        if (cityId.HasValue)
            query = query.Where(s => s.Allocations.Any(a => a.CityRoom.CityBuilding.DormitoryCityID == cityId && a.Status == "Active"));
        if (!string.IsNullOrEmpty(gender)) query = query.Where(s => s.Gender == gender);
        if (!string.IsNullOrEmpty(faculty)) query = query.Where(s => s.Faculty == faculty);
        if (!string.IsNullOrEmpty(search))
            query = query.Where(s => s.FullName.Contains(search) || s.NationalID.Contains(search));

        var total = await query.CountAsync();
        var students = await query.OrderBy(s => s.FullName).Skip((page - 1) * 30).Take(30).ToListAsync();

        var rows = students.Select(s => new StudentNoPhotoRowViewModel
        {
            ID = s.ID, FullName = s.FullName, NationalID = s.NationalID,
            StudentCode = s.StudentCode, Gender = s.Gender, Faculty = s.Faculty,
            Phone = s.Phone, City = s.City, Markaz = s.Markaz
        }).ToList();

        var vm = new StudentsWithoutPhotosViewModel
        {
            Students = rows,
            Filter = new WithoutPhotosFilterViewModel { CityID = cityId, Gender = gender, Faculty = faculty, Search = search },
            TotalCount = total, Page = page, TotalPages = (int)Math.Ceiling(total / 30.0),
            Cities = await _db.DormitoryCities.Where(c => c.IsActive && !c.IsDeleted)
                .Select(c => new CityLookup { ID = c.ID, Name = c.Name }).ToListAsync()
        };

        return View(vm);
    }

    [HttpGet]
    [RequirePermission("Students.Manage", "CanView")]
    public async Task<IActionResult> StudentsWithoutPhotosExportExcel(int? cityId = null, string? gender = null, string? faculty = null)
    {
        var rows = await BuildStudentsWithoutPhotosData(cityId, gender, faculty);
        var columns = new[] { "الاسم", "الرقم القومي", "كود الطالب", "النوع", "الكلية", "الهاتف", "المركز", "المدينة" };
        var data = _export.ExportToExcel("الطلاب بدون صور", columns, rows, r => new object?[] {
            r.FullName, r.NationalID, r.StudentCode, r.Gender == "Male" ? "ذكر" : "أنثى",
            r.Faculty, r.Phone, r.Markaz, r.City
        });
        return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "StudentsWithoutPhotos.xlsx");
    }

    private async Task<List<StudentNoPhotoRowViewModel>> BuildStudentsWithoutPhotosData(int? cityId, string? gender, string? faculty)
    {
        var query = _db.Students.Where(s => s.IsDeleted != true && (s.Photo == null || s.Photo == "")).AsQueryable();
        if (cityId.HasValue) query = query.Where(s => s.Allocations.Any(a => a.CityRoom.CityBuilding.DormitoryCityID == cityId && a.Status == "Active"));
        if (!string.IsNullOrEmpty(gender)) query = query.Where(s => s.Gender == gender);
        if (!string.IsNullOrEmpty(faculty)) query = query.Where(s => s.Faculty == faculty);
        return (await query.OrderBy(s => s.FullName).ToListAsync()).Select(s => new StudentNoPhotoRowViewModel
        {
            FullName = s.FullName, NationalID = s.NationalID, StudentCode = s.StudentCode,
            Gender = s.Gender, Faculty = s.Faculty, Phone = s.Phone, City = s.City, Markaz = s.Markaz
        }).ToList();
    }

    // ════════════════════════════════════════════════════════════════
    // 9. Meal Restriction Report
    // ════════════════════════════════════════════════════════════════

    [HttpGet]
    [RequirePermission("Meals.Manage", "CanView")]
    public async Task<IActionResult> MealRestriction(int? cityId = null, string? type = null, DateTime? fromDate = null, DateTime? toDate = null, string? search = null, int page = 1)
    {
        var allBlocks = await _db.MealBlocks.Include(b => b.Student).Include(b => b.DormitoryCity).ToListAsync();
        var allCancellations = await _db.MealCancellations.Include(c => c.Student).Include(c => c.DormitoryCity).ToListAsync();

        var allRestrictions = BuildMealRestrictionList(allBlocks, allCancellations, cityId, type);

        if (!string.IsNullOrEmpty(type))
            allRestrictions = allRestrictions.Where(r => r.Type == type).ToList();
        if (!string.IsNullOrEmpty(search))
            allRestrictions = allRestrictions.Where(r => r.StudentName.Contains(search) || r.NationalID.Contains(search)).ToList();
        if (fromDate.HasValue)
            allRestrictions = allRestrictions.Where(r => r.FromDate >= fromDate.Value).ToList();
        if (toDate.HasValue)
            allRestrictions = allRestrictions.Where(r => r.ToDate <= toDate.Value).ToList();

        var total = allRestrictions.Count;
        var rows = allRestrictions.OrderByDescending(r => r.CreatedAt).Skip((page - 1) * 30).Take(30).ToList();

        var vm = new MealRestrictionReportViewModel
        {
            Restrictions = rows,
            Filter = new MealRestrictionFilterViewModel { CityID = cityId, Type = type, Search = search, FromDate = fromDate, ToDate = toDate },
            TotalBlocks = allBlocks.Count, TotalCancellations = allCancellations.Count,
            ActiveBlocks = allRestrictions.Count(r => r.IsActive),
            Page = page, TotalPages = (int)Math.Ceiling(total / 30.0),
            Cities = await _db.DormitoryCities.Where(c => c.IsActive && !c.IsDeleted)
                .Select(c => new CityLookup { ID = c.ID, Name = c.Name }).ToListAsync()
        };

        return View(vm);
    }

    [HttpGet]
    [RequirePermission("Meals.Manage", "CanView")]
    public async Task<IActionResult> MealRestrictionExportExcel(int? cityId = null, string? type = null)
    {
        var allBlocks = await _db.MealBlocks.Include(b => b.Student).Include(b => b.DormitoryCity).ToListAsync();
        var allCancellations = await _db.MealCancellations.Include(c => c.Student).Include(c => c.DormitoryCity).ToListAsync();
        var rows = BuildMealRestrictionList(allBlocks, allCancellations, cityId, type);
        var columns = new[] { "النوع", "الطالب", "الرقم القومي", "المدينة", "من تاريخ", "إلى تاريخ", "نشط", "تاريخ الإنشاء" };
        var data = _export.ExportToExcel("قيود الوجبات", columns, rows, r => new object?[] {
            r.TypeDisplay, r.StudentName, r.NationalID, r.CityName,
            r.FromDate?.ToString("yyyy-MM-dd"), r.ToDate?.ToString("yyyy-MM-dd"),
            r.IsActive ? "نعم" : "لا", r.CreatedAt.ToString("yyyy-MM-dd")
        });
        return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "MealRestrictions.xlsx");
    }

    private List<MealRestrictionRowViewModel> BuildMealRestrictionList(List<MealBlock> blocks, List<MealCancellation> cancellations, int? cityId, string? type)
    {
        var result = new List<MealRestrictionRowViewModel>();
        foreach (var b in blocks)
        {
            if (cityId.HasValue && b.DormitoryCityID != cityId) continue;
            if (!string.IsNullOrEmpty(type) && type != "Block") continue;
            result.Add(new MealRestrictionRowViewModel
            {
                Type = "Block", TypeDisplay = "حظر",
                StudentName = b.Student?.FullName ?? "", NationalID = b.Student?.NationalID ?? "",
                CityName = b.DormitoryCity?.Name, FromDate = b.FromDate.ToDateTime(TimeOnly.MinValue),
                ToDate = b.ToDate.ToDateTime(TimeOnly.MinValue),
                IsActive = b.ToDate >= DateOnly.FromDateTime(DateTime.Now),
                CreatedAt = b.CreatedAt ?? DateTime.Now
            });
        }
        foreach (var c in cancellations)
        {
            if (cityId.HasValue && c.DormitoryCityID != cityId) continue;
            if (!string.IsNullOrEmpty(type) && type != "Cancellation") continue;
            result.Add(new MealRestrictionRowViewModel
            {
                Type = "Cancellation", TypeDisplay = "إلغاء",
                StudentName = c.Student?.FullName ?? "", NationalID = c.Student?.NationalID ?? "",
                CityName = c.DormitoryCity?.Name,
                FromDate = c.FromDate.ToDateTime(TimeOnly.MinValue),
                ToDate = c.ToDate.ToDateTime(TimeOnly.MinValue),
                IsActive = true, CreatedAt = c.CreatedAt ?? DateTime.Now
            });
        }
        return result.OrderByDescending(r => r.CreatedAt).ToList();
    }

    // ════════════════════════════════════════════════════════════════
    // 10. Student Meal History Report
    // ════════════════════════════════════════════════════════════════

    [HttpGet]
    [RequirePermission("Meals.Manage", "CanView")]
    public async Task<IActionResult> StudentMealHistory(int? studentId = null, int? cityId = null, string? mealType = null, DateTime? fromDate = null, DateTime? toDate = null, string? search = null, int page = 1)
    {
        const int pageSize = 30;

        var mealsQuery = _db.Meals
            .Include(m => m.Student)
            .Include(m => m.DormitoryCity)
            .AsQueryable();

        if (studentId.HasValue) mealsQuery = mealsQuery.Where(m => m.StudentID == studentId);
        if (cityId.HasValue) mealsQuery = mealsQuery.Where(m => m.DormitoryCityID == cityId);
        if (!string.IsNullOrEmpty(mealType)) mealsQuery = mealsQuery.Where(m => m.MealType == mealType);
        if (fromDate.HasValue) mealsQuery = mealsQuery.Where(m => m.MealDate >= DateOnly.FromDateTime(fromDate.Value));
        if (toDate.HasValue) mealsQuery = mealsQuery.Where(m => m.MealDate <= DateOnly.FromDateTime(toDate.Value));
        if (!string.IsNullOrEmpty(search))
            mealsQuery = mealsQuery.Where(m => m.Student.FullName.Contains(search) || m.Student.NationalID.Contains(search));

        var total = await mealsQuery.CountAsync();
        var meals = await mealsQuery.OrderByDescending(m => m.MealDate)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var mealIds = meals.Select(m => m.ID).ToList();
        var consumptions = await _db.MealConsumptions.Where(c => mealIds.Contains(c.MealID)).ToListAsync();
        var consumptionMap = consumptions.ToLookup(c => c.MealID);

        var rows = meals.Select(m =>
        {
            var consumed = consumptionMap[m.ID].Any();
            return new StudentMealRowViewModel
            {
                ID = m.ID, StudentName = m.Student.FullName, NationalID = m.Student.NationalID,
                CityName = m.DormitoryCity?.Name, MealDate = m.MealDate.ToDateTime(TimeOnly.MinValue),
                MealType = m.MealType, Price = m.Price,
                Status = consumed ? "Consumed" : "Booked",
                StatusDisplay = consumed ? "تم الاستلام" : "محجوز",
                ScannedAt = consumed ? consumptionMap[m.ID].First().ConsumedAt : null
            };
        }).ToList();

        var vm = new StudentMealHistoryViewModel
        {
            Meals = rows,
            Filter = new StudentMealFilterViewModel { StudentID = studentId, CityID = cityId, MealType = mealType, FromDate = fromDate, ToDate = toDate, Search = search },
            TotalMeals = total,
            TotalConsumed = rows.Count(r => r.Status == "Consumed"),
            TotalCancelled = 0,
            TotalSpent = meals.Sum(m => m.Price),
            Page = page, TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            Cities = await _db.DormitoryCities.Where(c => c.IsActive && !c.IsDeleted)
                .Select(c => new CityLookup { ID = c.ID, Name = c.Name }).ToListAsync()
        };

        return View(vm);
    }

    [HttpGet]
    [RequirePermission("Meals.Manage", "CanView")]
    public async Task<IActionResult> StudentMealHistoryExportExcel(int? cityId = null, string? mealType = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var rows = await BuildMealHistoryData(cityId, mealType, fromDate, toDate);
        var columns = new[] { "الطالب", "الرقم القومي", "المدينة", "التاريخ", "نوع الوجبة", "السعر", "الحالة", "وقت المسح" };
        var data = _export.ExportToExcel("سجل الوجبات", columns, rows, r => new object?[] {
            r.StudentName, r.NationalID, r.CityName,
            r.MealDate.ToString("yyyy-MM-dd"), r.MealType, r.Price,
            r.StatusDisplay, r.ScannedAt?.ToString("yyyy-MM-dd HH:mm")
        });
        return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "StudentMealHistory.xlsx");
    }

    private async Task<List<StudentMealRowViewModel>> BuildMealHistoryData(int? cityId, string? mealType, DateTime? fromDate, DateTime? toDate)
    {
        var query = _db.Meals.Include(m => m.Student).Include(m => m.DormitoryCity).AsQueryable();
        if (cityId.HasValue) query = query.Where(m => m.DormitoryCityID == cityId);
        if (!string.IsNullOrEmpty(mealType)) query = query.Where(m => m.MealType == mealType);
        if (fromDate.HasValue) query = query.Where(m => m.MealDate >= DateOnly.FromDateTime(fromDate.Value));
        if (toDate.HasValue) query = query.Where(m => m.MealDate <= DateOnly.FromDateTime(toDate.Value));

        var meals = await query.OrderByDescending(m => m.MealDate).ToListAsync();
        var mealIds = meals.Select(m => m.ID).ToList();
        var consumptions = await _db.MealConsumptions.Where(c => mealIds.Contains(c.MealID)).ToListAsync();
        var consumptionMap = consumptions.ToLookup(c => c.MealID);

        return meals.Select(m =>
        {
            var consumed = consumptionMap[m.ID].Any();
            return new StudentMealRowViewModel
            {
                StudentName = m.Student.FullName, NationalID = m.Student.NationalID,
                CityName = m.DormitoryCity?.Name, MealDate = m.MealDate.ToDateTime(TimeOnly.MinValue),
                MealType = m.MealType, Price = m.Price,
                Status = consumed ? "Consumed" : "Booked",
                StatusDisplay = consumed ? "تم الاستلام" : "محجوز",
                ScannedAt = consumed ? consumptionMap[m.ID].First().ConsumedAt : null
            };
        }).ToList();
    }

    // ════════════════════════════════════════════════════════════════
    // 8. Student Obligations Report (واجبات الطلاب)
    // ════════════════════════════════════════════════════════════════

    [HttpGet]
    [RequirePermission("Students.Manage", "CanView")]
    public async Task<IActionResult> StudentObligations(string? search = null, int? cityId = null, string? status = null, DateTime? fromDate = null, DateTime? toDate = null, int page = 1)
    {
        var query = _db.StudentInventories
            .Include(si => si.Student)
            .Include(si => si.InventoryItem)
            .Include(si => si.AssignedByNavigation)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(si => si.Student.FullName.Contains(search) || si.Student.NationalID.Contains(search));
        if (cityId.HasValue)
            query = query.Where(si => si.Allocation!.CityRoom.CityBuilding.DormitoryCityID == cityId);
        if (!string.IsNullOrEmpty(status))
        {
            if (status == "Signed")
                query = query.Where(si => si.AssignedAt != null);
            else if (status == "Pending")
                query = query.Where(si => si.AssignedAt == null);
            else if (status == "Returned")
                query = query.Where(si => si.IsReturned == true);
        }
        if (fromDate.HasValue)
            query = query.Where(si => si.AssignedAt >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(si => si.AssignedAt <= toDate.Value.AddDays(1));

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(si => si.AssignedAt).ThenBy(si => si.Student.FullName)
            .Skip((page - 1) * 30).Take(30).ToListAsync();

        var rows = items.Select(si =>
        {
            var statusVal = si.IsReturned == true ? "Returned" : si.AssignedAt != null ? "Signed" : "Pending";
            return new StudentObligationRowViewModel
            {
                ID = si.ID,
                FullName = si.Student.FullName,
                NationalID = si.Student.NationalID,
                StudentCode = si.Student.StudentCode,
                Faculty = si.Student.Faculty,
                CityName = si.Allocation?.CityRoom?.CityBuilding?.DormitoryCity?.Name,
                ObligationType = si.InventoryItem?.ItemName ?? "",
                ObligationTypeDisplay = si.InventoryItem?.ItemName ?? "",
                Status = statusVal,
                StatusDisplay = statusVal == "Returned" ? "تم الإرجاع" : statusVal == "Signed" ? "تم الاستلام" : "قيد الانتظار",
                SignedAt = si.AssignedAt,
                SignedByName = si.AssignedByNavigation?.Name,
                Notes = si.Condition
            };
        }).ToList();

        var vm = new StudentObligationsReportViewModel
        {
            Obligations = rows,
            Filter = new StudentObligationsFilterViewModel { Search = search, CityID = cityId, Status = status, FromDate = fromDate, ToDate = toDate },
            TotalCount = total,
            TotalSigned = rows.Count(r => r.Status == "Signed"),
            TotalPending = rows.Count(r => r.Status == "Pending"),
            Page = page,
            TotalPages = (int)Math.Ceiling(total / 30.0),
            Cities = await _db.DormitoryCities.Where(c => c.IsActive && !c.IsDeleted)
                .Select(c => new CityLookup { ID = c.ID, Name = c.Name }).ToListAsync()
        };

        return View(vm);
    }

    [HttpGet]
    [RequirePermission("Students.Manage", "CanView")]
    public async Task<IActionResult> StudentObligationsExportExcel(string? search = null, int? cityId = null, string? status = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var rows = await BuildStudentObligationsData(search, cityId, status, fromDate, toDate);
        var columns = new[] { "الطالب", "الرقم القومي", "كود الطالب", "الكلية", "المدينة", "نوع الواجب", "الحالة", "تاريخ الاستلام", "مسؤول التسليم", "ملاحظات" };
        var data = _export.ExportToExcel("استلام الطلاب للواجبات", columns, rows, r => new object?[] {
            r.FullName, r.NationalID, r.StudentCode, r.Faculty, r.CityName,
            r.ObligationTypeDisplay, r.StatusDisplay, r.SignedAt?.ToString("yyyy-MM-dd HH:mm"),
            r.SignedByName, r.Notes
        });
        return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "StudentObligations.xlsx");
    }

    private async Task<List<StudentObligationRowViewModel>> BuildStudentObligationsData(string? search, int? cityId, string? status, DateTime? fromDate, DateTime? toDate)
    {
        var query = _db.StudentInventories
            .Include(si => si.Student)
            .Include(si => si.InventoryItem)
            .Include(si => si.AssignedByNavigation)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(si => si.Student.FullName.Contains(search) || si.Student.NationalID.Contains(search));
        if (cityId.HasValue)
            query = query.Where(si => si.Allocation!.CityRoom.CityBuilding.DormitoryCityID == cityId);
        if (!string.IsNullOrEmpty(status))
        {
            if (status == "Signed")
                query = query.Where(si => si.AssignedAt != null);
            else if (status == "Pending")
                query = query.Where(si => si.AssignedAt == null);
            else if (status == "Returned")
                query = query.Where(si => si.IsReturned == true);
        }
        if (fromDate.HasValue) query = query.Where(si => si.AssignedAt >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(si => si.AssignedAt <= toDate.Value.AddDays(1));

        return (await query.OrderByDescending(si => si.AssignedAt).ThenBy(si => si.Student.FullName).ToListAsync()).Select(si =>
        {
            var statusVal = si.IsReturned == true ? "Returned" : si.AssignedAt != null ? "Signed" : "Pending";
            return new StudentObligationRowViewModel
            {
                FullName = si.Student.FullName, NationalID = si.Student.NationalID,
                StudentCode = si.Student.StudentCode, Faculty = si.Student.Faculty,
                CityName = si.Allocation?.CityRoom?.CityBuilding?.DormitoryCity?.Name,
                ObligationType = si.InventoryItem?.ItemName ?? "", ObligationTypeDisplay = si.InventoryItem?.ItemName ?? "",
                Status = statusVal, StatusDisplay = statusVal == "Returned" ? "تم الإرجاع" : statusVal == "Signed" ? "تم الاستلام" : "قيد الانتظار",
                SignedAt = si.AssignedAt, SignedByName = si.AssignedByNavigation?.Name, Notes = si.Condition
            };
        }).ToList();
    }

    private static string MapCaseType(string? type) => type switch
    {
        "Orphan" => "يتيم", "LowIncome" => "ضعف دخل", "Disability" => "إعاقة",
        "Medical" => "حالة مرضية", "FamilyAbroad" => "عائلة بالخارج", "Foreign" => "طالب وافد",
        _ => type ?? ""
    };
}
