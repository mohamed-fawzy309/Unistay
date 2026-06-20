using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Helpers;
using UniStay.Models;
using UniStay.Services.Interfaces;
using UniStay.ViewModels.Maintenance;

namespace UniStay.Controllers
{
    [Authorize(AuthenticationSchemes = "StaffCookie,AdminCookie")]
    public class MaintenanceController : Controller
    {
        private readonly AssuitDbContext _db;
        private readonly IAuditService _audit;
        private readonly IEmailService _email;

        public MaintenanceController(AssuitDbContext db, IAuditService audit, IEmailService email)
        {
            _db = db;
            _audit = audit;
            _email = email;
        }

        private int CurrentUserId => int.Parse(User.FindFirst("UserID")!.Value);

        [HttpGet]
        public async Task<IActionResult> Index(string? filterStatus = null)
        {
            var query = _db.MaintenanceRequests
                .Include(r => r.Student)
                .Include(r => r.CityRoom)
                    .ThenInclude(r => r.CityBuilding)
                .Include(r => r.AssignedToNavigation)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filterStatus) && filterStatus != "All")
                query = query.Where(r => r.Status == filterStatus);

            var requests = await query
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new MaintenanceRowViewModel
                {
                    ID = r.ID,
                    StudentName = r.Student.FullName,
                    RoomNumber = r.CityRoom.RoomNumber,
                    BuildingName = r.CityRoom.CityBuilding.BuildingName,
                    Category = r.Category,
                    Description = r.Description,
                    Priority = r.Priority,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    AssignedToName = r.AssignedToNavigation != null ? r.AssignedToNavigation.Name : null
                })
                .ToListAsync();

            var vm = new MaintenanceListViewModel
            {
                Requests = requests,
                FilterStatus = filterStatus
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Students = await _db.Students
                .Where(s => s.IsDeleted != true)
                .OrderBy(s => s.FullName)
                .Select(s => new StudentLookupItem
                {
                    ID = s.ID,
                    FullName = s.FullName,
                    NationalID = s.NationalID
                })
                .ToListAsync();

            ViewBag.Cities = await _db.DormitoryCities
                .Where(c => c.IsActive && !c.IsDeleted)
                .OrderBy(c => c.Name)
                .Select(c => new CityLookupItem { ID = c.ID, Name = c.Name })
                .ToListAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateMaintenanceViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Students = await _db.Students
                    .Where(s => s.IsDeleted != true)
                    .OrderBy(s => s.FullName)
                    .Select(s => new StudentLookupItem
                    {
                        ID = s.ID,
                        FullName = s.FullName,
                        NationalID = s.NationalID
                    })
                    .ToListAsync();

                ViewBag.Cities = await _db.DormitoryCities
                    .Where(c => c.IsActive && !c.IsDeleted)
                    .OrderBy(c => c.Name)
                    .Select(c => new CityLookupItem { ID = c.ID, Name = c.Name })
                    .ToListAsync();

                return View(model);
            }

            var request = new MaintenanceRequest
            {
                StudentID = model.StudentID,
                CityRoomID = model.CityRoomID,
                DormitoryCityID = model.DormitoryCityID,
                Category = model.Category,
                Description = model.Description,
                Priority = model.Priority,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _db.MaintenanceRequests.Add(request);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Maintenance.Create", "MaintenanceRequest", request.ID,
                null, new { request.StudentID, request.CityRoomID, request.Category, request.Priority });

            TempData["Success"] = "تم إنشاء طلب الصيانة بنجاح";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign([FromBody] AssignMaintenanceViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "بيانات غير صحيحة" });

            var request = await _db.MaintenanceRequests.FindAsync(model.RequestID);
            if (request == null)
                return Json(new { success = false, message = "طلب الصيانة غير موجود" });

            if (request.Status != "Pending")
                return Json(new { success = false, message = "يمكن فقط تعيين الطلبات المعلقة" });

            var oldStatus = request.Status;
            request.AssignedTo = model.StaffUserID;
            request.AssignedAt = DateTime.UtcNow;
            request.Status = "Assigned";

            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Maintenance.Assign", "MaintenanceRequest", model.RequestID,
                new { Status = oldStatus, AssignedTo = (int?)null },
                new { Status = "Assigned", AssignedTo = model.StaffUserID });

            return Json(new { success = true, message = "تم تعيين طلب الصيانة بنجاح" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateStatusViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "بيانات غير صحيحة" });

            var validStatuses = new[] { "Pending", "Assigned", "InProgress", "OnHold", "Completed", "Cancelled" };
            if (!validStatuses.Contains(model.NewStatus))
                return Json(new { success = false, message = "حالة غير صالحة" });

            var request = await _db.MaintenanceRequests.FindAsync(model.RequestID);
            if (request == null)
                return Json(new { success = false, message = "طلب الصيانة غير موجود" });

            var oldStatus = request.Status;
            request.Status = model.NewStatus;

            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Maintenance.UpdateStatus", "MaintenanceRequest", model.RequestID,
                new { Status = oldStatus }, new { Status = model.NewStatus });

            return Json(new { success = true, message = "تم تحديث حالة الطلب" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete([FromBody] CompleteMaintenanceViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "بيانات غير صحيحة" });

            var request = await _db.MaintenanceRequests
                .Include(r => r.Student)
                .FirstOrDefaultAsync(r => r.ID == model.RequestID);

            if (request == null)
                return Json(new { success = false, message = "طلب الصيانة غير موجود" });

            var oldStatus = request.Status;
            request.Status = "Completed";
            request.CompletedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Maintenance.Complete", "MaintenanceRequest", model.RequestID,
                new { Status = oldStatus }, new { Status = "Completed", Notes = model.CompletionNotes });

            if (request.Student != null && !string.IsNullOrEmpty(request.Student.Email))
            {
                var subject = "تم إكمال طلب الصيانة - UniStay";
                var body = $"<h3>تم إكمال طلب الصيانة</h3><p>عزيزي {request.Student.FullName}، تم إكمال طلب الصيانة الخاص بك.</p>";
                await _email.SendAsync(request.Student.Email, subject, body, EmailType.General, request.Student.ID);
            }

            return Json(new { success = true, message = "تم إكمال طلب الصيانة" });
        }

        [HttpGet]
        public async Task<IActionResult> GetStudents(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(new List<StudentLookupItem>());

            var students = await _db.Students
                .Where(s => s.IsDeleted != true &&
                    (s.FullName.Contains(term) || s.NationalID.Contains(term)))
                .OrderBy(s => s.FullName)
                .Take(20)
                .Select(s => new StudentLookupItem
                {
                    ID = s.ID,
                    FullName = s.FullName,
                    NationalID = s.NationalID
                })
                .ToListAsync();

            return Json(students);
        }

        [HttpGet]
        public async Task<IActionResult> GetRoomsByCity(int cityId)
        {
            var rooms = await _db.CityRooms
                .Include(r => r.CityBuilding)
                .Where(r => r.CityBuilding.DormitoryCityID == cityId && r.IsDeleted != true)
                .OrderBy(r => r.CityBuilding.BuildingName)
                .ThenBy(r => r.RoomNumber)
                .Select(r => new
                {
                    r.ID,
                    r.RoomNumber,
                    floorNumber = (int)r.FloorNumber,
                    buildingName = r.CityBuilding.BuildingName,
                    bedsCount = (int)r.BedsCount,
                    currentOccupancy = (int)r.CurrentOccupancy
                })
                .ToListAsync();

            return Json(rooms);
        }

        [HttpGet]
        public async Task<IActionResult> GetStaffUsers()
        {
            var users = await _db.SystemUsers
                .Where(u => u.IsActive && !u.IsDeleted)
                .OrderBy(u => u.Name)
                .Select(u => new { u.ID, u.Name, u.Email })
                .ToListAsync();

            return Json(users);
        }
    }
}
