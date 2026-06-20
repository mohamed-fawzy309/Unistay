using Microsoft.AspNetCore.Http;
using UniStay.ViewModels.Photos;

namespace UniStay.Services.Interfaces;

public interface IPhotoService
{
    Task<StudentPhotoRowViewModel?> GetStudentPhotoInfoAsync(int studentId);
    Task<string?> UploadPhotoAsync(int studentId, IFormFile file, int userId);
    Task<bool> DeletePhotoAsync(int studentId, int userId);
    Task<BulkImportResultViewModel> BulkImportFromZipAsync(IFormFile zipFile, string matchBy, int userId);
    bool IsValidImageFile(IFormFile file);
}
