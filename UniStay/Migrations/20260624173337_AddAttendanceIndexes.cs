using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniStay.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSession_StartedAt",
                table: "AttendanceSession",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceLog_RecognizedAt",
                table: "AttendanceLog",
                column: "RecognizedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceApiLog_CreatedAt",
                table: "AttendanceApiLog",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AttendanceSession_StartedAt",
                table: "AttendanceSession");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceLog_RecognizedAt",
                table: "AttendanceLog");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceApiLog_CreatedAt",
                table: "AttendanceApiLog");
        }
    }
}
