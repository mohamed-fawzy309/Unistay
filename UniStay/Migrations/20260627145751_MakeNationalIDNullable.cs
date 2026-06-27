using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniStay.Migrations
{
    /// <inheritdoc />
    public partial class MakeNationalIDNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationTypes");

            migrationBuilder.DropTable(
                name: "Country");

            migrationBuilder.DropTable(
                name: "SocialCase");

            migrationBuilder.DropTable(
                name: "SpecialCase");

            migrationBuilder.DropTable(
                name: "StudentCategory");

            migrationBuilder.DropTable(
                name: "UniversityPhoto");

            migrationBuilder.DropTable(
                name: "Village");

            migrationBuilder.DropIndex(
                name: "UQ_Student_NationalID",
                table: "Student");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "StudentLogin",
                type: "varchar(200)",
                unicode: false,
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(14)",
                oldUnicode: false,
                oldMaxLength: 14);

            migrationBuilder.AlterColumn<string>(
                name: "NationalID",
                table: "Student",
                type: "varchar(14)",
                unicode: false,
                maxLength: 14,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(14)",
                oldUnicode: false,
                oldMaxLength: 14);

            migrationBuilder.CreateIndex(
                name: "UQ_Student_NationalID",
                table: "Student",
                column: "NationalID",
                unique: true,
                filter: "[NationalID] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_Student_NationalID",
                table: "Student");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "StudentLogin",
                type: "varchar(14)",
                unicode: false,
                maxLength: 14,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldUnicode: false,
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "NationalID",
                table: "Student",
                type: "varchar(14)",
                unicode: false,
                maxLength: 14,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(14)",
                oldUnicode: false,
                oldMaxLength: 14,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "ApplicationTypes",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationTypes", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Country",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Country__3214EC27", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "SocialCase",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssignedTo = table.Column<int>(type: "int", nullable: true),
                    StudentID = table.Column<int>(type: "int", nullable: false),
                    CaseType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Priority = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true, defaultValue: "Open")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__SocialCa__3214EC2716577C8C", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SocialCase_AssignedTo",
                        column: x => x.AssignedTo,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_SocialCase_Student",
                        column: x => x.StudentID,
                        principalTable: "Student",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "SpecialCase",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationID = table.Column<int>(type: "int", nullable: false),
                    ReviewedBy = table.Column<int>(type: "int", nullable: true),
                    StudentID = table.Column<int>(type: "int", nullable: false),
                    CaseType = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true, defaultValue: "Pending"),
                    SupportingDocuments = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__SpecialC__3214EC2755A318C5", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SpecialCase_Application",
                        column: x => x.ApplicationID,
                        principalTable: "Application",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_SpecialCase_ReviewedBy",
                        column: x => x.ReviewedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_SpecialCase_Student",
                        column: x => x.StudentID,
                        principalTable: "Student",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "StudentCategory",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__StudentCategory__3214EC27", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "UniversityPhoto",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DormitoryCityID = table.Column<int>(type: "int", nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    PhotoType = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true, defaultValue: "Campus"),
                    SortOrder = table.Column<byte>(type: "tinyint", nullable: true, defaultValue: (byte)0),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Universi__3214EC27620AC0CD", x => x.ID);
                    table.ForeignKey(
                        name: "FK_UniversityPhoto_DormitoryCity",
                        column: x => x.DormitoryCityID,
                        principalTable: "DormitoryCity",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "Village",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    DormitoryCityID = table.Column<int>(type: "int", nullable: false),
                    LastUpdatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Village__3214EC27", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Village_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Village_DormitoryCity",
                        column: x => x.DormitoryCityID,
                        principalTable: "DormitoryCity",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Village_LastUpdatedBy",
                        column: x => x.LastUpdatedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateIndex(
                name: "UQ_Student_NationalID",
                table: "Student",
                column: "NationalID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SocialCase_AssignedTo",
                table: "SocialCase",
                column: "AssignedTo");

            migrationBuilder.CreateIndex(
                name: "IX_SocialCase_StudentID",
                table: "SocialCase",
                column: "StudentID");

            migrationBuilder.CreateIndex(
                name: "IX_SpecialCase_ApplicationID",
                table: "SpecialCase",
                column: "ApplicationID");

            migrationBuilder.CreateIndex(
                name: "IX_SpecialCase_ReviewedBy",
                table: "SpecialCase",
                column: "ReviewedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SpecialCase_StudentID",
                table: "SpecialCase",
                column: "StudentID");

            migrationBuilder.CreateIndex(
                name: "IX_UniversityPhoto_DormitoryCityID",
                table: "UniversityPhoto",
                column: "DormitoryCityID");

            migrationBuilder.CreateIndex(
                name: "IX_Village_CreatedBy",
                table: "Village",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Village_DormitoryCityID",
                table: "Village",
                column: "DormitoryCityID");

            migrationBuilder.CreateIndex(
                name: "IX_Village_LastUpdatedBy",
                table: "Village",
                column: "LastUpdatedBy");
        }
    }
}
