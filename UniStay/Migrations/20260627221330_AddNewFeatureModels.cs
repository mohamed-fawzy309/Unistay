using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniStay.Migrations
{
    /// <inheritdoc />
    public partial class AddNewFeatureModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmployeeRecord",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NationalID = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    JobTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    CreatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__EmployeeRecord__3214EC27", x => x.ID);
                    table.ForeignKey(
                        name: "FK_EmployeeRecord_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "GovernorateDistance",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GovernorateName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DistanceFromUniv = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    CreatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__GovernorateDistance__3214EC27", x => x.ID);
                    table.ForeignKey(
                        name: "FK_GovernorateDistance_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "HousingFeeTemplate",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FeeTypeID = table.Column<int>(type: "int", nullable: false),
                    DormitoryCityID = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    AcademicYear = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    InstallmentCount = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__HousingFeeTemplate__3214EC27", x => x.ID);
                    table.ForeignKey(
                        name: "FK_HousingFeeTemplate_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_HousingFeeTemplate_DormitoryCity",
                        column: x => x.DormitoryCityID,
                        principalTable: "DormitoryCity",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_HousingFeeTemplate_FeeType",
                        column: x => x.FeeTypeID,
                        principalTable: "FeeType",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_HousingFeeTemplate_LastUpdatedBy",
                        column: x => x.LastUpdatedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "PenaltyType",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Severity = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    DefaultFineAmount = table.Column<decimal>(type: "decimal(8,2)", nullable: true),
                    AffectsHousingEligibility = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    CreatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__PenaltyType__3214EC27", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PenaltyType_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "StudentFeeRecord",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentID = table.Column<int>(type: "int", nullable: false),
                    HousingFeeTemplateID = table.Column<int>(type: "int", nullable: false),
                    AllocationID = table.Column<int>(type: "int", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(8,2)", nullable: false, defaultValue: 0m),
                    InstallmentNumber = table.Column<int>(type: "int", nullable: false),
                    TotalInstallments = table.Column<int>(type: "int", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Pending"),
                    MonthYear = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RecordedBy = table.Column<int>(type: "int", nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__StudentFeeRecord__3214EC27", x => x.ID);
                    table.ForeignKey(
                        name: "FK_StudentFeeRecord_Allocation",
                        column: x => x.AllocationID,
                        principalTable: "Allocation",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_StudentFeeRecord_HousingFeeTemplate",
                        column: x => x.HousingFeeTemplateID,
                        principalTable: "HousingFeeTemplate",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_StudentFeeRecord_RecordedBy",
                        column: x => x.RecordedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_StudentFeeRecord_Student",
                        column: x => x.StudentID,
                        principalTable: "Student",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "StudentPenalty",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentID = table.Column<int>(type: "int", nullable: false),
                    PenaltyTypeID = table.Column<int>(type: "int", nullable: false),
                    DormitoryCityID = table.Column<int>(type: "int", nullable: true),
                    FineAmount = table.Column<decimal>(type: "decimal(8,2)", nullable: true),
                    FinePaid = table.Column<decimal>(type: "decimal(8,2)", nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Open"),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RecordedBy = table.Column<int>(type: "int", nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    ResolvedBy = table.Column<int>(type: "int", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__StudentPenalty__3214EC27", x => x.ID);
                    table.ForeignKey(
                        name: "FK_StudentPenalty_DormitoryCity",
                        column: x => x.DormitoryCityID,
                        principalTable: "DormitoryCity",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_StudentPenalty_PenaltyType",
                        column: x => x.PenaltyTypeID,
                        principalTable: "PenaltyType",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_StudentPenalty_RecordedBy",
                        column: x => x.RecordedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_StudentPenalty_ResolvedBy",
                        column: x => x.ResolvedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_StudentPenalty_Student",
                        column: x => x.StudentID,
                        principalTable: "Student",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRecord_CreatedBy",
                table: "EmployeeRecord",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "UQ_EmployeeRecord_Code",
                table: "EmployeeRecord",
                column: "EmployeeCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_EmployeeRecord_NationalID",
                table: "EmployeeRecord",
                column: "NationalID",
                unique: true,
                filter: "[NationalID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GovernorateDistance_CreatedBy",
                table: "GovernorateDistance",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "UQ_GovernorateDistance_Name",
                table: "GovernorateDistance",
                column: "GovernorateName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HousingFeeTemplate_CreatedBy",
                table: "HousingFeeTemplate",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_HousingFeeTemplate_DormitoryCityID",
                table: "HousingFeeTemplate",
                column: "DormitoryCityID");

            migrationBuilder.CreateIndex(
                name: "IX_HousingFeeTemplate_FeeTypeID",
                table: "HousingFeeTemplate",
                column: "FeeTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_HousingFeeTemplate_LastUpdatedBy",
                table: "HousingFeeTemplate",
                column: "LastUpdatedBy");

            migrationBuilder.CreateIndex(
                name: "UQ_HousingFeeTemplate_Name",
                table: "HousingFeeTemplate",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PenaltyType_CreatedBy",
                table: "PenaltyType",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "UQ_PenaltyType_Name",
                table: "PenaltyType",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentFeeRecord_AllocationID",
                table: "StudentFeeRecord",
                column: "AllocationID");

            migrationBuilder.CreateIndex(
                name: "IX_StudentFeeRecord_HousingFeeTemplateID",
                table: "StudentFeeRecord",
                column: "HousingFeeTemplateID");

            migrationBuilder.CreateIndex(
                name: "IX_StudentFeeRecord_RecordedBy",
                table: "StudentFeeRecord",
                column: "RecordedBy");

            migrationBuilder.CreateIndex(
                name: "IX_StudentFeeRecord_Status",
                table: "StudentFeeRecord",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_StudentFeeRecord_StudentID",
                table: "StudentFeeRecord",
                column: "StudentID");

            migrationBuilder.CreateIndex(
                name: "IX_StudentPenalty_DormitoryCityID",
                table: "StudentPenalty",
                column: "DormitoryCityID");

            migrationBuilder.CreateIndex(
                name: "IX_StudentPenalty_PenaltyTypeID",
                table: "StudentPenalty",
                column: "PenaltyTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_StudentPenalty_RecordedBy",
                table: "StudentPenalty",
                column: "RecordedBy");

            migrationBuilder.CreateIndex(
                name: "IX_StudentPenalty_ResolvedBy",
                table: "StudentPenalty",
                column: "ResolvedBy");

            migrationBuilder.CreateIndex(
                name: "IX_StudentPenalty_Status",
                table: "StudentPenalty",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_StudentPenalty_StudentID",
                table: "StudentPenalty",
                column: "StudentID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeRecord");

            migrationBuilder.DropTable(
                name: "GovernorateDistance");

            migrationBuilder.DropTable(
                name: "StudentFeeRecord");

            migrationBuilder.DropTable(
                name: "StudentPenalty");

            migrationBuilder.DropTable(
                name: "HousingFeeTemplate");

            migrationBuilder.DropTable(
                name: "PenaltyType");
        }
    }
}
