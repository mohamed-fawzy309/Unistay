using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniStay.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename permission group from "التقديم والتنسيق" to "التقديم والقبول"
            migrationBuilder.UpdateData(
                table: "PermissionGroup",
                keyColumn: "ID",
                keyValue: 2,
                columns: new[] { "GroupName" },
                values: new object[] { "التقديم والقبول" });

            // New permissions for Group 2: Applications & Admission
            migrationBuilder.InsertData(
                table: "Permission",
                columns: new[] { "ID", "PermissionKey", "DisplayName", "Category", "GroupID" },
                values: new object[,]
                {
                    { 49, "Applications.View",    "عرض التطبيقات",     null, 2 },
                    { 50, "Applications.Manage",  "إدارة التطبيقات",   null, 2 },
                    { 51, "Applications.Review",  "مراجعة التطبيقات",  null, 2 },
                    { 52, "Coordination.View",    "عرض التنسيق",       null, 2 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert group name
            migrationBuilder.UpdateData(
                table: "PermissionGroup",
                keyColumn: "ID",
                keyValue: 2,
                columns: new[] { "GroupName" },
                values: new object[] { "التقديم والتنسيق" });

            // Remove new permissions
            migrationBuilder.DeleteData(table: "Permission", keyColumn: "ID", keyValues: new object[] { 49, 50, 51, 52 });
        }
    }
}
