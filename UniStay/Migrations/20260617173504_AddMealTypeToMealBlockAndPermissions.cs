using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniStay.Migrations
{
    /// <inheritdoc />
    public partial class AddMealTypeToMealBlockAndPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MealType",
                table: "MealBlock",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true);

            // New permissions for Meals module (Group 6)
            migrationBuilder.InsertData(
                table: "Permission",
                columns: new[] { "ID", "PermissionKey", "DisplayName", "Category", "GroupID" },
                values: new object[,]
                {
                    { 44, "Meals.View",     "عرض الوجبات",     null, 6 },
                    { 45, "Meals.Receive",  "استلام الوجبات",  null, 6 },
                    { 46, "Meals.Book",     "حجز الوجبات",     null, 6 },
                    { 47, "Meals.Restrict", "حجب الوجبات",     null, 6 },
                    { 48, "Meals.Prepare",  "تجهيز الوجبات",   null, 6 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "Permission", keyColumn: "ID", keyValues: new object[] { 44, 45, 46, 47, 48 });

            migrationBuilder.DropColumn(
                name: "MealType",
                table: "MealBlock");
        }
    }
}
