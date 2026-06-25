using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Models;
using UniStay.ViewModels.Attendance;

namespace UniStay.Controllers;

[Route("api/attendance")]
[IgnoreAntiforgeryToken]
public class AttendanceApiController : Controller
{
    private readonly AssuitDbContext _db;
    private readonly IConfiguration _configuration;

    public AttendanceApiController(AssuitDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    private bool IsValidToken(string? token)
    {
        if (string.IsNullOrEmpty(token)) return false;
        var expected = _configuration["AttendanceApi:InternalToken"];
        return string.Equals(token, expected, StringComparison.Ordinal);
    }

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        var setting = await _db.AttendanceSettings
            .OrderByDescending(s => s.ID)
            .FirstOrDefaultAsync();

        if (setting == null)
            return NotFound(new { message = "No attendance settings configured" });

        return Ok(new AttendanceSettingsResponse
        {
            StartTime = setting.StartTime,
            EndTime = setting.EndTime,
            ConfidenceThreshold = setting.ConfidenceThreshold,
            IsEnabled = setting.IsEnabled
        });
    }

    [HttpPost("checkin")]
    public async Task<IActionResult> Checkin([FromBody] AttendanceCheckinRequest request)
    {
        var token = HttpContext.Request.Headers["X-Internal-Token"].FirstOrDefault();
        if (!IsValidToken(token))
            return Unauthorized(new { message = "Invalid or missing token" });

        if (request.StudentID <= 0)
        {
            await LogApiCall(null, "Failed", "Invalid student ID");
            return BadRequest(new { message = "Invalid student ID" });
        }

        if (request.Confidence.HasValue && (request.Confidence.Value < 0 || request.Confidence.Value > 1))
        {
            await LogApiCall(request.StudentID, "Failed", "Confidence out of range [0,1]");
            return BadRequest(new { message = "Confidence must be between 0 and 1" });
        }

        if (request.Timestamp.HasValue && request.Timestamp.Value > DateTime.Now)
        {
            await LogApiCall(request.StudentID, "Failed", "Timestamp in the future");
            return BadRequest(new { message = "Timestamp cannot be in the future" });
        }

        var activeSession = await _db.AttendanceSessions
            .FirstOrDefaultAsync(s => s.IsActive == true);

        if (activeSession == null)
        {
            await LogApiCall(request.StudentID, "Failed", "No active attendance session");
            return BadRequest(new { message = "No active attendance session" });
        }

        var exists = await _db.AttendanceLogs
            .AnyAsync(l => l.StudentID == request.StudentID && l.SessionID == activeSession.ID);

        if (exists)
        {
            await LogApiCall(request.StudentID, "Failed", "Duplicate attendance record");
            return Conflict(new { message = "Student already checked in for this session" });
        }

        var log = new AttendanceLog
        {
            StudentID = request.StudentID,
            SessionID = activeSession.ID,
            RecognizedAt = request.Timestamp ?? DateTime.Now,
            Confidence = request.Confidence
        };

        _db.AttendanceLogs.Add(log);
        await LogApiCall(request.StudentID, "Success", "Checkin recorded");

        return Ok(new { message = "Checkin successful", attendanceLogID = log.ID });
    }

    [HttpPost("session/start")]
    public async Task<IActionResult> StartSession([FromBody] SessionStartRequest request)
    {
        var token = HttpContext.Request.Headers["X-Internal-Token"].FirstOrDefault();
        if (!IsValidToken(token))
            return Unauthorized(new { message = "Invalid or missing token" });

        if (request == null || string.IsNullOrWhiteSpace(request.SessionName))
            return BadRequest(new { message = "Session name is required" });

        if (request.SessionName.Length > 200)
            return BadRequest(new { message = "Session name too long (max 200 characters)" });

        var activeSession = await _db.AttendanceSessions
            .FirstOrDefaultAsync(s => s.IsActive == true);

        if (activeSession != null)
        {
            activeSession.IsActive = false;
            activeSession.EndedAt = DateTime.Now;
        }

        var session = new AttendanceSession
        {
            SessionName = request.SessionName,
            StartedAt = DateTime.Now,
            IsActive = true
        };

        _db.AttendanceSessions.Add(session);
        await _db.SaveChangesAsync();

        await LogApiCall(null, "Success", $"Session '{request.SessionName}' started");

        return Ok(new { message = "Session started", sessionID = session.ID });
    }

    [HttpPost("session/stop")]
    public async Task<IActionResult> StopSession()
    {
        var token = HttpContext.Request.Headers["X-Internal-Token"].FirstOrDefault();
        if (!IsValidToken(token))
            return Unauthorized(new { message = "Invalid or missing token" });

        var activeSession = await _db.AttendanceSessions
            .FirstOrDefaultAsync(s => s.IsActive == true);

        if (activeSession == null)
        {
            await LogApiCall(null, "Failed", "No active session to stop");
            return BadRequest(new { message = "No active session to stop" });
        }

        activeSession.IsActive = false;
        activeSession.EndedAt = DateTime.Now;
        await _db.SaveChangesAsync();

        await LogApiCall(null, "Success", $"Session '{activeSession.SessionName}' stopped");

        return Ok(new { message = "Session stopped", sessionID = activeSession.ID });
    }

    private async Task LogApiCall(int? studentId, string status, string message)
    {
        _db.AttendanceApiLogs.Add(new AttendanceApiLog
        {
            StudentID = studentId,
            Status = status,
            Message = message,
            CreatedAt = DateTime.Now
        });
        await _db.SaveChangesAsync();
    }
}
