using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniStay.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttendanceApiLog",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentID = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__AttendanceApiLog__3214EC27", x => x.ID);
                    table.ForeignKey(
                        name: "FK_AttendanceApiLog_Student",
                        column: x => x.StudentID,
                        principalTable: "Student",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "AttendanceSession",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__AttendanceSession__3214EC27", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceSetting",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    ConfidenceThreshold = table.Column<decimal>(type: "decimal(5,4)", nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__AttendanceSetting__3214EC27", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceLog",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentID = table.Column<int>(type: "int", nullable: false),
                    SessionID = table.Column<int>(type: "int", nullable: false),
                    RecognizedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    Confidence = table.Column<decimal>(type: "decimal(5,4)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__AttendanceLog__3214EC27", x => x.ID);
                    table.ForeignKey(
                        name: "FK_AttendanceLog_AttendanceSession",
                        column: x => x.SessionID,
                        principalTable: "AttendanceSession",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_AttendanceLog_Student",
                        column: x => x.StudentID,
                        principalTable: "Student",
                        principalColumn: "ID");
                });

            migrationBuilder.InsertData(
                table: "AttendanceSetting",
                columns: new[] { "ID", "ConfidenceThreshold", "EndTime", "IsEnabled", "StartTime" },
                values: new object[] { 1, 0.85m, new TimeOnly(4, 0, 0), true, new TimeOnly(23, 0, 0) });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceApiLog_StudentID",
                table: "AttendanceApiLog",
                column: "StudentID");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceLog_SessionID",
                table: "AttendanceLog",
                column: "SessionID");

            migrationBuilder.CreateIndex(
                name: "UQ_AttendanceLog_StudentSession",
                table: "AttendanceLog",
                columns: new[] { "StudentID", "SessionID" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceApiLog");

            migrationBuilder.DropTable(
                name: "AttendanceLog");

            migrationBuilder.DropTable(
                name: "AttendanceSetting");

            migrationBuilder.DropTable(
                name: "AttendanceSession");
        }
    }
}
