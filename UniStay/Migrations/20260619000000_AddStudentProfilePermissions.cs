using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniStay.Migrations
{
    public partial class AddStudentProfilePermissions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Permissions for Group 3 (الطلاب) - IDs 55-59
            migrationBuilder.InsertData(
                table: "Permission",
                columns: new[] { "ID", "PermissionKey", "DisplayName", "Category", "GroupID" },
                values: new object[,]
                {
                    { 55, "Students.View", "عرض بيانات الطلاب", null, 3 },
                    { 56, "Students.Edit", "تعديل بيانات الطلاب", null, 3 },
                    { 57, "StudentStatus.View", "عرض بيان حالة الطالب", null, 3 },
                    { 58, "SpecialCases.Manage", "إدارة الحالات الخاصة", null, 3 },
                    { 59, "SocialCases.Manage", "إدارة حالات البحث الاجتماعي", null, 3 },
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            for (int id = 55; id <= 59; id++)
                migrationBuilder.DeleteData("Permission", "ID", id);
        }
    }
}
