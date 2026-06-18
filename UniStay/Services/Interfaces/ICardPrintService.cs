using UniStay.ViewModels.Photos;

namespace UniStay.Services.Interfaces;

public interface ICardPrintService
{
    Task AddToPrintQueueAsync(List<int> studentIds, int userId);
    Task<byte[]> GenerateSingleCardPdfAsync(int studentId);
    Task<byte[]> GenerateBatchCardPdfAsync(List<int> studentIds);
    Task MarkAsPrintedAsync(int queueId, int userId);
    Task MarkAsFailedAsync(int queueId, string? reason = null);
    byte[] GenerateQrCodePng(string data);
}
