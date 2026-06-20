using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniStay.Migrations
{
    /// <inheritdoc />
    public partial class AddPhotoCardPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Permission Group 10: Photos & Cards
            migrationBuilder.InsertData(
                table: "PermissionGroup",
                columns: new[] { "ID", "GroupName", "Description" },
                values: new object[] { 10, "البطاقات والصور", "إدارة صور الطلاب وطباعة البطاقات" });

            // Permissions for Group 10
            migrationBuilder.InsertData(
                table: "Permission",
                columns: new[] { "ID", "PermissionKey", "DisplayName", "Category", "GroupID" },
                values: new object[,]
                {
                    { 39, "Photos.View",   "عرض الصور",     null, 10 },
                    { 40, "Photos.Manage", "إدارة الصور",   null, 10 },
                    { 41, "Cards.View",    "عرض البطاقات",  null, 10 },
                    { 42, "Cards.Print",   "طباعة البطاقات",null, 10 },
                    { 43, "Cards.Manage",  "إدارة البطاقات",null, 10 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "Permission", keyColumn: "ID", keyValues: new object[] { 39, 40, 41, 42, 43 });
            migrationBuilder.DeleteData(table: "PermissionGroup", keyColumn: "ID", keyValues: new object[] { 10 });
        }
    }
}
