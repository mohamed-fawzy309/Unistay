using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniStay.Migrations
{
    /// <inheritdoc />
    public partial class SeedPermissionsData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ──────────────────────────────────────────────────────────
            // Permission Groups
            // ──────────────────────────────────────────────────────────
            migrationBuilder.InsertData(
                table: "PermissionGroup",
                columns: new[] { "ID", "GroupName", "Description" },
                values: new object[,]
                {
                    { 1, "لوحة التحكم", "إدارة لوحة التحكم والإحصائيات" },
                    { 2, "التقديم والتنسيق", "إدارة طلبات التقديم والتنسيق والتوزيع" },
                    { 3, "الطلاب", "إدارة بيانات الطلاب والخريجين والتسجيلات" },
                    { 4, "المدن والمباني", "إدارة المدن الجامعية والمباني والغرف" },
                    { 5, "المواعيد والتعليمات", "إدارة المواعيد والتعليمات وفئات الطلاب" },
                    { 6, "الوجبات والتغذية", "إدارة الوجبات وجداولها وأنواعها" },
                    { 7, "الخدمات والانضباط", "إدارة الصيانة والمخالفات والحضور والمستندات" },
                    { 8, "المالية", "إدارة المدفوعات وأنواع الرسوم وإعداداتها" },
                    { 9, "الإدارة", "إعدادات النظام والمستخدمين والأدوار والصلاحيات" }
                });

            // ──────────────────────────────────────────────────────────
            // Permissions
            // ──────────────────────────────────────────────────────────
            migrationBuilder.InsertData(
                table: "Permission",
                columns: new[] { "ID", "PermissionKey", "DisplayName", "Category", "GroupID" },
                values: new object[,]
                {
                    // Group 1: Dashboard
                    { 1,  "Dashboard.View",           "عرض لوحة التحكم",              null, 1 },

                    // Group 2: Applications & Coordination
                    { 2,  "Coordination.Manage",       "إدارة التنسيق",                null, 2 },
                    { 3,  "ApplicationSchedules.Manage","إدارة جداول التقديم",         null, 2 },
                    { 4,  "MonthlyAllocation.Manage",  "إدارة التوزيع الشهري",         null, 2 },
                    { 5,  "GuestAccommodation.Manage", "إدارة ضيافة الزوار",           null, 2 },

                    // Group 3: Students
                    { 6,  "Students.Manage",           "إدارة الطلاب",                 null, 3 },
                    { 7,  "GraduatedStudents.Manage",  "إدارة الخريجين",               null, 3 },
                    { 8,  "PendingRegistrations.Manage","إدارة التسجيلات المعلقة",      null, 3 },
                    { 9,  "RejectedApplications.Manage","إدارة الطلبات المرفوضة",       null, 3 },
                    { 10, "SpecialAccommodations.Manage","إدارة الإسكان الخاص",         null, 3 },

                    // Group 4: Cities & Buildings
                    { 11, "DormitoryCities.Manage",    "إدارة المدن الجامعية",         null, 4 },
                    { 12, "Buildings.Manage",          "إدارة المباني",                null, 4 },
                    { 13, "Floors.Manage",             "إدارة الطوابق",                null, 4 },
                    { 14, "Rooms.Manage",              "إدارة الغرف",                  null, 4 },
                    { 15, "Furniture.Manage",          "إدارة الأثاث",                 null, 4 },
                    { 16, "Villages.Manage",           "إدارة القرى",                  null, 4 },
                    { 17, "HousingTypes.Manage",       "إدارة أنواع السكن",            null, 4 },

                    // Group 5: Appointments & Instructions
                    { 18, "Appointments.Manage",       "إدارة المواعيد",               null, 5 },
                    { 19, "AppointmentInstructions.Manage","إدارة تعليمات المواعيد",    null, 5 },
                    { 20, "Instructions.Manage",       "إدارة التعليمات",              null, 5 },
                    { 21, "StudentCategories.Manage",  "إدارة فئات الطلاب",            null, 5 },

                    // Group 6: Meals & Nutrition
                    { 22, "Meals.Manage",              "إدارة الوجبات",                null, 6 },
                    { 23, "MealSchedules.Manage",      "إدارة جداول الوجبات",          null, 6 },
                    { 24, "MealItems.Manage",          "إدارة عناصر الوجبات",          null, 6 },
                    { 25, "MealTypes.Manage",          "إدارة أنواع الوجبات",          null, 6 },

                    // Group 7: Services & Discipline
                    { 26, "MaintenanceRequests.Manage","إدارة طلبات الصيانة",           null, 7 },
                    { 27, "Violations.Manage",         "إدارة المخالفات",              null, 7 },
                    { 28, "Attendance.Manage",         "إدارة الحضور",                 null, 7 },
                    { 29, "Documents.Manage",          "إدارة المستندات",              null, 7 },

                    // Group 8: Financial
                    { 30, "Payments.Manage",           "إدارة المدفوعات",              null, 8 },
                    { 31, "FeeTypes.Manage",           "إدارة أنواع الرسوم",           null, 8 },
                    { 32, "FeeConfigurations.Manage",  "إدارة إعدادات الرسوم",         null, 8 },

                    // Group 9: Administration
                    { 33, "Countries.Manage",          "إدارة الدول",                  null, 9 },
                    { 34, "AppConfig.Manage",          "إدارة إعدادات التطبيق",        null, 9 },
                    { 35, "SystemUsers.Manage",        "إدارة المستخدمين",             null, 9 },
                    { 36, "Roles.Manage",              "إدارة الأدوار",                null, 9 },
                    { 37, "Permissions.Manage",        "إدارة الصلاحيات",              null, 9 },
                    { 38, "AuditLog.View",             "عرض سجل النشاطات",             null, 9 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "Permission", keyColumn: "ID", keyValues: new object[] {
                1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
                11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
                21, 22, 23, 24, 25, 26, 27, 28, 29, 30,
                31, 32, 33, 34, 35, 36, 37, 38
            });
            migrationBuilder.DeleteData(table: "PermissionGroup", keyColumn: "ID", keyValues: new object[] {
                1, 2, 3, 4, 5, 6, 7, 8, 9
            });
        }
    }
}

