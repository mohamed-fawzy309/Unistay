using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace UniStay.ViewModels.Document
{
    public class DocumentAdminIndexViewModel
    {
        public List<DocumentRowViewModel> Documents { get; set; } = new();
        public string? FilterStatus { get; set; }
        public int Page { get; set; }
        public int TotalPages { get; set; }
    }

    public class DocumentRowViewModel
    {
        public int ID { get; set; }
        public int StudentID { get; set; }
        public string StudentName { get; set; } = null!;
        public string NationalID { get; set; } = null!;
        public string DocumentType { get; set; } = null!;
        public string? FileName { get; set; }
        public bool? IsVerified { get; set; }
        public DateTime? UploadedAt { get; set; }
        public string? VerifiedByName { get; set; }
    }

    public class UploadDocumentViewModel
    {
        [Required(ErrorMessage = "الطالب مطلوب")]
        public int StudentID { get; set; }

        public int? ApplicationID { get; set; }

        [Required(ErrorMessage = "نوع المستند مطلوب")]
        public string DocumentType { get; set; } = null!;

        [Required(ErrorMessage = "الملف مطلوب")]
        public IFormFile File { get; set; } = null!;
    }
}
