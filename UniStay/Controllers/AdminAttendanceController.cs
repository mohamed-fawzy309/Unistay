using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using UniStay.Data;
using UniStay.Models;
using UniStay.ViewModels.Attendance;
using UniStay.Services.Interfaces;

namespace UniStay.Controllers
{
    [Authorize(AuthenticationSchemes = "StaffCookie,AdminCookie")]
    public class AdminAttendanceController : Controller
    {
        private readonly AssuitDbContext _db;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AdminAttendanceController> _logger;
        private readonly IAuditService _audit;

        public AdminAttendanceController(AssuitDbContext db, IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<AdminAttendanceController> logger, IAuditService audit)
        {
            _db = db;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _audit = audit;
        }

        private HttpClient CreateFlaskClient()
        {
            var client = _httpClientFactory.CreateClient();
            var token = _configuration["AttendanceApi:InternalToken"];
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Add("X-Internal-Token", token);
            }
            return client;
        }

        [HttpGet]
        public async Task<IActionResult> Index(DateTime? date, string? studentName)
        {
            var filterDate = date ?? DateTime.Today;

            var totalStudents = await _db.Students
                .CountAsync(s => s.IsEnrolled == true && s.IsDeleted != true);

            var presentCount = await _db.AttendanceLogs
                .Where(l => l.RecognizedAt.HasValue && l.RecognizedAt.Value.Date == filterDate.Date)
                .Select(l => l.StudentID)
                .Distinct()
                .CountAsync();

            var activeSession = await _db.AttendanceSessions
                .FirstOrDefaultAsync(s => s.IsActive == true);

            var todayLogs = await _db.AttendanceLogs
                .Where(l => l.RecognizedAt.HasValue && l.RecognizedAt.Value.Date == filterDate.Date)
                .Include(l => l.Student)
                .ToListAsync();

            var studentIds = todayLogs.Select(l => l.StudentID).Distinct().ToList();
            var allocations = await _db.Allocations
                .Where(a => studentIds.Contains(a.StudentID) && a.Status == "Active")
                .Include(a => a.CityRoom)
                .ToListAsync();
            var roomMap = allocations
                .GroupBy(a => a.StudentID)
                .ToDictionary(g => g.Key, g => g.First().CityRoom?.RoomNumber ?? "N/A");

            var records = todayLogs
                .Where(l => string.IsNullOrEmpty(studentName) ||
                            l.Student.FullName.Contains(studentName, StringComparison.OrdinalIgnoreCase))
                .Select(l => new TodayAttendanceItemViewModel
                {
                    StudentID = l.StudentID,
                    StudentName = l.Student.FullName,
                    RoomNumber = roomMap.GetValueOrDefault(l.StudentID, "N/A"),
                    RecognizedAt = l.RecognizedAt!.Value,
                    Confidence = l.Confidence
                })
                .OrderByDescending(x => x.RecognizedAt)
                .ToList();

            var setting = await _db.AttendanceSettings
                .OrderByDescending(s => s.ID)
                .FirstOrDefaultAsync();

            var todayStart = filterDate.Date;
            var todayEnd = todayStart.AddDays(1);
            var apiLogsToday = await _db.AttendanceApiLogs
                .Where(al => al.CreatedAt >= todayStart && al.CreatedAt < todayEnd)
                .ToListAsync();

            var dashboardStats = new AttendanceDashboardStatsViewModel
            {
                RecognitionSuccessCount = apiLogsToday.Count(al => al.Status == "Success"),
                DuplicateAttemptsCount = apiLogsToday.Count(al => al.Message != null && al.Message.Contains("Duplicate", StringComparison.OrdinalIgnoreCase)),
                ApiFailuresCount = apiLogsToday.Count(al => al.Status == "Failed")
            };

            var vm = new AttendanceDashboardViewModel
            {
                PresentCount = presentCount,
                AbsentCount = totalStudents - presentCount,
                AttendancePercentage = totalStudents > 0
                    ? Math.Round((decimal)presentCount / totalStudents * 100, 1)
                    : 0,
                ActiveSession = activeSession?.SessionName,
                TodayRecords = records
            };

            ViewBag.FilterDate = filterDate;
            ViewBag.FilterStudentName = studentName;
            ViewBag.TotalStudents = totalStudents;
            ViewBag.Setting = setting;
            ViewBag.DashboardStats = dashboardStats;

            return View(vm);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> StartSessionAjax([FromBody] SessionStartRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.SessionName))
                return Json(new { message = "اسم الجلسة مطلوب" });

            if (request.SessionName.Length > 200)
                return Json(new { message = "اسم الجلسة طويل جدًا" });

            var activeSession = await _db.AttendanceSessions
                .FirstOrDefaultAsync(s => s.IsActive == true);

            if (activeSession != null)
            {
                activeSession.IsActive = false;
                activeSession.EndedAt = DateTime.Now;
                await _db.SaveChangesAsync();
            }

            var baseUrl = _configuration["FaceRecognition:BaseUrl"];

            try
            {
                var client = CreateFlaskClient();
                var response = await client.PostAsJsonAsync(
                    $"{baseUrl}/api/session/start",
                    new { sessionName = request.SessionName });

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Flask /api/session/start returned HTTP {StatusCode}: {Body}",
                        (int)response.StatusCode, body);
                    return Json(new { success = false, message = "تعذر بدء الجلسة في نظام التعرف" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Flask /api/session/start unreachable at {BaseUrl}", baseUrl);
                return Json(new { success = false, message = "تعذر الاتصال بنظام التعرف" });
            }

            var session = new AttendanceSession
            {
                SessionName = request.SessionName,
                StartedAt = DateTime.Now,
                IsActive = true
            };
            _db.AttendanceSessions.Add(session);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(0, "System", "Attendance.SessionStarted",
                "AttendanceSession", session.ID,
                null, new { session.SessionName, session.StartedAt });

            return Json(new { success = true, message = "تم بدء الجلسة بنجاح" });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> StopSessionAjax()
        {
            var activeSession = await _db.AttendanceSessions
                .FirstOrDefaultAsync(s => s.IsActive == true);

            if (activeSession == null)
                return Json(new { message = "لا توجد جلسة نشطة" });

            activeSession.IsActive = false;
            activeSession.EndedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            await _audit.LogAsync(0, "System", "Attendance.SessionStopped",
                "AttendanceSession", activeSession.ID,
                null, new { activeSession.SessionName, activeSession.EndedAt });

            var baseUrl = _configuration["FaceRecognition:BaseUrl"];

            try
            {
                var client = CreateFlaskClient();
                var response = await client.PostAsJsonAsync(
                    $"{baseUrl}/api/session/stop",
                    new { });

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Flask /api/session/stop returned HTTP {StatusCode}: {Body}",
                        (int)response.StatusCode, body);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Flask /api/session/stop unreachable at {BaseUrl}", baseUrl);
            }

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                await FinalizeAbsencesForSession(activeSession);
                await tx.CommitAsync();

                await _audit.LogAsync(0, "System", "Attendance.AbsenceProcessingCompleted",
                    "Absence", null,
                    null, new { activeSession.ID, activeSession.SessionName });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Absence finalization failed for session {SessionId}, transaction rolled back", activeSession.ID);

                await _audit.LogAsync(0, "System", "Attendance.AbsenceProcessingFailed",
                    "Absence", null,
                    null, new { activeSession.ID, activeSession.SessionName, error = ex.Message });

                return Json(new { message = "تم إيقاف الجلسة ولكن فشل احتساب الغياب" });
            }

            return Json(new { message = "تم إيقاف الجلسة واحتساب الغياب بنجاح" });
        }

        private async Task FinalizeAbsencesForSession(AttendanceSession session)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var sessionDate = session.StartedAt?.Date ?? DateTime.Today;

            var accommodatedStudents = await _db.Allocations
                .Where(a => a.Status == "Active" && (a.EndDate == null || a.EndDate >= today))
                .Include(a => a.CityRoom)
                    .ThenInclude(cr => cr.CityBuilding)
                        .ThenInclude(cb => cb.DormitoryCity)
                .ToListAsync();

            var presentStudentIds = await _db.AttendanceLogs
                .Where(l => l.SessionID == session.ID && l.RecognizedAt.HasValue)
                .Select(l => l.StudentID)
                .Distinct()
                .ToListAsync();

            var absentStudents = accommodatedStudents
                .Where(a => !presentStudentIds.Contains(a.StudentID))
                .ToList();

            var existingAbsences = await _db.Absences
                .Where(a => a.AbsenceDate == DateOnly.FromDateTime(sessionDate)
                    && a.AbsenceType == "Absence"
                    && a.Status == "Approved"
                    && a.Reason == "تغيب عن جلسة التعرف الآلي")
                .Select(a => a.StudentID)
                .ToListAsync();

            var newAbsences = new List<Absence>();
            foreach (var alloc in absentStudents)
            {
                if (existingAbsences.Contains(alloc.StudentID))
                    continue;

                newAbsences.Add(new Absence
                {
                    StudentID = alloc.StudentID,
                    DormitoryCityID = alloc.CityRoom.CityBuilding.DormitoryCityID,
                    AbsenceDate = DateOnly.FromDateTime(sessionDate),
                    AbsenceType = "Absence",
                    Status = "Approved",
                    RequestedBy = "Staff",
                    Reason = "تغيب عن جلسة التعرف الآلي",
                    CreatedAt = DateTime.Now
                });
            }

            if (newAbsences.Count > 0)
            {
                _db.Absences.AddRange(newAbsences);
                await _db.SaveChangesAsync();
                _logger.LogInformation("Finalized {Count} absences for session {SessionId} ({SessionName})",
                    newAbsences.Count, session.ID, session.SessionName);

                await _audit.LogAsync(0, "System", "Attendance.AttendanceFinalized",
                    "Absence", null,
                    null, new { sessionId = session.ID, sessionName = session.SessionName, absenceCount = newAbsences.Count });
            }
            else
            {
                _logger.LogInformation("No new absences to record for session {SessionId}", session.ID);

                await _audit.LogAsync(0, "System", "Attendance.AttendanceFinalized",
                    "Absence", null,
                    null, new { sessionId = session.ID, sessionName = session.SessionName, absenceCount = 0 });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ControlRoom()
        {
            var today = DateTime.Today;
            var totalStudents = await _db.Students
                .CountAsync(s => s.IsEnrolled == true && s.IsDeleted != true);

            var presentCount = await _db.AttendanceLogs
                .Where(l => l.RecognizedAt.HasValue && l.RecognizedAt.Value.Date == today)
                .Select(l => l.StudentID)
                .Distinct()
                .CountAsync();

            var activeSession = await _db.AttendanceSessions
                .FirstOrDefaultAsync(s => s.IsActive == true);

            var vm = new ControlRoomViewModel
            {
                PresentCount = presentCount,
                AbsentCount = totalStudents - presentCount,
                AttendancePercentage = totalStudents > 0
                    ? Math.Round((decimal)presentCount / totalStudents * 100, 1)
                    : 0,
                ActiveSession = activeSession?.SessionName,
                TotalStudents = totalStudents
            };

            ViewBag.FaceRecognitionUrl = _configuration["FaceRecognition:BaseUrl"];
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> GetStudentAccommodation(int studentId)
        {
            var allocation = await _db.Allocations
                .Where(a => a.StudentID == studentId && a.Status == "Active")
                .Include(a => a.Student)
                .Include(a => a.CityRoom)
                    .ThenInclude(cr => cr.CityBuilding)
                        .ThenInclude(cb => cb.DormitoryCity)
                .OrderByDescending(a => a.ID)
                .FirstOrDefaultAsync();

            if (allocation == null)
                return Json(new { found = false });

            return Json(new
            {
                found = true,
                studentID = studentId,
                studentName = allocation.Student.FullName,
                nationalID = allocation.Student.NationalID,
                city = allocation.CityRoom.CityBuilding.DormitoryCity.Name,
                building = allocation.CityRoom.CityBuilding.BuildingName,
                room = allocation.CityRoom.RoomNumber,
                bed = allocation.BedNumber.ToString(),
                hasPhoto = !string.IsNullOrEmpty(allocation.Student.Photo)
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetSessionSummary()
        {
            var today = DateTime.Today;
            var todayDateOnly = DateOnly.FromDateTime(today);

            var latestSession = await _db.AttendanceSessions
                .Where(s => s.StartedAt.HasValue && s.StartedAt.Value.Date == today)
                .OrderByDescending(s => s.ID)
                .FirstOrDefaultAsync();

            var totalStudents = await _db.Allocations
                .Where(a => a.Status == "Active" && (a.EndDate == null || a.EndDate >= todayDateOnly))
                .Select(a => a.StudentID)
                .Distinct()
                .CountAsync();

            var presentCount = latestSession != null
                ? await _db.AttendanceLogs
                    .Where(l => l.SessionID == latestSession.ID && l.RecognizedAt.HasValue)
                    .Select(l => l.StudentID)
                    .Distinct()
                    .CountAsync()
                : 0;

            var absentCount = totalStudents - presentCount;
            var attendancePercent = totalStudents > 0 ? Math.Round((decimal)presentCount / totalStudents * 100, 1) : 0;

            var duplicateCount = latestSession != null
                ? await _db.AttendanceApiLogs
                    .CountAsync(al => al.CreatedAt.HasValue && al.CreatedAt.Value.Date == today
                        && al.Status == "Failed"
                        && al.Message != null
                        && al.Message.Contains("Duplicate", StringComparison.OrdinalIgnoreCase))
                : 0;

            var flaskStats = new { facesDetected = 0, studentsRecognized = 0, unknownFaces = 0, cameraIndex = 0 };
            var baseUrl = _configuration["FaceRecognition:BaseUrl"];
            try
            {
                var client = CreateFlaskClient();
                var resp = await client.GetAsync($"{baseUrl}/api/status");
                if (resp.IsSuccessStatusCode)
                {
                    var json = await resp.Content.ReadAsStringAsync();
                    var data = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
                    flaskStats = new
                    {
                        facesDetected = data.TryGetProperty("facesDetected", out var fd) ? fd.GetInt32() : 0,
                        studentsRecognized = data.TryGetProperty("studentsRecognized", out var sr) ? sr.GetInt32() : 0,
                        unknownFaces = data.TryGetProperty("unknownFaces", out var uf) ? uf.GetInt32() : 0,
                        cameraIndex = data.TryGetProperty("cameraIndex", out var ci) ? ci.GetInt32() : 0,
                    };
                }
            }
            catch { }

            var recognitionAccuracy = flaskStats.facesDetected > 0
                ? Math.Round((decimal)flaskStats.studentsRecognized / flaskStats.facesDetected * 100, 1)
                : 0;

            var duration = "—";
            if (latestSession?.StartedAt != null && latestSession.EndedAt != null)
            {
                var diff = latestSession.EndedAt.Value - latestSession.StartedAt.Value;
                duration = $"{(int)diff.TotalHours}h {diff.Minutes}m";
            }

            var vm = new SessionSummaryViewModel
            {
                TotalStudents = totalStudents,
                PresentCount = presentCount,
                AbsentCount = absentCount,
                AttendancePercentage = attendancePercent,
                FacesDetected = flaskStats.facesDetected,
                StudentsRecognized = flaskStats.studentsRecognized,
                UnknownFaces = flaskStats.unknownFaces,
                RecognitionAccuracy = recognitionAccuracy,
                DuplicateAttempts = duplicateCount,
                CameraIndex = flaskStats.cameraIndex,
                SessionName = latestSession?.SessionName ?? "—",
                SessionId = latestSession?.ID,
                StartedAt = latestSession?.StartedAt,
                EndedAt = latestSession?.EndedAt,
                Duration = duration,
                HasAbsences = absentCount > 0,
                AbsenceCount = absentCount
            };

            if (latestSession != null)
            {
                await _audit.LogAsync(0, "System", "Attendance.SummaryGenerated",
                    "AttendanceSession", latestSession.ID,
                    null, new { latestSession.SessionName, totalStudents, presentCount, absentCount, attendancePercent });
            }

            return Json(vm);
        }

        [HttpGet]
        public async Task<IActionResult> GetFlaskStatus()
        {
            var baseUrl = _configuration["FaceRecognition:BaseUrl"];
            var client = CreateFlaskClient();
            try
            {
                var response = await client.GetAsync($"{baseUrl}/api/status");
                if (!response.IsSuccessStatusCode)
                    return Json(new { running = false });

                var json = await response.Content.ReadAsStringAsync();
                return Content(json, "application/json");
            }
            catch
            {
                return Json(new { running = false });
            }
        }

        [HttpGet]
        public async Task<IActionResult> PingFlask()
        {
            var baseUrl = _configuration["FaceRecognition:BaseUrl"];
            var client = CreateFlaskClient();
            try
            {
                var response = await client.GetAsync($"{baseUrl}/api/status");
                return Json(new { alive = response.IsSuccessStatusCode });
            }
            catch
            {
                return Json(new { alive = false });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRecognitionEvents()
        {
            var baseUrl = _configuration["FaceRecognition:BaseUrl"];
            var client = CreateFlaskClient();
            try
            {
                var response = await client.GetAsync($"{baseUrl}/api/events/recent");
                if (!response.IsSuccessStatusCode)
                    return Json(new List<ControlRoomEventItem>());

                var json = await response.Content.ReadAsStringAsync();
                return Content(json, "application/json");
            }
            catch
            {
                return Json(new List<ControlRoomEventItem>());
            }
        }

        [HttpGet]
        public async Task CameraStreamProxy()
        {
            var baseUrl = _configuration["FaceRecognition:BaseUrl"];
            var token = _configuration["AttendanceApi:InternalToken"];
            if (string.IsNullOrEmpty(token))
            {
                Response.StatusCode = 500;
                return;
            }
            var client = _httpClientFactory.CreateClient();
            client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
            client.DefaultRequestHeaders.Add("X-Internal-Token", token);

            var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/api/camera/stream");
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                response.Dispose();
                Response.StatusCode = (int)response.StatusCode;
                return;
            }

            Response.ContentType = response.Content.Headers.ContentType?.ToString()
                ?? "multipart/x-mixed-replace; boundary=frame";

            var stream = await response.Content.ReadAsStreamAsync();
            try
            {
                await stream.CopyToAsync(Response.Body, 81920, HttpContext.RequestAborted);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                stream.Dispose();
                response.Dispose();
            }
        }

        [HttpGet]
        public async Task<IActionResult> CameraListProxy()
        {
            var baseUrl = _configuration["FaceRecognition:BaseUrl"];
            var client = CreateFlaskClient();
            try
            {
                var response = await client.GetAsync($"{baseUrl}/api/camera/list");
                if (!response.IsSuccessStatusCode)
                    return Json(new { cameras = new object[] { } });
                var json = await response.Content.ReadAsStringAsync();
                return Content(json, "application/json");
            }
            catch
            {
                return Json(new { cameras = new object[] { } });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> DevModeProxy([FromBody] System.Text.Json.JsonElement body)
        {
            var baseUrl = _configuration["FaceRecognition:BaseUrl"];
            var client = CreateFlaskClient();
            try
            {
                var response = await client.PostAsJsonAsync($"{baseUrl}/api/dev/mode", body);
                var json = await response.Content.ReadAsStringAsync();
                return Content(json, "application/json");
            }
            catch
            {
                return Json(new { devMode = false });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> CameraResolutionProxy([FromBody] System.Text.Json.JsonElement body)
        {
            var baseUrl = _configuration["FaceRecognition:BaseUrl"];
            var client = CreateFlaskClient();
            try
            {
                var response = await client.PostAsJsonAsync($"{baseUrl}/api/camera/resolution", body);
                var json = await response.Content.ReadAsStringAsync();
                return Content(json, "application/json");
            }
            catch
            {
                return Json(new { error = "proxy_error" });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> CameraQualityProxy([FromBody] System.Text.Json.JsonElement body)
        {
            var baseUrl = _configuration["FaceRecognition:BaseUrl"];
            var client = CreateFlaskClient();
            try
            {
                var response = await client.PostAsJsonAsync($"{baseUrl}/api/camera/quality", body);
                var json = await response.Content.ReadAsStringAsync();
                return Content(json, "application/json");
            }
            catch
            {
                return Json(new { error = "proxy_error" });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> CameraFpsProxy([FromBody] System.Text.Json.JsonElement body)
        {
            var baseUrl = _configuration["FaceRecognition:BaseUrl"];
            var client = CreateFlaskClient();
            try
            {
                var response = await client.PostAsJsonAsync($"{baseUrl}/api/camera/fps", body);
                var json = await response.Content.ReadAsStringAsync();
                return Content(json, "application/json");
            }
            catch
            {
                return Json(new { error = "proxy_error" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Enrollment()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            var allocations = await _db.Allocations
                .Where(a => a.Status == "Active" && (a.EndDate == null || a.EndDate >= today))
                .Include(a => a.Student)
                .Include(a => a.CityRoom)
                    .ThenInclude(cr => cr.CityBuilding)
                        .ThenInclude(cb => cb.DormitoryCity)
                .ToListAsync();

            var latestPerStudent = allocations
                .GroupBy(a => a.StudentID)
                .Select(g => g.OrderByDescending(a => a.ID).First())
                .OrderBy(a => a.CityRoom.CityBuilding.DormitoryCity.Name)
                .ThenBy(a => a.CityRoom.CityBuilding.BuildingName)
                .ThenBy(a => a.CityRoom.RoomNumber)
                .ThenBy(a => a.BedNumber)
                .ThenBy(a => a.Student.FullName)
                .ToList();

            var vm = new EnrollmentViewModel
            {
                TotalAccommodated = latestPerStudent.Count,
                Students = latestPerStudent.Select(a => new EnrollmentStudentItem
                {
                    StudentID = a.Student.ID,
                    FullName = a.Student.FullName,
                    NationalID = a.Student.NationalID,
                    DormitoryCity = a.CityRoom.CityBuilding.DormitoryCity.Name,
                    Building = a.CityRoom.CityBuilding.BuildingName,
                    Floor = a.CityRoom.FloorNumber,
                    RoomNumber = a.CityRoom.RoomNumber,
                    Bed = a.BedNumber,
                    Photo = a.Student.Photo
                }).ToList()
            };

            ViewBag.Cities = await _db.DormitoryCities
                .Where(c => c.IsActive && !c.IsDeleted)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> GetBuildingsByCity(int cityId)
        {
            var buildings = await _db.CityBuildings
                .Where(b => b.DormitoryCityID == cityId && b.IsActive && !b.IsDeleted)
                .OrderBy(b => b.BuildingName)
                .Select(b => new { b.ID, b.BuildingName })
                .ToListAsync();
            return Json(buildings);
        }

        [HttpGet]
        public async Task<IActionResult> GetEnrolledList()
        {
            var baseUrl = _configuration["FaceRecognition:BaseUrl"];
            var client = CreateFlaskClient();
            try
            {
                var response = await client.GetAsync($"{baseUrl}/api/enrollment/list");
                if (!response.IsSuccessStatusCode)
                    return Json(new { enrolledStudents = Array.Empty<string>() });

                var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                return Json(result);
            }
            catch
            {
                return Json(new { enrolledStudents = Array.Empty<string>() });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> RegisterFace([FromBody] FaceEnrollRequest request)
        {
            var baseUrl = _configuration["FaceRecognition:BaseUrl"];
            var client = CreateFlaskClient();
            try
            {
                var response = await client.PostAsJsonAsync(
                    $"{baseUrl}/api/enrollment/register", request);
                var body = await response.Content.ReadAsStringAsync();
                try
                {
                    var jsonResult = JsonSerializer.Deserialize<JsonElement>(body);
                    return StatusCode((int)response.StatusCode, jsonResult);
                }
                catch (JsonException)
                {
                    return StatusCode((int)response.StatusCode, JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(new { error = body })));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Face enrollment request failed");
                return StatusCode(500, JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(new { error = "تعذر الاتصال بنظام التعرف" })));
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> DeleteFace([FromBody] FaceDeleteRequest request)
        {
            var baseUrl = _configuration["FaceRecognition:BaseUrl"];
            var client = CreateFlaskClient();
            try
            {
                var response = await client.PostAsJsonAsync(
                    $"{baseUrl}/api/enrollment/delete", request);
                var body = await response.Content.ReadAsStringAsync();
                try
                {
                    var jsonResult = JsonSerializer.Deserialize<JsonElement>(body);
                    return StatusCode((int)response.StatusCode, jsonResult);
                }
                catch (JsonException)
                {
                    return StatusCode((int)response.StatusCode, JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(new { error = body })));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Face deletion request failed");
                return StatusCode(500, JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(new { error = "تعذر الاتصال بنظام التعرف" })));
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> TestFace([FromBody] FaceTestRequest request)
        {
            var baseUrl = _configuration["FaceRecognition:BaseUrl"];
            var client = CreateFlaskClient();
            try
            {
                var response = await client.PostAsJsonAsync(
                    $"{baseUrl}/api/enrollment/test", request);
                var body = await response.Content.ReadAsStringAsync();
                try
                {
                    var jsonResult = JsonSerializer.Deserialize<JsonElement>(body);
                    return StatusCode((int)response.StatusCode, jsonResult);
                }
                catch (JsonException)
                {
                    return StatusCode((int)response.StatusCode, JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(new { error = body })));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Face test request failed");
                return StatusCode(500, JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(new { error = "تعذر الاتصال بنظام التعرف" })));
            }
        }

        private string? FlaskPidFilePath =>
            System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(typeof(Program).Assembly.Location)!,
                "..", "..", "..", "..", "UniStay.FaceRecognition", "flask_pid.txt");

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> StartFlask()
        {
            var baseUrl = _configuration["FaceRecognition:BaseUrl"];
            var client = CreateFlaskClient();
            try
            {
                var resp = await client.GetAsync($"{baseUrl}/api/status");
                if (resp.IsSuccessStatusCode)
                    return Json(new { success = true, message = "نظام التعرف يعمل بالفعل" });
            }
            catch { }

            var flaskDir = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(typeof(Program).Assembly.Location)!,
                "..", "..", "..", "..", "UniStay.FaceRecognition");

            if (!Directory.Exists(flaskDir))
                return Json(new { success = false, message = "لم يتم العثور على مجلد نظام التعرف" });

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = "flask_api.py",
                    WorkingDirectory = flaskDir,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                var process = new Process { StartInfo = psi };
                process.Start();
                System.IO.File.WriteAllText(FlaskPidFilePath, process.Id.ToString());

                await Task.Delay(3000);

                try
                {
                    var resp = await client.GetAsync($"{baseUrl}/api/status");
                    if (resp.IsSuccessStatusCode)
                        return Json(new { success = true, message = "تم تشغيل نظام التعرف بنجاح" });
                }
                catch { }

                return Json(new { success = true, message = "تم بدء التشغيل. النظام قد يحتاج بضع ثوانٍ للاستجابة" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start Flask");
                return Json(new { success = false, message = "فشل تشغيل نظام التعرف" });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> StopFlask()
        {
            var baseUrl = _configuration["FaceRecognition:BaseUrl"];
            var client = CreateFlaskClient();

            try
            {
                var resp = await client.PostAsync($"{baseUrl}/api/shutdown", null);
                if (resp.IsSuccessStatusCode)
                {
                    await Task.Delay(1000);
                    CleanupFlaskPid();
                    return Json(new { success = true, message = "تم إيقاف نظام التعرف" });
                }
            }
            catch { }

            if (System.IO.File.Exists(FlaskPidFilePath))
            {
                var pid = int.Parse(System.IO.File.ReadAllText(FlaskPidFilePath).Trim());
                try
                {
                    var proc = Process.GetProcessById(pid);
                    proc.Kill(entireProcessTree: true);
                    CleanupFlaskPid();
                    await Task.Delay(500);
                    return Json(new { success = true, message = "تم إيقاف نظام التعرف" });
                }
                catch { }
            }

            return Json(new { success = false, message = "تعذر إيقاف النظام - يرجى إغلاق flask_api.py يدويًا" });
        }

        private void CleanupFlaskPid()
        {
            try { if (System.IO.File.Exists(FlaskPidFilePath)) System.IO.File.Delete(FlaskPidFilePath); } catch { }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSettings(string startTime, string endTime, decimal confidenceThreshold, bool isEnabled)
        {
            if (!TimeOnly.TryParse(startTime, out var start) || !TimeOnly.TryParse(endTime, out var end))
            {
                TempData["Error"] = "تنسيق الوقت غير صحيح";
                return RedirectToAction(nameof(Index));
            }

            if (confidenceThreshold < 0 || confidenceThreshold > 1)
            {
                TempData["Error"] = "حد الثقة يجب أن يكون بين 0 و 1";
                return RedirectToAction(nameof(Index));
            }

            var setting = await _db.AttendanceSettings
                .OrderByDescending(s => s.ID)
                .FirstOrDefaultAsync();

            if (setting == null)
            {
                _db.AttendanceSettings.Add(new AttendanceSetting
                {
                    StartTime = start,
                    EndTime = end,
                    ConfidenceThreshold = confidenceThreshold,
                    IsEnabled = isEnabled,
                    UpdatedAt = DateTime.Now
                });
            }
            else
            {
                setting.StartTime = start;
                setting.EndTime = end;
                setting.ConfidenceThreshold = confidenceThreshold;
                setting.IsEnabled = isEnabled;
                setting.UpdatedAt = DateTime.Now;
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "تم تحديث إعدادات الحضور بنجاح";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> DailyReport(DateTime? date, string? studentName, string? roomNumber, int? cityId)
        {
            var filterDate = date ?? DateTime.Today;

            var totalStudents = await _db.Students
                .CountAsync(s => s.IsEnrolled == true && s.IsDeleted != true);

            var presentStudentIds = await _db.AttendanceLogs
                .Where(l => l.RecognizedAt.HasValue && l.RecognizedAt.Value.Date == filterDate.Date)
                .Select(l => l.StudentID)
                .Distinct()
                .ToListAsync();

            var presentCount = presentStudentIds.Count;
            var absentCount = totalStudents - presentCount;

            var logs = await _db.AttendanceLogs
                .Where(l => l.RecognizedAt.HasValue && l.RecognizedAt.Value.Date == filterDate.Date)
                .Include(l => l.Student)
                .ToListAsync();

            var allStudentIds = logs.Select(l => l.StudentID).Distinct().ToList();
            var allocations = await _db.Allocations
                .Where(a => allStudentIds.Contains(a.StudentID) && a.Status == "Active")
                .Include(a => a.CityRoom)
                    .ThenInclude(cr => cr.CityBuilding)
                    .ThenInclude(cb => cb.DormitoryCity)
                .ToListAsync();

            var cityName = cityId.HasValue && cityId > 0
                ? await _db.DormitoryCities.Where(c => c.ID == cityId.Value).Select(c => c.Name).FirstOrDefaultAsync()
                : null;

            var rows = logs.Select(l => new DailyReportRowViewModel
            {
                StudentID = l.StudentID,
                StudentName = l.Student.FullName,
                NationalID = l.Student.NationalID,
                RoomNumber = allocations.FirstOrDefault(a => a.StudentID == l.StudentID)?.CityRoom?.RoomNumber ?? "N/A",
                CityName = allocations.FirstOrDefault(a => a.StudentID == l.StudentID)?.CityRoom?.CityBuilding?.DormitoryCity?.Name,
                RecognizedAt = l.RecognizedAt,
                Confidence = l.Confidence
            })
            .Where(r => string.IsNullOrEmpty(studentName) || r.StudentName.Contains(studentName, StringComparison.OrdinalIgnoreCase))
            .Where(r => string.IsNullOrEmpty(roomNumber) || r.RoomNumber.Contains(roomNumber, StringComparison.OrdinalIgnoreCase))
            .Where(r => string.IsNullOrEmpty(cityName) || r.CityName == cityName)
            .OrderByDescending(r => r.RecognizedAt)
            .ToList();

            var vm = new DailyReportViewModel
            {
                FilterDate = filterDate,
                FilterStudentName = studentName,
                FilterRoomNumber = roomNumber,
                FilterCityId = cityId,
                PresentCount = presentCount,
                AbsentCount = absentCount,
                AttendancePercentage = totalStudents > 0 ? Math.Round((decimal)presentCount / totalStudents * 100, 1) : 0,
                Rows = rows
            };

            ViewBag.Cities = await _db.DormitoryCities.OrderBy(c => c.Name).ToListAsync();
            ViewBag.FilterCityName = cityId.HasValue && cityId.Value > 0
                ? await _db.DormitoryCities.Where(c => c.ID == cityId.Value).Select(c => c.Name).FirstOrDefaultAsync()
                : null;

            return View(vm);
        }

        public async Task<IActionResult> DailyReportExportExcel(DateTime? date, string? studentName, string? roomNumber, int? cityId)
        {
            var filterDate = date ?? DateTime.Today;

            var filterCityName = cityId.HasValue && cityId.Value > 0
                ? await _db.DormitoryCities.Where(c => c.ID == cityId.Value).Select(c => c.Name).FirstOrDefaultAsync()
                : null;

            var logs = await _db.AttendanceLogs
                .Where(l => l.RecognizedAt.HasValue && l.RecognizedAt.Value.Date == filterDate.Date)
                .Include(l => l.Student)
                .ToListAsync();

            var studentIds = logs.Select(l => l.StudentID).Distinct().ToList();
            var allocations = await _db.Allocations
                .Where(a => studentIds.Contains(a.StudentID) && a.Status == "Active")
                .Include(a => a.CityRoom)
                    .ThenInclude(cr => cr.CityBuilding)
                    .ThenInclude(cb => cb.DormitoryCity)
                .ToListAsync();

            var cityName = cityId.HasValue && cityId > 0
                ? await _db.DormitoryCities.Where(c => c.ID == cityId.Value).Select(c => c.Name).FirstOrDefaultAsync()
                : null;

            var rows = logs.Select(l => new
            {
                l.Student.FullName,
                l.Student.NationalID,
                RoomNumber = allocations.FirstOrDefault(a => a.StudentID == l.StudentID)?.CityRoom?.RoomNumber ?? "N/A",
                CityName = allocations.FirstOrDefault(a => a.StudentID == l.StudentID)?.CityRoom?.CityBuilding?.DormitoryCity?.Name ?? "",
                l.RecognizedAt,
                l.Confidence
            })
            .Where(r => string.IsNullOrEmpty(studentName) || r.FullName.Contains(studentName, StringComparison.OrdinalIgnoreCase))
            .Where(r => string.IsNullOrEmpty(roomNumber) || r.RoomNumber.Contains(roomNumber, StringComparison.OrdinalIgnoreCase))
            .Where(r => string.IsNullOrEmpty(cityName) || r.CityName == cityName)
            .OrderByDescending(r => r.RecognizedAt)
            .ToList();

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var ws = workbook.Worksheets.Add($"DailyReport_{filterDate:yyyyMMdd}");

            ws.Cell(1, 1).Value = "اسم الطالب";
            ws.Cell(1, 2).Value = "الرقم القومي";
            ws.Cell(1, 3).Value = "رقم الغرفة";
            ws.Cell(1, 4).Value = "المدينة";
            ws.Cell(1, 5).Value = "وقت التعرف";
            ws.Cell(1, 6).Value = "نسبة الثقة";

            var headerRange = ws.Range(1, 1, 1, 6);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;

            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                ws.Cell(i + 2, 1).Value = r.FullName;
                ws.Cell(i + 2, 2).Value = r.NationalID;
                ws.Cell(i + 2, 3).Value = r.RoomNumber;
                ws.Cell(i + 2, 4).Value = r.CityName;
                ws.Cell(i + 2, 5).Value = r.RecognizedAt?.ToString("yyyy-MM-dd HH:mm:ss");
                ws.Cell(i + 2, 6).Value = r.Confidence?.ToString("P1");
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Seek(0, SeekOrigin.Begin);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"DailyReport_{filterDate:yyyyMMdd}.xlsx");
        }

        public async Task<IActionResult> DailyReportExportPdf(DateTime? date, string? studentName, string? roomNumber, int? cityId)
        {
            var filterDate = date ?? DateTime.Today;

            var filterCityName = cityId.HasValue && cityId.Value > 0
                ? await _db.DormitoryCities.Where(c => c.ID == cityId.Value).Select(c => c.Name).FirstOrDefaultAsync()
                : null;

            var logs = await _db.AttendanceLogs
                .Where(l => l.RecognizedAt.HasValue && l.RecognizedAt.Value.Date == filterDate.Date)
                .Include(l => l.Student)
                .ToListAsync();

            var studentIds = logs.Select(l => l.StudentID).Distinct().ToList();
            var allocations = await _db.Allocations
                .Where(a => studentIds.Contains(a.StudentID) && a.Status == "Active")
                .Include(a => a.CityRoom)
                    .ThenInclude(cr => cr.CityBuilding)
                    .ThenInclude(cb => cb.DormitoryCity)
                .ToListAsync();

            var cityName = cityId.HasValue && cityId > 0
                ? await _db.DormitoryCities.Where(c => c.ID == cityId.Value).Select(c => c.Name).FirstOrDefaultAsync()
                : null;

            var rows = logs.Select(l => new
            {
                l.Student.FullName,
                l.Student.NationalID,
                RoomNumber = allocations.FirstOrDefault(a => a.StudentID == l.StudentID)?.CityRoom?.RoomNumber ?? "N/A",
                CityName = allocations.FirstOrDefault(a => a.StudentID == l.StudentID)?.CityRoom?.CityBuilding?.DormitoryCity?.Name ?? "",
                l.RecognizedAt,
                l.Confidence
            })
            .Where(r => string.IsNullOrEmpty(studentName) || r.FullName.Contains(studentName, StringComparison.OrdinalIgnoreCase))
            .Where(r => string.IsNullOrEmpty(roomNumber) || r.RoomNumber.Contains(roomNumber, StringComparison.OrdinalIgnoreCase))
            .Where(r => string.IsNullOrEmpty(cityName) || r.CityName == cityName)
            .OrderByDescending(r => r.RecognizedAt)
            .ToList();

            var doc = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(20);
                    // FIX: AlignCenter() على الـ container مش على الـ TextDescriptor
                    page.Header().AlignCenter().Text($"تقرير الحضور اليومي - {filterDate:yyyy/MM/dd}")
                        .FontSize(16).Bold();
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });
                        table.Header(header =>
                        {
                            header.Cell().Text("اسم الطالب").Bold();
                            header.Cell().Text("الرقم القومي").Bold();
                            header.Cell().Text("رقم الغرفة").Bold();
                            header.Cell().Text("المدينة").Bold();
                            header.Cell().Text("وقت التعرف").Bold();
                            header.Cell().Text("نسبة الثقة").Bold();
                        });
                        foreach (var r in rows)
                        {
                            table.Cell().Text(r.FullName);
                            table.Cell().Text(r.NationalID);
                            table.Cell().Text(r.RoomNumber);
                            table.Cell().Text(r.CityName);
                            table.Cell().Text(r.RecognizedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "");
                            table.Cell().Text(r.Confidence?.ToString("P1") ?? "");
                        }
                    });
                });
            });

            var pdfBytes = doc.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"DailyReport_{filterDate:yyyyMMdd}.pdf");
        }

        [HttpGet]
        public async Task<IActionResult> MonthlyReport(int? month, int? year, string? studentName)
        {
            var now = DateTime.Now;
            var filterMonth = month ?? now.Month;
            var filterYear = year ?? now.Year;

            var startDate = new DateTime(filterYear, filterMonth, 1);
            var endDate = startDate.AddMonths(1);

            var sessionDays = await _db.AttendanceSessions
                .Where(s => s.StartedAt >= startDate && s.StartedAt < endDate)
                .Select(s => s.StartedAt!.Value.Date)
                .Distinct()
                .CountAsync();

            var totalStudents = await _db.Students
                .CountAsync(s => s.IsEnrolled == true && s.IsDeleted != true);

            var studentsQuery = _db.Students
                .Where(s => s.IsEnrolled == true && s.IsDeleted != true);

            if (!string.IsNullOrEmpty(studentName))
                studentsQuery = studentsQuery.Where(s => s.FullName.Contains(studentName));

            var students = await studentsQuery.ToListAsync();

            var allocations = await _db.Allocations
                .Where(a => a.Status == "Active")
                .Include(a => a.CityRoom)
                .ToListAsync();
            var roomMap = allocations
                .GroupBy(a => a.StudentID)
                .ToDictionary(g => g.Key, g => g.First().CityRoom?.RoomNumber ?? "N/A");

            var logsInMonth = await _db.AttendanceLogs
                .Where(l => l.RecognizedAt.HasValue && l.RecognizedAt.Value >= startDate && l.RecognizedAt.Value < endDate)
                .ToListAsync();

            var presentDaysPerStudent = logsInMonth
                .GroupBy(l => l.StudentID)
                .ToDictionary(g => g.Key, g => g.Select(l => l.RecognizedAt!.Value.Date).Distinct().Count());

            var rows = students
                .Select(s =>
                {
                    var presentDays = presentDaysPerStudent.GetValueOrDefault(s.ID, 0);
                    return new MonthlyReportRowViewModel
                    {
                        StudentID = s.ID,
                        StudentName = s.FullName,
                        RoomNumber = roomMap.GetValueOrDefault(s.ID, "N/A"),
                        PresentDays = presentDays,
                        TotalDays = sessionDays,
                        Percentage = sessionDays > 0 ? Math.Round((decimal)presentDays / sessionDays * 100, 1) : 0
                    };
                })
                .OrderByDescending(r => r.Percentage)
                .ToList();

            var avgPercentage = rows.Count > 0 ? Math.Round(rows.Average(r => r.Percentage), 1) : 0;

            var vm = new MonthlyReportViewModel
            {
                FilterMonth = filterMonth,
                FilterYear = filterYear,
                FilterStudentName = studentName,
                TotalSessionDays = sessionDays,
                AverageAttendancePercentage = avgPercentage,
                Rows = rows
            };

            ViewBag.Months = Enumerable.Range(1, 12).Select(m => new { Value = m, Name = new DateTime(2000, m, 1).ToString("MMMM") });
            ViewBag.Years = Enumerable.Range(DateTime.Now.Year - 2, 5).ToList();

            return View(vm);
        }

        public async Task<IActionResult> MonthlyReportExportExcel(int? month, int? year, string? studentName)
        {
            var now = DateTime.Now;
            var filterMonth = month ?? now.Month;
            var filterYear = year ?? now.Year;

            var startDate = new DateTime(filterYear, filterMonth, 1);
            var endDate = startDate.AddMonths(1);

            var sessionDays = await _db.AttendanceSessions
                .Where(s => s.StartedAt >= startDate && s.StartedAt < endDate)
                .Select(s => s.StartedAt!.Value.Date)
                .Distinct()
                .CountAsync();

            var studentsQuery = _db.Students
                .Where(s => s.IsEnrolled == true && s.IsDeleted != true);

            if (!string.IsNullOrEmpty(studentName))
                studentsQuery = studentsQuery.Where(s => s.FullName.Contains(studentName));

            var students = await studentsQuery.ToListAsync();

            var allocations = await _db.Allocations
                .Where(a => a.Status == "Active")
                .Include(a => a.CityRoom)
                .ToListAsync();
            var roomMap = allocations
                .GroupBy(a => a.StudentID)
                .ToDictionary(g => g.Key, g => g.First().CityRoom?.RoomNumber ?? "N/A");

            var logsInMonth = await _db.AttendanceLogs
                .Where(l => l.RecognizedAt.HasValue && l.RecognizedAt.Value >= startDate && l.RecognizedAt.Value < endDate)
                .ToListAsync();

            var presentDaysPerStudent = logsInMonth
                .GroupBy(l => l.StudentID)
                .ToDictionary(g => g.Key, g => g.Select(l => l.RecognizedAt!.Value.Date).Distinct().Count());

            var rows = students
                .Select(s =>
                {
                    var presentDays = presentDaysPerStudent.GetValueOrDefault(s.ID, 0);
                    return new
                    {
                        s.FullName,
                        RoomNumber = roomMap.GetValueOrDefault(s.ID, "N/A"),
                        PresentDays = presentDays,
                        TotalDays = sessionDays,
                        Percentage = sessionDays > 0 ? Math.Round((decimal)presentDays / sessionDays * 100, 1) : 0
                    };
                })
                .OrderByDescending(r => r.Percentage)
                .ToList();

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var ws = workbook.Worksheets.Add($"Monthly_{filterYear}{filterMonth:D2}");

            ws.Cell(1, 1).Value = "اسم الطالب";
            ws.Cell(1, 2).Value = "رقم الغرفة";
            ws.Cell(1, 3).Value = "أيام الحضور";
            ws.Cell(1, 4).Value = "إجمالي أيام الجلسات";
            ws.Cell(1, 5).Value = "نسبة الحضور";

            var headerRange = ws.Range(1, 1, 1, 5);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;

            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                ws.Cell(i + 2, 1).Value = r.FullName;
                ws.Cell(i + 2, 2).Value = r.RoomNumber;
                ws.Cell(i + 2, 3).Value = r.PresentDays;
                ws.Cell(i + 2, 4).Value = r.TotalDays;
                ws.Cell(i + 2, 5).Value = r.Percentage.ToString("0.0") + "%";
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Seek(0, SeekOrigin.Begin);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"MonthlyReport_{filterYear}{filterMonth:D2}.xlsx");
        }

        public async Task<IActionResult> MonthlyReportExportPdf(int? month, int? year, string? studentName)
        {
            var now = DateTime.Now;
            var filterMonth = month ?? now.Month;
            var filterYear = year ?? now.Year;

            var startDate = new DateTime(filterYear, filterMonth, 1);
            var endDate = startDate.AddMonths(1);

            var sessionDays = await _db.AttendanceSessions
                .Where(s => s.StartedAt >= startDate && s.StartedAt < endDate)
                .Select(s => s.StartedAt!.Value.Date)
                .Distinct()
                .CountAsync();

            var studentsQuery = _db.Students
                .Where(s => s.IsEnrolled == true && s.IsDeleted != true);

            if (!string.IsNullOrEmpty(studentName))
                studentsQuery = studentsQuery.Where(s => s.FullName.Contains(studentName));

            var students = await studentsQuery.ToListAsync();

            var allocations = await _db.Allocations
                .Where(a => a.Status == "Active")
                .Include(a => a.CityRoom)
                .ToListAsync();
            var roomMap = allocations
                .GroupBy(a => a.StudentID)
                .ToDictionary(g => g.Key, g => g.First().CityRoom?.RoomNumber ?? "N/A");

            var logsInMonth = await _db.AttendanceLogs
                .Where(l => l.RecognizedAt.HasValue && l.RecognizedAt.Value >= startDate && l.RecognizedAt.Value < endDate)
                .ToListAsync();

            var presentDaysPerStudent = logsInMonth
                .GroupBy(l => l.StudentID)
                .ToDictionary(g => g.Key, g => g.Select(l => l.RecognizedAt!.Value.Date).Distinct().Count());

            var rows = students
                .Select(s =>
                {
                    var presentDays = presentDaysPerStudent.GetValueOrDefault(s.ID, 0);
                    return new
                    {
                        s.FullName,
                        RoomNumber = roomMap.GetValueOrDefault(s.ID, "N/A"),
                        PresentDays = presentDays,
                        TotalDays = sessionDays,
                        Percentage = sessionDays > 0 ? Math.Round((decimal)presentDays / sessionDays * 100, 1) : 0
                    };
                })
                .OrderByDescending(r => r.Percentage)
                .ToList();

            var doc = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(20);
                    // FIX: AlignCenter() على الـ container مش على الـ TextDescriptor
                    page.Header().AlignCenter().Text($"تقرير الحضور الشهري - {filterYear}/{filterMonth:D2}")
                        .FontSize(16).Bold();
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });
                        table.Header(header =>
                        {
                            header.Cell().Text("اسم الطالب").Bold();
                            header.Cell().Text("رقم الغرفة").Bold();
                            header.Cell().Text("أيام الحضور").Bold();
                            header.Cell().Text("إجمالي الأيام").Bold();
                            header.Cell().Text("النسبة").Bold();
                        });
                        foreach (var r in rows)
                        {
                            table.Cell().Text(r.FullName);
                            table.Cell().Text(r.RoomNumber);
                            table.Cell().Text(r.PresentDays.ToString());
                            table.Cell().Text(r.TotalDays.ToString());
                            table.Cell().Text(r.Percentage.ToString("0.0") + "%");
                        }
                    });
                });
            });

            var pdfBytes = doc.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"MonthlyReport_{filterYear}{filterMonth:D2}.pdf");
        }

        [HttpGet]
        public async Task<IActionResult> GetLatestRecords(DateTime? date, int? lastId)
        {
            var filterDate = date ?? DateTime.Today;

            var logs = await _db.AttendanceLogs
                .Where(l => l.RecognizedAt.HasValue && l.RecognizedAt.Value.Date == filterDate.Date
                    && (!lastId.HasValue || l.ID > lastId.Value))
                .Include(l => l.Student)
                .OrderByDescending(l => l.RecognizedAt)
                .Take(50)
                .ToListAsync();

            var studentIds = logs.Select(l => l.StudentID).Distinct().ToList();
            var allocations = await _db.Allocations
                .Where(a => studentIds.Contains(a.StudentID) && a.Status == "Active")
                .Include(a => a.CityRoom)
                .ToListAsync();
            var roomMap = allocations
                .GroupBy(a => a.StudentID)
                .ToDictionary(g => g.Key, g => g.First().CityRoom?.RoomNumber ?? "N/A");

            var records = logs.Select(l => new
            {
                id = l.ID,
                studentName = l.Student.FullName,
                roomNumber = roomMap.GetValueOrDefault(l.StudentID, "N/A"),
                recognizedAt = l.RecognizedAt?.ToString("HH:mm:ss"),
                confidence = l.Confidence?.ToString("P1") ?? "N/A",
                confidenceValue = l.Confidence ?? 0
            }).ToList();

            return Json(new { records, maxId = logs.Any() ? logs.Max(l => l.ID) : (lastId ?? 0) });
        }

        [HttpGet]
        public async Task<IActionResult> Monitoring()
        {
            var todayStart = DateTime.Today;
            var todayEnd = todayStart.AddDays(1);

            var todayLogs = await _db.AttendanceApiLogs
                .Where(al => al.CreatedAt >= todayStart && al.CreatedAt < todayEnd)
                .OrderByDescending(al => al.ID)
                .ToListAsync();

            var vm = new MonitoringDashboardViewModel
            {
                SuccessCount = todayLogs.Count(al => al.Status == "Success"),
                FailedCount = todayLogs.Count(al => al.Status == "Failed"),
                DuplicateCount = todayLogs.Count(al => al.Message != null && al.Message.Contains("Duplicate", StringComparison.OrdinalIgnoreCase)),
                LastErrorMessage = todayLogs.FirstOrDefault(al => al.Status == "Failed")?.Message,
                LastErrorTime = todayLogs.FirstOrDefault(al => al.Status == "Failed")?.CreatedAt,
                RecentLogs = todayLogs.Select(al => new MonitoringLogItemViewModel
                {
                    ID = al.ID,
                    StudentID = al.StudentID,
                    Status = al.Status,
                    Message = al.Message,
                    CreatedAt = al.CreatedAt
                }).ToList()
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> SessionSummaryExportExcel()
        {
            var today = DateTime.Today;
            var todayDateOnly = DateOnly.FromDateTime(today);

            var session = await _db.AttendanceSessions
                .Where(s => s.StartedAt.HasValue && s.StartedAt.Value.Date == today)
                .OrderByDescending(s => s.ID)
                .FirstOrDefaultAsync();

            var accommodated = await _db.Allocations
                .Where(a => a.Status == "Active" && (a.EndDate == null || a.EndDate >= todayDateOnly))
                .Include(a => a.Student)
                .Include(a => a.CityRoom)
                .ToListAsync();

            var presentIds = session != null
                ? await _db.AttendanceLogs
                    .Where(l => l.SessionID == session.ID && l.RecognizedAt.HasValue)
                    .Select(l => l.StudentID)
                    .Distinct()
                    .ToListAsync()
                : new List<int>();

            var logTimeMap = new Dictionary<int, DateTime?>();
            if (session != null)
            {
                var logTimes = await _db.AttendanceLogs
                    .Where(l => l.SessionID == session.ID && l.RecognizedAt.HasValue)
                    .Select(l => new { l.StudentID, l.RecognizedAt })
                    .ToListAsync();

                logTimeMap = logTimes
                    .GroupBy(x => x.StudentID)
                    .ToDictionary(g => g.Key, g => g.First().RecognizedAt);
            }

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var ws = workbook.Worksheets.Add($"SessionSummary_{today:yyyyMMdd}");

            ws.Cell(1, 1).Value = "رقم الطالب";
            ws.Cell(1, 2).Value = "اسم الطالب";
            ws.Cell(1, 3).Value = "رقم الغرفة";
            ws.Cell(1, 4).Value = "الحالة";
            ws.Cell(1, 5).Value = "وقت التسجيل";

            var headerRange = ws.Range(1, 1, 1, 5);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;

            int row = 2;
            foreach (var alloc in accommodated.OrderBy(a => a.Student.FullName))
            {
                var isPresent = presentIds.Contains(alloc.StudentID);
                var time = isPresent && logTimeMap.TryGetValue(alloc.StudentID, out var t) ? t : null;

                ws.Cell(row, 1).Value = alloc.StudentID;
                ws.Cell(row, 2).Value = alloc.Student.FullName;
                ws.Cell(row, 3).Value = alloc.CityRoom.RoomNumber;
                ws.Cell(row, 4).Value = isPresent ? "حاضر" : "غائب";
                ws.Cell(row, 5).Value = time?.ToString("HH:mm:ss") ?? "—";
                row++;
            }

            ws.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Seek(0, SeekOrigin.Begin);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"SessionSummary_{today:yyyyMMdd}.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> SessionSummaryExportPdf()
        {
            var today = DateTime.Today;
            var todayDateOnly = DateOnly.FromDateTime(today);

            var session = await _db.AttendanceSessions
                .Where(s => s.StartedAt.HasValue && s.StartedAt.Value.Date == today)
                .OrderByDescending(s => s.ID)
                .FirstOrDefaultAsync();

            var accommodated = await _db.Allocations
                .Where(a => a.Status == "Active" && (a.EndDate == null || a.EndDate >= todayDateOnly))
                .Include(a => a.Student)
                .Include(a => a.CityRoom)
                .ToListAsync();

            var presentIds = session != null
                ? await _db.AttendanceLogs
                    .Where(l => l.SessionID == session.ID && l.RecognizedAt.HasValue)
                    .Select(l => l.StudentID)
                    .Distinct()
                    .ToListAsync()
                : new List<int>();

            var rows = accommodated
                .OrderBy(a => a.Student.FullName)
                .Select(a => new
                {
                    a.StudentID,
                    a.Student.FullName,
                    Room = a.CityRoom.RoomNumber,
                    Status = presentIds.Contains(a.StudentID) ? "حاضر" : "غائب"
                })
                .ToList();

            var present = rows.Count(r => r.Status == "حاضر");
            var absent = rows.Count - present;
            var pct = rows.Count > 0 ? Math.Round((decimal)present / rows.Count * 100, 1) : 0;

            var doc = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(20);
                    page.Header().AlignCenter().Text($"ملخص الجلسة - {today:yyyy/MM/dd}")
                        .FontSize(16).Bold();

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            c.RelativeColumn(3);
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });
                        table.Header(header =>
                        {
                            header.Cell().Text("الرقم").Bold();
                            header.Cell().Text("الاسم").Bold();
                            header.Cell().Text("الغرفة").Bold();
                            header.Cell().Text("الحالة").Bold();
                        });
                        foreach (var r in rows)
                        {
                            table.Cell().Text(r.StudentID.ToString());
                            table.Cell().Text(r.FullName);
                            table.Cell().Text(r.Room);
                            table.Cell().Text(r.Status);
                        }
                    });

                    page.Footer().AlignLeft().Text($"الإجمالي: {rows.Count} | حاضر: {present} | غائب: {absent} | النسبة: {pct}%")
                        .FontSize(10);
                });
            });

            var pdfBytes = doc.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"SessionSummary_{today:yyyyMMdd}.pdf");
        }
    }
}