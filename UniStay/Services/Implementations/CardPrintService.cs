using Microsoft.EntityFrameworkCore;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using UniStay.Data;
using UniStay.Models;
using UniStay.Services.Interfaces;
using UniStay.ViewModels.Photos;

namespace UniStay.Services.Implementations;

public class CardPrintService : ICardPrintService
{
    private readonly AssuitDbContext _db;
    private readonly IAuditService _audit;

    public CardPrintService(AssuitDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public byte[] GenerateQrCodePng(string data)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);
        return qrCode.GetGraphic(20);
    }

    public async Task AddToPrintQueueAsync(List<int> studentIds, int userId)
    {
        var alreadyQueued = await _db.CardPrintQueues
            .Where(q => studentIds.Contains(q.StudentID) && q.Status == "Pending")
            .Select(q => q.StudentID).ToListAsync();

        var toAdd = studentIds.Except(alreadyQueued).ToList();
        if (toAdd.Count == 0) return;

        foreach (var sid in toAdd)
        {
            var alloc = await _db.Allocations
                .Include(a => a.CityRoom).ThenInclude(cr => cr.CityBuilding)
                .FirstOrDefaultAsync(a => a.StudentID == sid && a.Status == "Active");

            _db.CardPrintQueues.Add(new CardPrintQueue
            {
                StudentID = sid,
                DormitoryCityID = alloc?.CityRoom?.CityBuilding?.DormitoryCityID ?? 0,
                Status = "Pending",
                QueuedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        await _audit.LogAsync(userId, "Staff", "CardPrint.Queue", "CardPrintQueue", null,
            null, new { StudentIDs = studentIds, Count = toAdd.Count });
    }

    public async Task<byte[]> GenerateSingleCardPdfAsync(int studentId)
    {
        var student = await LoadStudentDataAsync(studentId);
        if (student is null) return Array.Empty<byte>();

        var qrPng = GenerateQrCodePng(BuildQrData(student));
        return GenerateSingleCardDocument(student, qrPng).GeneratePdf();
    }

    public async Task<byte[]> GenerateBatchCardPdfAsync(List<int> studentIds)
    {
        var students = new List<StudentCardData>();
        foreach (var id in studentIds)
        {
            var s = await LoadStudentDataAsync(id);
            if (s is not null) students.Add(s);
        }

        if (students.Count == 0) return Array.Empty<byte>();

        return GenerateBatchCardDocument(students).GeneratePdf();
    }

    public async Task MarkAsPrintedAsync(int queueId, int userId)
    {
        var item = await _db.CardPrintQueues.FindAsync(new object[] { queueId });
        if (item is null) return;

        item.Status = "Printed";
        item.PrintedAt = DateTime.UtcNow;
        item.PrintedBy = userId;

        var student = await LoadStudentDataAsync(item.StudentID);
        var idCard = await _db.IDCards.FirstOrDefaultAsync(c => c.StudentID == item.StudentID && c.IsActive == true);
        if (idCard is null)
        {
            idCard = new IDCard
            {
                StudentID = item.StudentID,
                CardNumber = $"CARD-{item.StudentID}-{DateTime.UtcNow:yyyyMMdd}",
                QRCode = student is not null ? BuildQrData(student) : "",
                IsPrinted = true,
                IsActive = true,
                ReprintCount = 0,
                PrintedAt = DateTime.UtcNow,
                PrintedBy = userId
            };
            _db.IDCards.Add(idCard);
        }
        else
        {
            idCard.IsPrinted = true;
            idCard.ReprintCount = (byte?)((idCard.ReprintCount ?? 0) + 1);
            idCard.PrintedAt = DateTime.UtcNow;
            idCard.PrintedBy = userId;
        }

        await _db.SaveChangesAsync();
        await _audit.LogAsync(userId, "Staff", "CardPrint.Print", "CardPrintQueue", queueId);
    }

    public async Task MarkAsFailedAsync(int queueId, string? reason = null)
    {
        var item = await _db.CardPrintQueues.FindAsync(new object[] { queueId });
        if (item is null) return;
        item.Status = "Failed";
        await _db.SaveChangesAsync();
    }

    private async Task<StudentCardData?> LoadStudentDataAsync(int studentId)
    {
        return await _db.Students
            .Where(s => s.ID == studentId && s.IsDeleted != true)
            .Select(s => new StudentCardData
            {
                StudentID = s.ID,
                FullName = s.FullName,
                NationalID = s.NationalID,
                StudentCode = s.StudentCode,
                Faculty = s.Faculty,
                Gender = s.Gender,
                PhotoPath = s.Photo,
                CityName = s.Allocations
                    .Where(a => a.Status == "Active")
                    .Select(a => a.CityRoom.CityBuilding.DormitoryCity.Name)
                    .FirstOrDefault() ?? "",
                BuildingName = s.Allocations
                    .Where(a => a.Status == "Active")
                    .Select(a => a.CityRoom.CityBuilding.BuildingName)
                    .FirstOrDefault() ?? "",
                RoomNumber = s.Allocations
                    .Where(a => a.Status == "Active")
                    .Select(a => a.CityRoom.RoomNumber)
                    .FirstOrDefault() ?? "",
                BedNumber = s.Allocations
                    .Where(a => a.Status == "Active")
                    .Select(a => (int?)a.BedNumber)
                    .FirstOrDefault()
            }).FirstOrDefaultAsync();
    }

    private static string BuildQrData(StudentCardData s)
    {
        return $"UniStay|{s.StudentID}|{s.NationalID}|{s.FullName}";
    }

    private static QuestPDF.Fluent.Document GenerateSingleCardDocument(StudentCardData student, byte[] qrPng)
    {
        return QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(15);
                page.DefaultTextStyle(x => x.FontSize(10));
                page.Content().Element(c => DrawCard(c, student, qrPng));
            });
        });
    }

    private static QuestPDF.Fluent.Document GenerateBatchCardDocument(List<StudentCardData> students)
    {
        return QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(15);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Content().Grid(grid =>
                {
                    grid.Columns(2);
                    grid.Spacing(10);

                    foreach (var student in students)
                    {
                        var qrPng = GenerateQrCodePngStatic(BuildQrData(student));
                        grid.Item().Element(c => DrawCard(c, student, qrPng));
                    }
                });
            });
        });
    }

    private static byte[] GenerateQrCodePngStatic(string data)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);
        return qrCode.GetGraphic(20);
    }

    private static void DrawCard(IContainer container, StudentCardData s, byte[] qrPng)
    {
        container.Border(1).BorderColor(Colors.Grey.Darken2).Padding(10).Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text("جامعة أسيوط").Bold().FontSize(14).FontColor(Colors.Blue.Darken3);
                    left.Item().PaddingTop(5).Text("بطاقة تعريف طالب").SemiBold().FontSize(11);
                    left.Item().PaddingTop(3).Text($"الاسم: {s.FullName}").FontSize(10);
                    left.Item().Text($"الكود: {s.StudentCode ?? "---"}").FontSize(10);
                    left.Item().Text($"الرقم القومي: {s.NationalID}").FontSize(10);
                    left.Item().Text($"الكلية: {s.Faculty ?? "---"}").FontSize(10);
                    left.Item().Text($"المدينة: {s.CityName}").FontSize(10);
                    left.Item().Text($"المبنى: {s.BuildingName}").FontSize(10);
                    left.Item().Text($"الغرفة: {s.RoomNumber} - سرير {s.BedNumber}").FontSize(10);
                });

                row.ConstantItem(90).Column(right =>
                {
                    var photoPath = s.PhotoPath;
                    if (!string.IsNullOrEmpty(photoPath))
                    {
                        var fullPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", photoPath.TrimStart('/'));
                        if (File.Exists(fullPath))
                            right.Item().AlignCenter().Width(70).Height(90).Image(fullPath);
                    }
                    if (qrPng is not null && qrPng.Length > 0)
                    {
                        right.Item().PaddingTop(5).AlignCenter().Width(70).Height(70).Image(qrPng);
                    }
                });
            });
        });
    }

    private class StudentCardData
    {
        public int StudentID { get; set; }
        public string FullName { get; set; } = "";
        public string NationalID { get; set; } = "";
        public string? StudentCode { get; set; }
        public string? Faculty { get; set; }
        public string Gender { get; set; } = "";
        public string? PhotoPath { get; set; }
        public string CityName { get; set; } = "";
        public string BuildingName { get; set; } = "";
        public string RoomNumber { get; set; } = "";
        public int? BedNumber { get; set; }
    }
}
