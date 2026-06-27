using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniStay.Migrations
{
    /// <inheritdoc />
    public partial class FixPendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[Document]', 'U') IS NOT NULL
                    DROP TABLE [Document]
            ");

            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[FacultyQuota]', 'U') IS NOT NULL
                    DROP TABLE [FacultyQuota]
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Document",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationID = table.Column<int>(type: "int", nullable: true),
                    StudentID = table.Column<int>(type: "int", nullable: false),
                    VerifiedBy = table.Column<int>(type: "int", nullable: true),
                    DocumentType = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsVerified = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Document__3214EC273EA5E377", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Document_Application",
                        column: x => x.ApplicationID,
                        principalTable: "Application",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Document_Student",
                        column: x => x.StudentID,
                        principalTable: "Student",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Document_VerifiedBy",
                        column: x => x.VerifiedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "FacultyQuota",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DormitoryCityID = table.Column<int>(type: "int", nullable: false),
                    AcademicYear = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CurrentCount = table.Column<int>(type: "int", nullable: false),
                    Faculty = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MaxQuota = table.Column<int>(type: "int", nullable: false),
                    MinQuota = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__FacultyQ__3214EC2724A3F6E5", x => x.ID);
                    table.ForeignKey(
                        name: "FK_FacultyQuota_DormitoryCity",
                        column: x => x.DormitoryCityID,
                        principalTable: "DormitoryCity",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Document_ApplicationID",
                table: "Document",
                column: "ApplicationID");

            migrationBuilder.CreateIndex(
                name: "IX_Document_StudentID",
                table: "Document",
                column: "StudentID");

            migrationBuilder.CreateIndex(
                name: "IX_Document_VerifiedBy",
                table: "Document",
                column: "VerifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FacultyQuota_DormitoryCityID",
                table: "FacultyQuota",
                column: "DormitoryCityID");
        }
    }
}
