using System;
using System.Collections.Generic;

namespace UniStay.ViewModels.Attendance
{
    public class AttendanceCheckinRequest
    {
        public int StudentID { get; set; }
        public decimal? Confidence { get; set; }
        public DateTime? Timestamp { get; set; }
    }

    public class AttendanceSettingsResponse
    {
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public decimal? ConfidenceThreshold { get; set; }
        public bool? IsEnabled { get; set; }
    }

    public class SessionStartRequest
    {
        public string SessionName { get; set; } = null!;
    }

    public class AttendanceDashboardViewModel
    {
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public decimal AttendancePercentage { get; set; }
        public string? ActiveSession { get; set; }
        public List<TodayAttendanceItemViewModel> TodayRecords { get; set; } = new();
    }

    public class TodayAttendanceItemViewModel
    {
        public int StudentID { get; set; }
        public string StudentName { get; set; } = null!;
        public string RoomNumber { get; set; } = "N/A";
        public DateTime RecognizedAt { get; set; }
        public decimal? Confidence { get; set; }
    }

    public class StudentAttendanceHistoryViewModel
    {
        public bool IsPresentToday { get; set; }
        public DateTime? TodayRecognitionTime { get; set; }
        public bool IsAbsentToday { get; set; }
        public int PresentDaysThisMonth { get; set; }
        public int TotalSessionDaysThisMonth { get; set; }
        public decimal AttendancePercentage { get; set; }
        public List<AttendanceHistoryItemViewModel> HistoryItems { get; set; } = new();
    }

    public class AttendanceHistoryItemViewModel
    {
        public DateTime Date { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? RecognitionTime { get; set; }
    }

    public class DailyReportViewModel
    {
        public DateTime? FilterDate { get; set; }
        public string? FilterStudentName { get; set; }
        public string? FilterRoomNumber { get; set; }
        public int? FilterCityId { get; set; }
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public decimal AttendancePercentage { get; set; }
        public List<DailyReportRowViewModel> Rows { get; set; } = new();
    }

    public class DailyReportRowViewModel
    {
        public int StudentID { get; set; }
        public string StudentName { get; set; } = null!;
        public string? NationalID { get; set; }
        public string RoomNumber { get; set; } = "N/A";
        public string? CityName { get; set; }
        public DateTime? RecognizedAt { get; set; }
        public decimal? Confidence { get; set; }
    }

    public class MonthlyReportViewModel
    {
        public int? FilterMonth { get; set; }
        public int? FilterYear { get; set; }
        public string? FilterStudentName { get; set; }
        public int TotalSessionDays { get; set; }
        public decimal AverageAttendancePercentage { get; set; }
        public List<MonthlyReportRowViewModel> Rows { get; set; } = new();
    }

    public class MonthlyReportRowViewModel
    {
        public int StudentID { get; set; }
        public string StudentName { get; set; } = null!;
        public string RoomNumber { get; set; } = "N/A";
        public int PresentDays { get; set; }
        public int TotalDays { get; set; }
        public decimal Percentage { get; set; }
    }

    public class MonitoringDashboardViewModel
    {
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public int DuplicateCount { get; set; }
        public string? LastErrorMessage { get; set; }
        public DateTime? LastErrorTime { get; set; }
        public List<MonitoringLogItemViewModel> RecentLogs { get; set; } = new();
    }

    public class MonitoringLogItemViewModel
    {
        public int ID { get; set; }
        public int? StudentID { get; set; }
        public string? Status { get; set; }
        public string? Message { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class AttendanceDashboardStatsViewModel
    {
        public int RecognitionSuccessCount { get; set; }
        public int DuplicateAttemptsCount { get; set; }
        public int ApiFailuresCount { get; set; }
    }

    public class EnrollmentStudentItem
    {
        public int StudentID { get; set; }
        public string FullName { get; set; } = null!;
        public string? NationalID { get; set; }
        public string? DormitoryCity { get; set; }
        public string? Building { get; set; }
        public string? RoomNumber { get; set; }
        public byte? Floor { get; set; }
        public byte? Bed { get; set; }
        public string? Photo { get; set; }
    }

    public class EnrollmentViewModel
    {
        public int TotalAccommodated { get; set; }
        public int RegisteredCount { get; set; }
        public int NotRegisteredCount { get; set; }
        public decimal EnrollmentPercentage { get; set; }
        public List<EnrollmentStudentItem> Students { get; set; } = new();
    }

    public class FaceEnrollRequest
    {
        public string StudentID { get; set; } = null!;
        public List<string> Images { get; set; } = new();
    }

    public class FaceDeleteRequest
    {
        public string StudentID { get; set; } = null!;
    }

    public class FaceTestRequest
    {
        public string Image { get; set; } = null!;
    }

    public class ControlRoomViewModel
    {
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public decimal AttendancePercentage { get; set; }
        public string? ActiveSession { get; set; }
        public int TotalStudents { get; set; }
    }

    public class ControlRoomEventItem
    {
        public string Time { get; set; } = "";
        public string Type { get; set; } = "";
        public string Message { get; set; } = "";
        public string? StudentName { get; set; }
        public int? StudentID { get; set; }
        public double? Confidence { get; set; }
        public string? AttendanceResult { get; set; }
    }

    public class SessionSummaryViewModel
    {
        public int TotalStudents { get; set; }
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public decimal AttendancePercentage { get; set; }
        public int FacesDetected { get; set; }
        public int StudentsRecognized { get; set; }
        public int UnknownFaces { get; set; }
        public decimal RecognitionAccuracy { get; set; }
        public int DuplicateAttempts { get; set; }
        public int CameraIndex { get; set; }
        public string? SessionName { get; set; }
        public int? SessionId { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public string Duration { get; set; } = "—";
        public bool HasAbsences { get; set; }
        public int AbsenceCount { get; set; }
    }
}
