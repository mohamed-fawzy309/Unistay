using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Models;
using UniStay.Services.Interfaces;
using UniStay.ViewModels.Photos;
using System.IO.Compression;

namespace UniStay.Services.Implementations;

public class PhotoService : IPhotoService
{
    private readonly AssuitDbContext _db;
    private readonly IAuditService _audit;
    private readonly string _photosDir;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png" };
    private const int MaxFileSize = 5 * 1024 * 1024;

    private static readonly byte[] JpegSig = { 0xFF, 0xD8, 0xFF };
    private static readonly byte[] PngSig = { 0x89, 0x50, 0x4E, 0x47 };

    public PhotoService(AssuitDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
        _photosDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "photos");
        Directory.CreateDirectory(_photosDir);
    }

    public bool IsValidImageFile(IFormFile file)
    {
        if (file is null || file.Length == 0 || file.Length > MaxFileSize) return false;

        var ext = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(ext)) return false;

        using var ms = new MemoryStream();
        file.CopyTo(ms);
        var header = ms.ToArray().Take(4).ToArray();

        return header.Take(3).SequenceEqual(JpegSig) || header.Take(4).SequenceEqual(PngSig);
    }

    public async Task<StudentPhotoRowViewModel?> GetStudentPhotoInfoAsync(int studentId)
    {
        var student = await _db.Students
            .Include(s => s.Allocations).ThenInclude(a => a.CityRoom).ThenInclude(cr => cr.CityBuilding).ThenInclude(cb => cb.DormitoryCity)
            .FirstOrDefaultAsync(s => s.ID == studentId && s.IsDeleted != true);

        if (student is null) return null;

        var alloc = student.Allocations.FirstOrDefault(a => a.Status == "Active");
        return new StudentPhotoRowViewModel
        {
            StudentID = student.ID,
            FullName = student.FullName,
            NationalID = student.NationalID,
            Faculty = student.Faculty,
            CityName = alloc?.CityRoom?.CityBuilding?.DormitoryCity?.Name,
            PhotoPath = student.Photo
        };
    }

    public async Task<string?> UploadPhotoAsync(int studentId, IFormFile file, int userId)
    {
        var student = await _db.Students.FindAsync(new object[] { studentId });
        if (student is null || student.IsDeleted == true) return null;

        if (!IsValidImageFile(file)) throw new InvalidOperationException("صورة غير صالحة. يجب أن تكون JPG أو PNG وأقل من 5MB");

        var ext = Path.GetExtension(file.FileName);
        var fileName = $"{studentId}_{Guid.NewGuid()}{ext}";
        var relativePath = $"/uploads/photos/{fileName}";
        var fullPath = Path.Combine(_photosDir, fileName);

        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var oldPhoto = student.Photo;
        student.Photo = relativePath;
        student.LastUpdatedAt = DateTime.UtcNow;
        student.LastUpdatedBy = userId;

        await _db.SaveChangesAsync();

        if (!string.IsNullOrEmpty(oldPhoto))
        {
            var oldFullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", oldPhoto.TrimStart('/'));
            if (File.Exists(oldFullPath)) File.Delete(oldFullPath);
        }

        await _audit.LogAsync(userId, "Staff", "Photo.Upload", "Student", studentId,
            new { Photo = oldPhoto }, new { Photo = relativePath });

        return relativePath;
    }

    public async Task<bool> DeletePhotoAsync(int studentId, int userId)
    {
        var student = await _db.Students.FindAsync(new object[] { studentId });
        if (student is null || student.IsDeleted == true) return false;

        var oldPhoto = student.Photo;
        if (string.IsNullOrEmpty(oldPhoto)) return false;

        student.Photo = null;
        student.LastUpdatedAt = DateTime.UtcNow;
        student.LastUpdatedBy = userId;

        await _db.SaveChangesAsync();

        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", oldPhoto.TrimStart('/'));
        if (File.Exists(fullPath)) File.Delete(fullPath);

        await _audit.LogAsync(userId, "Staff", "Photo.Delete", "Student", studentId,
            new { Photo = oldPhoto }, new { Photo = (string?)null });

        return true;
    }

    public async Task<BulkImportResultViewModel> BulkImportFromZipAsync(IFormFile zipFile, string matchBy, int userId)
    {
        var result = new BulkImportResultViewModel();
        var tempDir = Path.Combine(Path.GetTempPath(), $"bulk_import_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var zipPath = Path.Combine(tempDir, "import.zip");
            await using (var fs = new FileStream(zipPath, FileMode.Create))
            {
                await zipFile.CopyToAsync(fs);
            }

            ZipFile.ExtractToDirectory(zipPath, tempDir);

            var imageFiles = Directory.GetFiles(tempDir, "*.*", SearchOption.AllDirectories)
                .Where(f => AllowedExtensions.Contains(Path.GetExtension(f)))
                .ToList();

            result.TotalInZip = imageFiles.Count;

            var students = await _db.Students.Where(s => s.IsDeleted != true).ToListAsync();

            foreach (var file in imageFiles)
            {
                var detail = new ImportRowResult { FileName = Path.GetFileName(file) };

                try
                {
                    var fileNameWithoutExt = Path.GetFileNameWithoutExtension(file);
                    Student? matchedStudent = null;

                    if (matchBy == "StudentID" || string.IsNullOrEmpty(matchBy))
                    {
                        if (int.TryParse(fileNameWithoutExt, out var sid))
                            matchedStudent = students.FirstOrDefault(s => s.ID == sid);
                    }

                    if (matchBy == "NationalID" || (matchBy != "StudentID" && matchedStudent is null))
                    {
                        var parts = fileNameWithoutExt.Split('_', '-');
                        foreach (var part in parts)
                        {
                            matchedStudent = students.FirstOrDefault(s => s.NationalID == part);
                            if (matchedStudent is not null) break;
                        }
                    }

                    if (matchedStudent is null)
                    {
                        detail.Status = "missing";
                        detail.ErrorMessage = "لم يتم العثور على طالب مطابق";
                        result.MissingCount++;
                        result.Details.Add(detail);
                        continue;
                    }

                    detail.MatchedStudent = matchedStudent.FullName;
                    detail.MatchedNationalID = matchedStudent.NationalID;

                    if (!string.IsNullOrEmpty(matchedStudent.Photo))
                    {
                        detail.Status = "duplicate";
                        detail.ErrorMessage = "الطالب لديه صورة بالفعل";
                        result.DuplicateCount++;
                        result.Details.Add(detail);
                        continue;
                    }

                    var ext = Path.GetExtension(file);
                    var newFileName = $"{matchedStudent.ID}_{Guid.NewGuid()}{ext}";
                    var relativePath = $"/uploads/photos/{newFileName}";
                    var destPath = Path.Combine(_photosDir, newFileName);

                    File.Copy(file, destPath, overwrite: true);

                    matchedStudent.Photo = relativePath;
                    matchedStudent.LastUpdatedAt = DateTime.UtcNow;
                    matchedStudent.LastUpdatedBy = userId;

                    detail.Status = "imported";
                    result.ImportedCount++;
                    result.Details.Add(detail);
                }
                catch (Exception ex)
                {
                    detail.Status = "failed";
                    detail.ErrorMessage = ex.Message;
                    result.FailedCount++;
                    result.Details.Add(detail);
                }
            }

            await _db.SaveChangesAsync();

            await _audit.LogAsync(userId, "Staff", "Photo.BulkImport", "Student", null,
                null, new { Imported = result.ImportedCount, Failed = result.FailedCount, Missing = result.MissingCount, Duplicate = result.DuplicateCount });
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }

        return result;
    }
}
