namespace UniStay.Services.Interfaces;

public enum EmailType
{
    ApplicationReceived,
    ApplicationAccepted,
    ApplicationRejected,
    AbsenceApproved,
    AbsenceRejected,
    PaymentReminder,
    Eviction,
    General
}

public interface IEmailService
{
    // الاسم هنا toEmail عشان يتطابق مع كل الاستدعاءات
    Task SendAsync(string? toEmail, string subject, string body, EmailType type, int? studentId = null);
    Task SendBulkAsync(List<string> emails, string subject, string body, EmailType type);
}