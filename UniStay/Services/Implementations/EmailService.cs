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
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string SenderName { get; set; } = "UniStay";
    public string SenderEmail { get; set; } = "";
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
            await client.ConnectAsync(_c.Host, _c.Port, _c.EnableSsl);
            await client.AuthenticateAsync(_c.Username, _c.Password);

            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress(_c.SenderName, _c.SenderEmail));
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