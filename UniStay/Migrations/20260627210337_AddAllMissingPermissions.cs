using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniStay.Migrations
{
    public partial class AddAllMissingPermissions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ──────────────────────────────────────────────────────────
            // Rename Group 2: التقديم والتنسيق ← التقديم والقبول
            // ──────────────────────────────────────────────────────────
            migrationBuilder.UpdateData(
                table: "PermissionGroup",
                keyColumn: "ID",
                keyValue: 2,
                columns: new[] { "GroupName" },
                values: new object[] { "التقديم والقبول" });

            // ──────────────────────────────────────────────────────────
            // Update Group 4: المدن والمباني ← المدن والمباني والتسكين
            // ──────────────────────────────────────────────────────────
            migrationBuilder.UpdateData(
                table: "PermissionGroup",
                keyColumn: "ID",
                keyValue: 4,
                columns: new[] { "GroupName", "Description" },
                values: new object[] { "المدن والمباني والتسكين", "إدارة المدن الجامعية والمباني والغرف والتسكين" });

            // ──────────────────────────────────────────────────────────
            // Update Group 5: المواعيد والتعليمات ← المواعيد والتعليمات والإعلانات
            // ──────────────────────────────────────────────────────────
            migrationBuilder.UpdateData(
                table: "PermissionGroup",
                keyColumn: "ID",
                keyValue: 5,
                columns: new[] { "GroupName", "Description" },
                values: new object[] { "المواعيد والتعليمات والإعلانات", "إدارة المواعيد والتعليمات والإعلانات وإعدادات التطبيق" });

            // ──────────────────────────────────────────────────────────
            // Update Group 7: الخدمات والانضباط ← الخدمات والانضباط والحضور
            // ──────────────────────────────────────────────────────────
            migrationBuilder.UpdateData(
                table: "PermissionGroup",
                keyColumn: "ID",
                keyValue: 7,
                columns: new[] { "GroupName", "Description" },
                values: new object[] { "الخدمات والانضباط والحضور", "إدارة الصيانة والمخالفات والحضور والغياب والتصاريح" });

            // ──────────────────────────────────────────────────────────
            // New Group 11: الإحصائيات (Statistics)
            // ──────────────────────────────────────────────────────────
            migrationBuilder.InsertData(
                table: "PermissionGroup",
                columns: new[] { "ID", "GroupName", "Description" },
                values: new object[] { 11, "الإحصائيات", "عرض وتصدير الإحصائيات والتقارير البيانية" });

            // ──────────────────────────────────────────────────────────
            // New Group 12: التقارير (Reports)
            // ──────────────────────────────────────────────────────────
            migrationBuilder.InsertData(
                table: "PermissionGroup",
                columns: new[] { "ID", "GroupName", "Description" },
                values: new object[] { 12, "التقارير", "تقارير النظام المتنوعة" });

            // ──────────────────────────────────────────────────────────
            // All new permissions
            // ──────────────────────────────────────────────────────────
            migrationBuilder.InsertData(
                table: "Permission",
                columns: new[] { "ID", "PermissionKey", "DisplayName", "Category", "GroupID" },
                values: new object[,]
                {
                    // Group 11: Statistics
                    { 53, "Statistics.View",   "عرض الإحصائيات",  null, 11 },
                    { 54, "Statistics.Export", "تصدير الإحصائيات", null, 11 },

                    // Group 3: Students (additional)
                    { 55, "Students.View",   "عرض بيانات الطلاب",       null, 3 },
                    { 56, "Students.Edit",   "تعديل بيانات الطلاب",     null, 3 },
                    { 57, "StudentStatus.View", "عرض بيان حالة الطالب",  null, 3 },
                    { 58, "SpecialCases.Manage", "إدارة الحالات الخاصة", null, 3 },
                    { 59, "SocialCases.Manage", "إدارة حالات البحث الاجتماعي", null, 3 },

                    // Group 4: Allocation
                    { 60, "Allocation.Manage",   "إدارة التسكين",     null, 4 },
                    { 61, "Allocation.Transfer", "نقل الطلاب",        null, 4 },
                    { 62, "Allocation.Evict",    "إخلاء الطلاب",      null, 4 },

                    // Group 5: Announcements
                    { 63, "Announcements.Manage", "إدارة الإعلانات", null, 5 },

                    // Group 7: Attendance sub-features + reports
                    { 64, "Attendance.ControlRoom",   "غرفة التحكم في الحضور",     null, 7 },
                    { 65, "Attendance.Enrollment",    "تسجيل وجوه الطلاب",         null, 7 },
                    { 66, "Attendance.DailyReport",   "التقرير اليومي للحضور",     null, 7 },
                    { 67, "Attendance.MonthlyReport", "التقرير الشهري للحضور",     null, 7 },
                    { 68, "Attendance.Monitoring",    "مراقبة API الحضور",         null, 7 },
                    { 69, "Attendance.Settings",      "إعدادات الحضور",            null, 7 },
                    { 70, "Violations.Report",        "تقارير المخالفات",          null, 7 },
                    { 71, "Attendance.Report",        "تقارير الغياب والتصاريح",   null, 7 },

                    // Group 8: Payments
                    { 72, "Payments.Report",    "تقارير المدفوعات",      null, 8 },
                    { 73, "Payments.GatewayLog","سجل بوابة الدفع",      null, 8 },

                    // Group 6: Meals
                    { 75, "Meals.Consume", "صرف الوجبات",     null, 6 },
                    { 76, "Meals.Report",  "تقارير الوجبات",  null, 6 },

                    // Group 12: Reports
                    { 77, "Reports.StudentLists",          "قوائم الطلاب",              null, 12 },
                    { 78, "Reports.RoomOccupancy",         "حالة الغرف",                null, 12 },
                    { 79, "Reports.PrintedCards",          "البطاقات المطبوعة",         null, 12 },
                    { 80, "Reports.StudentsWithoutPhotos", "طلاب بدون صور",             null, 12 },
                    { 81, "Reports.StudentObligations",    "بيان استلام الطلاب",        null, 12 },
                    { 82, "Reports.MealRestriction",       "المحرومون من الوجبات",      null, 12 },
                    { 83, "Reports.StudentMealHistory",    "وجبات الطالب",              null, 12 },
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove all new permissions
            migrationBuilder.DeleteData(table: "Permission", keyColumn: "ID", keyValues: new object[] {
                53, 54, 55, 56, 57, 58, 59, 60, 61, 62,
                63, 64, 65, 66, 67, 68, 69, 70, 71, 72,
                73, 75, 76, 77, 78, 79, 80, 81, 82, 83
            });

            // Remove new groups
            migrationBuilder.DeleteData(table: "PermissionGroup", keyColumn: "ID", keyValues: new object[] { 11, 12 });

            // Revert Group 7 name
            migrationBuilder.UpdateData(
                table: "PermissionGroup",
                keyColumn: "ID",
                keyValue: 7,
                columns: new[] { "GroupName", "Description" },
                values: new object[] { "الخدمات والانضباط", "إدارة الصيانة والمخالفات والحضور والمستندات" });

            // Revert Group 5 name
            migrationBuilder.UpdateData(
                table: "PermissionGroup",
                keyColumn: "ID",
                keyValue: 5,
                columns: new[] { "GroupName", "Description" },
                values: new object[] { "المواعيد والتعليمات", "إدارة المواعيد والتعليمات وفئات الطلاب" });

            // Revert Group 4 name
            migrationBuilder.UpdateData(
                table: "PermissionGroup",
                keyColumn: "ID",
                keyValue: 4,
                columns: new[] { "GroupName", "Description" },
                values: new object[] { "المدن والمباني", "إدارة المدن الجامعية والمباني والغرف" });

            // Revert Group 2 name
            migrationBuilder.UpdateData(
                table: "PermissionGroup",
                keyColumn: "ID",
                keyValue: 2,
                columns: new[] { "GroupName" },
                values: new object[] { "التقديم والتنسيق" });
        }
    }
}
