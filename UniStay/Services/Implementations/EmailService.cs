using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using UniStay.Data;
using UniStay.Models;
using UniStay.Services.Interfaces;

namespace UniStay.Services.Implementations;

public class EmailSettings
{
    public string Smtp { get; set; } = "";
    public int Port { get; set; } = 587;
    public string User { get; set; } = "";
    public string Pass { get; set; } = "";
    public string From { get; set; } = "";
}

public class EmailService : IEmailService
{
    private readonly AssuitDbContext _db;
    private readonly EmailSettings _c;

    public EmailService(AssuitDbContext db, IOptions<EmailSettings> opt)
    {
        _db = db;
        _c = opt.Value;
    }

    public async Task SendAsync(string? toEmail, string subject, string body, EmailType type, int? studentId = null)
    {
        if (string.IsNullOrWhiteSpace(toEmail)) return;

        var log = new EmailLog
        {
            RecipientEmail = toEmail,
            Subject = subject,
            Body = body,
            EmailType = type.ToString(),
            StudentID = studentId,
            Status = "Pending",
            CreatedAt = DateTime.Now
        };
        _db.EmailLogs.Add(log);
        await _db.SaveChangesAsync();

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_c.Smtp, _c.Port, true);
            await client.AuthenticateAsync(_c.User, _c.Pass);

            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress("UniStay", _c.From));
            msg.To.Add(MailboxAddress.Parse(toEmail));
            msg.Subject = subject;
            msg.Body = new TextPart("html") { Text = body };

            await client.SendAsync(msg);
            await client.DisconnectAsync(true);

            log.Status = "Sent";
            log.SentAt = DateTime.Now;
        }
        catch (Exception ex)
        {
            log.Status = "Failed";
            log.ErrorMessage = ex.Message;
            log.FailedAt = DateTime.Now;
        }
        await _db.SaveChangesAsync();
    }

    public Task SendBulkAsync(List<string> emails, string subject, string body, EmailType type)
        => Task.WhenAll(emails.Select(e => SendAsync(e, subject, body, type)));
}