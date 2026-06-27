using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniStay.Migrations
{
    /// <inheritdoc />
    public partial class EnhanceCoordinationRule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HousingType",
                table: "CoordinationRule",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "CoordinationRule",
                type: "bit",
                nullable: true,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MinDistance",
                table: "CoordinationRule",
                type: "decimal(8,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinGrade",
                table: "CoordinationRule",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StudentType",
                table: "CoordinationRule",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HousingType",
                table: "CoordinationRule");

            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "CoordinationRule");

            migrationBuilder.DropColumn(
                name: "MinDistance",
                table: "CoordinationRule");

            migrationBuilder.DropColumn(
                name: "MinGrade",
                table: "CoordinationRule");

            migrationBuilder.DropColumn(
                name: "StudentType",
                table: "CoordinationRule");
        }
    }
}
