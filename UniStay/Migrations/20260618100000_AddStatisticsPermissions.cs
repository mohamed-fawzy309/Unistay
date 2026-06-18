using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniStay.Migrations
{
    /// <inheritdoc />
    public partial class AddStatisticsPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Permission Group 11: Statistics
            migrationBuilder.InsertData(
                table: "PermissionGroup",
                columns: new[] { "ID", "GroupName", "Description" },
                values: new object[] { 11, "الإحصائيات", "عرض وتصدير الإحصائيات والتقارير البيانية" });

            // Permissions for Group 11
            migrationBuilder.InsertData(
                table: "Permission",
                columns: new[] { "ID", "PermissionKey", "DisplayName", "Category", "GroupID" },
                values: new object[,]
                {
                    { 53, "Statistics.View",   "عرض الإحصائيات",  null, 11 },
                    { 54, "Statistics.Export", "تصدير الإحصائيات", null, 11 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "Permission", keyColumn: "ID", keyValues: new object[] { 53, 54 });
            migrationBuilder.DeleteData(table: "PermissionGroup", keyColumn: "ID", keyValues: new object[] { 11 });
        }
    }
}
