using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace UniStay.Migrations
{
    /// <inheritdoc />
    public partial class AddFacultyTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Faculty",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Faculty__3214EC27", x => x.ID);
                });

            migrationBuilder.InsertData(
                table: "Faculty",
                columns: new[] { "ID", "Description", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, null, true, "كلية الطب" },
                    { 2, null, true, "كلية طب الأسنان" },
                    { 3, null, true, "كلية الصيدلة" },
                    { 4, null, true, "كلية التمريض" },
                    { 5, null, true, "كلية العلوم" },
                    { 6, null, true, "كلية الهندسة" },
                    { 7, null, true, "كلية الزراعة" },
                    { 8, null, true, "كلية الطب البيطري" },
                    { 9, null, true, "كلية التجارة" },
                    { 10, null, true, "كلية التربية" },
                    { 11, null, true, "كلية الحقوق" },
                    { 12, null, true, "كلية التربية الرياضية" },
                    { 13, null, true, "كلية الخدمة الاجتماعية" },
                    { 14, null, true, "كلية الآداب" },
                    { 15, null, true, "كلية التربية النوعية" },
                    { 16, null, true, "كلية الحاسبات والمعلومات" },
                    { 17, null, true, "كلية تكنولوجيا صناعة السكر والصناعات التكاملية" },
                    { 18, null, true, "كلية الفنون الجميلة" },
                    { 19, null, true, "كلية التربية للطفولة المبكرة" },
                    { 20, null, true, "كلية التربية (الوادي الجديد)" },
                    { 21, null, true, "معهد جنوب مصر للأورام" },
                    { 22, null, true, "المعهد الفني للتمريض" },
                    { 23, null, true, "معهد بحوث ودراسات البيولوجيا الجزيئية" },
                    { 24, null, true, "معهد بحوث تطوير وابتكار الدواء" },
                    { 25, null, true, "معهد علوم المواد والنانوتكنولوجي" },
                    { 26, null, true, "معهد بحوث تكنولوجيا صناعة السكر" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Faculty");
        }
    }
}
