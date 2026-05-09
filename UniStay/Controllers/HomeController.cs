using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Models;

namespace UniStay.Controllers
{
    public class HomeController : Controller
    {
        private readonly AssuitDbContext _db;

        public HomeController(AssuitDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var announcements = await _db.Announcements
                .Where(a => a.IsPublished == true && (!a.ExpiresAt.HasValue || a.ExpiresAt > DateTime.UtcNow))
                .OrderByDescending(a => a.PublishedAt)
                .Take(5)
                .ToListAsync();

            var cities = await _db.DormitoryCities
                .Where(c => c.IsActive && !c.IsDeleted)
                .ToListAsync();

            ViewBag.Announcements = announcements;
            ViewBag.Cities = cities;
            return View();
        }

        public async Task<IActionResult> Announcements(int page = 1)
        {
            var query = _db.Announcements
                .Where(a => a.IsPublished == true && (!a.ExpiresAt.HasValue || a.ExpiresAt > DateTime.UtcNow));

            var total = await query.CountAsync();

            var announcements = await query
                .OrderByDescending(a => a.PublishedAt)
                .Skip((page - 1) * 12)
                .Take(12)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / 12.0);

            return View(announcements);
        }

        public async Task<IActionResult> Cities()
        {
            var cities = await _db.DormitoryCities
                .Include(c => c.CityBuildings)
                .Where(c => c.IsActive && !c.IsDeleted)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(cities);
        }

        public async Task<IActionResult> CityDetails(int id)
        {
            var city = await _db.DormitoryCities
                .Include(c => c.CityBuildings)
                    .ThenInclude(b => b.CityRooms)
                .Include(c => c.University)
                .FirstOrDefaultAsync(c => c.ID == id && c.IsActive && !c.IsDeleted);

            if (city == null) return NotFound();

            ViewBag.TotalBuildings = city.CityBuildings.Count;
            ViewBag.TotalRooms = city.CityBuildings.Sum(b => b.CityRooms.Count);
            ViewBag.TotalBeds = city.CityBuildings.Sum(b => b.CityRooms.Sum(r => r.BedsCount));
            ViewBag.OccupiedBeds = city.CityBuildings.Sum(b => b.CityRooms.Sum(r => r.CurrentOccupancy));

            return View(city);
        }

        public IActionResult About()
        {
            return View();
        }

        public async Task<IActionResult> Dates()
        {
            var schedules = await _db.ApplicationSchedules
                .Include(s => s.DormitoryCity)
                .OrderByDescending(s => s.AcademicYear)
                .ThenBy(s => s.DormitoryCity.Name)
                .ToListAsync();

            ViewBag.Schedules = schedules;
            return View();
        }

        public IActionResult Accept()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> CheckAcceptance(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Length != 14)
                return Json(new { status = "invalid" });

            var student = await _db.Students
                .FirstOrDefaultAsync(s => s.NationalID == id && s.IsDeleted != true);

            if (student == null)
                return Json(new { status = "not_found" });

            var latestApp = await _db.Applications
                .Where(a => a.StudentID == student.ID)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            if (latestApp == null)
                return Json(new { status = "not_found" });

            var status = latestApp.Status switch
            {
                "Accepted" or "Allocated" => "accepted",
                "UnderReview" or "Pending" => "under_review",
                _ => "not_found"
            };

            return Json(new
            {
                status,
                name = student.FullName,
                faculty = student.Faculty ?? "—",
                grade = student.GradeText ?? student.AcademicYear?.ToString() ?? "—",
                studyType = latestApp.StudentType == "New" ? "طالب جديد" : "طالب قديم",
                seat = student.StudentCode ?? "—"
            });
        }
    }
}
