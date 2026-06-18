using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Helpers;
using UniStay.Services.Interfaces;
using UniStay.ViewModels.Photos;

namespace UniStay.Controllers;

[Authorize(AuthenticationSchemes = "AdminCookie")]
public class CardsController : Controller
{
    private readonly AssuitDbContext _db;
    private readonly ICardPrintService _cardService;
    private readonly IAuditService _audit;

    public CardsController(AssuitDbContext db, ICardPrintService cardService, IAuditService audit)
    {
        _db = db;
        _cardService = cardService;
        _audit = audit;
    }

    private int CurrentUserId => int.Parse(User.FindFirst("UserID")!.Value);

    [HttpGet]
    [RequirePermission("Cards.View", "CanView")]
    public async Task<IActionResult> Index(string? search = null, int? cityId = null, string? status = null, int page = 1)
    {
        const int pageSize = 30;

        var query = _db.CardPrintQueues
            .Include(q => q.Student)
            .Include(q => q.DormitoryCity)
            .Include(q => q.PrintedByNavigation)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(q => q.Student.FullName.Contains(search) || q.Student.NationalID.Contains(search));
        if (cityId.HasValue)
            query = query.Where(q => q.DormitoryCityID == cityId.Value);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(q => q.Status == status);

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(q => q.QueuedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var rows = items.Select(q => new CardQueueRowViewModel
        {
            QueueID = q.ID,
            StudentID = q.StudentID,
            StudentName = q.Student?.FullName ?? "",
            NationalID = q.Student?.NationalID ?? "",
            Faculty = q.Student?.Faculty,
            CityName = q.DormitoryCity?.Name,
            Status = q.Status,
            QueuedAt = q.QueuedAt,
            PrintedAt = q.PrintedAt,
            PrintedByName = q.PrintedByNavigation?.Name
        }).ToList();

        var pendingCount = await _db.CardPrintQueues.CountAsync(q => q.Status == "Pending");
        var printedCount = await _db.CardPrintQueues.CountAsync(q => q.Status == "Printed");
        var failedCount = await _db.CardPrintQueues.CountAsync(q => q.Status == "Failed");
        var cities = await _db.DormitoryCities.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();

        var vm = new CardIndexViewModel
        {
            QueuedItems = rows,
            Filter = new CardFilterViewModel { Search = search, CityID = cityId, Status = status },
            TotalCount = total,
            Page = page,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            PendingCount = pendingCount,
            PrintedCount = printedCount,
            FailedCount = failedCount,
            Cities = cities.Select(c => new CityLookup { ID = c.ID, Name = c.Name }).ToList()
        };

        return View(vm);
    }

    [HttpGet]
    [RequirePermission("Cards.Print", "CanEdit")]
    public async Task<IActionResult> PrintSelection(string? search = null, int? cityId = null, int? buildingId = null, int? roomId = null, bool? hasPhoto = null, int page = 1)
    {
        const int pageSize = 50;

        var query = _db.Students
            .Include(s => s.Allocations.Where(a => a.Status == "Active"))
                .ThenInclude(a => a.CityRoom).ThenInclude(cr => cr.CityBuilding).ThenInclude(cb => cb.DormitoryCity)
            .Where(s => s.IsDeleted != true)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(s => s.FullName.Contains(search) || s.NationalID.Contains(search));
        if (cityId.HasValue)
            query = query.Where(s => s.Allocations.Any(a => a.CityRoom.CityBuilding.DormitoryCityID == cityId.Value));
        if (buildingId.HasValue)
            query = query.Where(s => s.Allocations.Any(a => a.CityRoom.CityBuildingID == buildingId.Value));
        if (roomId.HasValue)
            query = query.Where(s => s.Allocations.Any(a => a.CityRoomID == roomId.Value));
        if (hasPhoto == true)
            query = query.Where(s => s.Photo != null && s.Photo != "");
        else if (hasPhoto == false)
            query = query.Where(s => s.Photo == null || s.Photo == "");

        var total = await query.CountAsync();
        var students = await query.OrderBy(s => s.FullName)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var rows = students.Select(s =>
        {
            var alloc = s.Allocations.FirstOrDefault(a => a.Status == "Active");
            return new SelectableStudentRow
            {
                StudentID = s.ID,
                Selected = false,
                FullName = s.FullName,
                NationalID = s.NationalID,
                Faculty = s.Faculty,
                CityName = alloc?.CityRoom?.CityBuilding?.DormitoryCity?.Name,
                BuildingName = alloc?.CityRoom?.CityBuilding?.BuildingName,
                RoomNumber = alloc?.CityRoom?.RoomNumber,
                HasPhoto = !string.IsNullOrEmpty(s.Photo)
            };
        }).ToList();

        var cities = await _db.DormitoryCities.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();

        var vm = new PrintSelectionViewModel
        {
            Students = rows,
            Filter = new SelectionFilterViewModel
            {
                Search = search, CityID = cityId, BuildingID = buildingId, RoomID = roomId, HasPhoto = hasPhoto
            },
            TotalCount = total,
            Page = page,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            Cities = cities.Select(c => new CityLookup { ID = c.ID, Name = c.Name }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("Cards.Print", "CanEdit")]
    public async Task<IActionResult> AddToQueue(List<int> studentIds)
    {
        if (studentIds == null || studentIds.Count == 0)
        {
            TempData["Error"] = "يرجى اختيار طالب واحد على الأقل";
            return RedirectToAction("PrintSelection");
        }

        await _cardService.AddToPrintQueueAsync(studentIds, CurrentUserId);
        TempData["Success"] = $"تمت إضافة {studentIds.Count} طالب إلى قائمة الطباعة";
        return RedirectToAction("Index");
    }

    [HttpGet]
    [RequirePermission("Cards.Print", "CanPrint")]
    public async Task<IActionResult> Print(int id)
    {
        var pdf = await _cardService.GenerateSingleCardPdfAsync(id);
        if (pdf.Length == 0) return NotFound();

        await _cardService.MarkAsPrintedAsync(id, CurrentUserId);
        return File(pdf, "application/pdf", $"Card_{id}.pdf");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("Cards.Print", "CanPrint")]
    public async Task<IActionResult> BatchPrint(List<int> queueIds)
    {
        if (queueIds == null || queueIds.Count == 0)
        {
            TempData["Error"] = "يرجى اختيار عنصر واحد على الأقل";
            return RedirectToAction("Index");
        }

        var items = await _db.CardPrintQueues.Where(q => queueIds.Contains(q.ID)).ToListAsync();
        var studentIds = items.Select(q => q.StudentID).ToList();

        var pdf = await _cardService.GenerateBatchCardPdfAsync(studentIds);
        if (pdf.Length == 0) return NotFound();

        foreach (var item in items)
            await _cardService.MarkAsPrintedAsync(item.ID, CurrentUserId);

        return File(pdf, "application/pdf", $"BatchCards_{DateTime.UtcNow:yyyyMMdd}.pdf");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("Cards.Manage", "CanEdit")]
    public async Task<IActionResult> MarkPrinted(int id)
    {
        await _cardService.MarkAsPrintedAsync(id, CurrentUserId);
        return Json(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("Cards.Manage", "CanEdit")]
    public async Task<IActionResult> MarkFailed(int id)
    {
        await _cardService.MarkAsFailedAsync(id);
        return Json(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("Cards.Manage", "CanEdit")]
    public async Task<IActionResult> RemoveFromQueue(int id)
    {
        var item = await _db.CardPrintQueues.FindAsync(new object[] { id });
        if (item is null) return Json(new { success = false });

        _db.CardPrintQueues.Remove(item);
        await _db.SaveChangesAsync();
        return Json(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("Cards.Print", "CanPrint")]
    public async Task<IActionResult> PrintSingleFromQueue(int id)
    {
        var item = await _db.CardPrintQueues.Include(q => q.Student).FirstOrDefaultAsync(q => q.ID == id);
        if (item == null) return NotFound();

        var pdf = await _cardService.GenerateSingleCardPdfAsync(item.StudentID);
        if (pdf.Length == 0) return NotFound();

        await _cardService.MarkAsPrintedAsync(id, CurrentUserId);

        return File(pdf, "application/pdf", $"Card_{item.StudentID}.pdf");
    }

    [HttpGet]
    [RequirePermission("Cards.View", "CanView")]
    public async Task<IActionResult> Preview(int studentId)
    {
        var pdf = await _cardService.GenerateSingleCardPdfAsync(studentId);
        if (pdf.Length == 0) return NotFound();

        return File(pdf, "application/pdf");
    }

    [HttpGet]
    [RequirePermission("Cards.View", "CanView")]
    public async Task<IActionResult> GetCities()
    {
        var cities = await _db.DormitoryCities.Where(c => c.IsActive).OrderBy(c => c.Name)
            .Select(c => new CityLookup { ID = c.ID, Name = c.Name }).ToListAsync();
        return Json(cities);
    }

    [HttpGet]
    [RequirePermission("Cards.View", "CanView")]
    public async Task<IActionResult> GetBuildings(int cityId)
    {
        var buildings = await _db.CityBuildings.Where(b => b.DormitoryCityID == cityId && b.IsActive)
            .OrderBy(b => b.BuildingName)
            .Select(b => new BuildingLookup { ID = b.ID, Name = b.BuildingName }).ToListAsync();
        return Json(buildings);
    }

    [HttpGet]
    [RequirePermission("Cards.View", "CanView")]
    public async Task<IActionResult> GetRooms(int buildingId)
    {
        var rooms = await _db.CityRooms.Where(r => r.CityBuildingID == buildingId && r.IsActive == true)
            .OrderBy(r => r.RoomNumber)
            .Select(r => new RoomLookup { ID = r.ID, RoomNumber = r.RoomNumber }).ToListAsync();
        return Json(rooms);
    }
}
