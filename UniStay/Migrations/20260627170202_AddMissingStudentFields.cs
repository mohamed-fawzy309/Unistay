using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniStay.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingStudentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BirthPlace",
                table: "Student",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CountryOfOrigin",
                table: "Student",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CountryOfOriginOther",
                table: "Student",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HighSchoolDivision",
                table: "Student",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HighSchoolFromAbroad",
                table: "Student",
                type: "bit",
                nullable: true,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "HighSchoolPercentage",
                table: "Student",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HighSchoolTotal",
                table: "Student",
                type: "decimal(8,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastYearGrade",
                table: "Student",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LastYearPercentage",
                table: "Student",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParentStatus",
                table: "Student",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PassportIssuePlace",
                table: "Student",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PassportNumber",
                table: "Student",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BirthPlace",
                table: "Student");

            migrationBuilder.DropColumn(
                name: "CountryOfOrigin",
                table: "Student");

            migrationBuilder.DropColumn(
                name: "CountryOfOriginOther",
                table: "Student");

            migrationBuilder.DropColumn(
                name: "HighSchoolDivision",
                table: "Student");

            migrationBuilder.DropColumn(
                name: "HighSchoolFromAbroad",
                table: "Student");

            migrationBuilder.DropColumn(
                name: "HighSchoolPercentage",
                table: "Student");

            migrationBuilder.DropColumn(
                name: "HighSchoolTotal",
                table: "Student");

            migrationBuilder.DropColumn(
                name: "LastYearGrade",
                table: "Student");

            migrationBuilder.DropColumn(
                name: "LastYearPercentage",
                table: "Student");

            migrationBuilder.DropColumn(
                name: "ParentStatus",
                table: "Student");

            migrationBuilder.DropColumn(
                name: "PassportIssuePlace",
                table: "Student");

            migrationBuilder.DropColumn(
                name: "PassportNumber",
                table: "Student");
        }
    }
}
