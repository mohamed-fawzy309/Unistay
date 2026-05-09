using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniStay.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLog",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    UserType = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TableName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RecordID = table.Column<int>(type: "int", nullable: true),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IPAddress = table.Column<string>(type: "varchar(45)", unicode: false, maxLength: 45, nullable: true),
                    DormitoryCityID = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__AuditLog__3214EC2744A6BC61", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "DataScope",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScopeType = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    ScopeValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__DataScop__3214EC276F43A621", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "InventoryItem",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItemName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ItemCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    ItemValue = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    TotalStock = table.Column<int>(type: "int", nullable: false),
                    AvailableStock = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: true, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Inventor__3214EC27C2B8EAF1", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "PermissionGroup",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Permissi__3214EC27E614522F", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "SystemUser",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    NationalID = table.Column<string>(type: "varchar(14)", unicode: false, maxLength: 14, nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsSuperAdmin = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    MustChangePassword = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__SystemUs__3214EC27F5EC8BFF", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SystemUser_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_SystemUser_LastUpdatedBy",
                        column: x => x.LastUpdatedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "University",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Logo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Phone = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    Website = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    APIBaseUrl = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    APIKey = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Universi__3214EC27362CEB9C", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Permission",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PermissionKey = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    GroupID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Permissi__3214EC2761DB10DB", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Permission_Group",
                        column: x => x.GroupID,
                        principalTable: "PermissionGroup",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "BulkOperationLog",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OperationType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AffectedCount = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    SuccessCount = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    FailedCount = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__BulkOper__3214EC2793E8F8BD", x => x.ID);
                    table.ForeignKey(
                        name: "FK_BulkOperationLog_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "Student",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NationalID = table.Column<string>(type: "varchar(14)", unicode: false, maxLength: 14, nullable: false),
                    StudentCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Gender = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Religion = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Nationality = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false, defaultValue: "Egyptian"),
                    Phone = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    Faculty = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AcademicYear = table.Column<byte>(type: "tinyint", nullable: true),
                    GradePercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    GradeText = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsEnrolled = table.Column<bool>(type: "bit", nullable: true, defaultValueSql: "(NULL)"),
                    Governorate = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DistanceFromUniv = table.Column<decimal>(type: "decimal(8,2)", nullable: true),
                    Photo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HasDisability = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    IsOrphan = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    IsLowIncome = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    HasFamilyAbroad = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    HasMedicalCondition = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    MedicalDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsForeign = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Student__3214EC2710B21BF6", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Student_LastUpdatedBy",
                        column: x => x.LastUpdatedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "UniversityAPISync",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NationalID = table.Column<string>(type: "varchar(14)", unicode: false, maxLength: 14, nullable: false),
                    StudentCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    SyncType = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    APIData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LocalData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsMatch = table.Column<bool>(type: "bit", nullable: true),
                    DifferenceDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    SyncedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Universi__3214EC275A106C57", x => x.ID);
                    table.ForeignKey(
                        name: "FK_UniversityAPISync_SyncedBy",
                        column: x => x.SyncedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "UserDataScope",
                columns: table => new
                {
                    SystemUserID = table.Column<int>(type: "int", nullable: false),
                    DataScopeID = table.Column<int>(type: "int", nullable: false),
                    DataScopeID1 = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__UserData__FDB49D8B7483810F", x => new { x.SystemUserID, x.DataScopeID });
                    table.ForeignKey(
                        name: "FK_UserDataScope_DataScope",
                        column: x => x.DataScopeID,
                        principalTable: "DataScope",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_UserDataScope_DataScope_DataScopeID1",
                        column: x => x.DataScopeID1,
                        principalTable: "DataScope",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_UserDataScope_SystemUser",
                        column: x => x.SystemUserID,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "DormitoryCity",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UniversityID = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CityType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Dormitor__3214EC27B65B16A9", x => x.ID);
                    table.ForeignKey(
                        name: "FK_DormitoryCity_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_DormitoryCity_LastUpdatedBy",
                        column: x => x.LastUpdatedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_DormitoryCity_University",
                        column: x => x.UniversityID,
                        principalTable: "University",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "UserPermission",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SystemUserID = table.Column<int>(type: "int", nullable: false),
                    PermissionID = table.Column<int>(type: "int", nullable: false),
                    CanView = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    CanCreate = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    CanEdit = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    CanDelete = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    GrantedBy = table.Column<int>(type: "int", nullable: true),
                    GrantedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__UserPerm__3214EC273AA19E9B", x => x.ID);
                    table.ForeignKey(
                        name: "FK_UserPermission_GrantedBy",
                        column: x => x.GrantedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_UserPermission_Permission",
                        column: x => x.PermissionID,
                        principalTable: "Permission",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_UserPermission_SystemUser",
                        column: x => x.SystemUserID,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "EmailLog",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecipientEmail = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    StudentID = table.Column<int>(type: "int", nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmailType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true, defaultValue: "Pending"),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__EmailLog__3214EC27E8524F0C", x => x.ID);
                    table.ForeignKey(
                        name: "FK_EmailLog_Student",
                        column: x => x.StudentID,
                        principalTable: "Student",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "Guardian",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentID = table.Column<int>(type: "int", nullable: false),
                    GuardianType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NationalID = table.Column<string>(type: "varchar(14)", unicode: false, maxLength: 14, nullable: true),
                    Phone = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Phone2 = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Job = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDeceased = table.Column<bool>(type: "bit", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Guardian__3214EC27D8FB476A", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Guardian_Student",
                        column: x => x.StudentID,
                        principalTable: "Student",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "IDCard",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentID = table.Column<int>(type: "int", nullable: false),
                    CardNumber = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Barcode = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    QRCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsPrinted = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    ReprintCount = table.Column<byte>(type: "tinyint", nullable: true, defaultValue: (byte)0),
                    PrintedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PrintedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__IDCard__3214EC270F63707B", x => x.ID);
                    table.ForeignKey(
                        name: "FK_IDCard_PrintedBy",
                        column: x => x.PrintedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_IDCard_Student",
                        column: x => x.StudentID,
                        principalTable: "Student",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "SocialCase",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentID = table.Column<int>(type: "int", nullable: false),
                    CaseType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true, defaultValue: "Open"),
                    Priority = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    AssignedTo = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
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
                name: "StudentDownloadLog",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentID = table.Column<int>(type: "int", nullable: false),
                    FormType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    DownloadedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    DownloadedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__StudentD__3214EC27846588A5", x => x.ID);
                    table.ForeignKey(
                        name: "FK_StudentDownloadLog_DownloadedBy",
                        column: x => x.DownloadedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_StudentDownloadLog_Student",
                        column: x => x.StudentID,
                        principalTable: "Student",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "StudentLogin",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentID = table.Column<int>(type: "int", nullable: false),
                    Username = table.Column<string>(type: "varchar(14)", unicode: false, maxLength: 14, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    FailedAttempts = table.Column<byte>(type: "tinyint", nullable: true, defaultValue: (byte)0),
                    LockedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MustChangePassword = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__StudentL__3214EC2766FB3B4D", x => x.ID);
                    table.ForeignKey(
                        name: "FK_StudentLogin_Student",
                        column: x => x.StudentID,
                        principalTable: "Student",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "StudentValidationLog",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentID = table.Column<int>(type: "int", nullable: false),
                    ValidationType = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    IssueSeverity = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    IssueDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsResolved = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    ResolvedBy = table.Column<int>(type: "int", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__StudentV__3214EC27B79FA509", x => x.ID);
                    table.ForeignKey(
                        name: "FK_StudentValidationLog_ResolvedBy",
                        column: x => x.ResolvedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_StudentValidationLog_Student",
                        column: x => x.StudentID,
                        principalTable: "Student",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "Absence",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentID = table.Column<int>(type: "int", nullable: false),
                    DormitoryCityID = table.Column<int>(type: "int", nullable: false),
                    AbsenceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ToDate = table.Column<DateOnly>(type: "date", nullable: true),
                    AbsenceType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Pending"),
                    RequestedBy = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Student"),
                    GuardianName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    GuardianRelation = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    GuardianPhone = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReviewedBy = table.Column<int>(type: "int", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Absence__3214EC278A1614AC", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Absence_DormitoryCity",
                        column: x => x.DormitoryCityID,
                        principalTable: "DormitoryCity",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Absence_ReviewedBy",
                        column: x => x.ReviewedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Absence_Student",
                        column: x => x.StudentID,
                        principalTable: "Student",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "Announcement",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AnnouncementType = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    DormitoryCityID = table.Column<int>(type: "int", nullable: true),
                    TargetAudience = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true, defaultValue: "All"),
                    IsPublished = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Announce__3214EC270FA560D0", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Announcement_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Announcement_DormitoryCity",
                        column: x => x.DormitoryCityID,
                        principalTable: "DormitoryCity",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "Application",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentID = table.Column<int>(type: "int", nullable: false),
                    DormitoryCityID = table.Column<int>(type: "int", nullable: false),
                    AcademicYear = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    StudentType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    HousingType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    MealSubscription = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    HasSpecialNeeds = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    SpecialNeedsDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "Pending"),
                    ReviewedBy = table.Column<int>(type: "int", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AdminNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CoordinationScore = table.Column<decimal>(type: "decimal(8,2)", nullable: true),
                    CoordinationRank = table.Column<int>(type: "int", nullable: true),
                    ServerVerificationStatus = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "NotChecked"),
                    ServerVerificationAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ServerVerificationBy = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Applicat__3214EC27488E77E6", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Application_DormitoryCity",
                        column: x => x.DormitoryCityID,
                        principalTable: "DormitoryCity",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Application_LastUpdatedBy",
                        column: x => x.LastUpdatedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Application_ReviewedBy",
                        column: x => x.ReviewedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Application_ServerVerBy",
                        column: x => x.ServerVerificationBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Application_Student",
                        column: x => x.StudentID,
                        principalTable: "Student",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "ApplicationSchedule",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DormitoryCityID = table.Column<int>(type: "int", nullable: false),
                    AcademicYear = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    NewStudentsOpenDate = table.Column<DateOnly>(type: "date", nullable: true),
                    NewStudentsCloseDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ReturningStudentsOpenDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ReturningStudentsCloseDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Applicat__3214EC27452C7B0C", x => x.ID);
                    table.ForeignKey(
                        name: "FK_AppSchedule_DormitoryCity",
                        column: x => x.DormitoryCityID,
                        principalTable: "DormitoryCity",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "CardPrintQueue",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentID = table.Column<int>(type: "int", nullable: false),
                    DormitoryCityID = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true, defaultValue: "Pending"),
                    QueuedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    PrintedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PrintedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__CardPrin__3214EC274810518E", x => x.ID);
                    table.ForeignKey(
                        name: "FK_CardPrintQueue_DormitoryCity",
                        column: x => x.DormitoryCityID,
                        principalTable: "DormitoryCity",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_CardPrintQueue_PrintedBy",
                        column: x => x.PrintedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_CardPrintQueue_Student",
                        column: x => x.StudentID,
                        principalTable: "Student",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "CityBuilding",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DormitoryCityID = table.Column<int>(type: "int", nullable: false),
                    BuildingName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BuildingType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    FloorCount = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)1),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__CityBuil__3214EC27E20E5827", x => x.ID);
                    table.ForeignKey(
                        name: "FK_CityBuilding_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_CityBuilding_DormitoryCity",
                        column: x => x.DormitoryCityID,
                        principalTable: "DormitoryCity",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_CityBuilding_LastUpdatedBy",
                        column: x => x.LastUpdatedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "CityConfiguration",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DormitoryCityID = table.Column<int>(type: "int", nullable: false),
                    StandardFee = table.Column<decimal>(type: "decimal(10,2)", nullable: true, defaultValue: 0m),
                    PremiumFee = table.Column<decimal>(type: "decimal(10,2)", nullable: true, defaultValue: 0m),
                    VIPFee = table.Column<decimal>(type: "decimal(10,2)", nullable: true, defaultValue: 0m),
                    ForeignStudentFee = table.Column<decimal>(type: "decimal(10,2)", nullable: true, defaultValue: 0m),
                    SecurityDeposit = table.Column<decimal>(type: "decimal(10,2)", nullable: true, defaultValue: 0m),
                    MealFee = table.Column<decimal>(type: "decimal(10,2)", nullable: true, defaultValue: 0m),
                    RamadanMealFee = table.Column<decimal>(type: "decimal(10,2)", nullable: true, defaultValue: 0m),
                    ChristianMealFee = table.Column<decimal>(type: "decimal(10,2)", nullable: true, defaultValue: 0m),
                    NewStudentsOpenDate = table.Column<DateOnly>(type: "date", nullable: true),
                    NewStudentsCloseDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ReturningStudentsOpenDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ReturningStudentsCloseDate = table.Column<DateOnly>(type: "date", nullable: true),
                    MinDistanceKm = table.Column<decimal>(type: "decimal(8,2)", nullable: true, defaultValue: 0m),
                    MinGradePercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: true, defaultValue: 0m),
                    MaxAge = table.Column<byte>(type: "tinyint", nullable: true, defaultValue: (byte)28),
                    AutoCoordinationEnabled = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    ExcludedFaculties = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AllowedFacultiesOnly = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaxBedsPerRoom = table.Column<byte>(type: "tinyint", nullable: true, defaultValue: (byte)4),
                    AllowStudentBedSelection = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__CityConf__3214EC276A904117", x => x.ID);
                    table.ForeignKey(
                        name: "FK_CityConfig_DormitoryCity",
                        column: x => x.DormitoryCityID,
                        principalTable: "DormitoryCity",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_CityConfig_LastUpdatedBy",
                        column: x => x.LastUpdatedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "CityStaff",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SystemUserID = table.Column<int>(type: "int", nullable: false),
                    DormitoryCityID = table.Column<int>(type: "int", nullable: false),
                    RoleInCity = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    AssignedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__CityStaf__3214EC275F511772", x => x.ID);
                    table.ForeignKey(
                        name: "FK_CityStaff_AssignedBy",
                        column: x => x.AssignedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_CityStaff_DormitoryCity",
                        column: x => x.DormitoryCityID,
                        principalTable: "DormitoryCity",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_CityStaff_SystemUser",
                        column: x => x.SystemUserID,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "CoordinationRule",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DormitoryCityID = table.Column<int>(type: "int", nullable: false),
                    RuleName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RuleType = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    Priority = table.Column<byte>(type: "tinyint", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    CreatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Coordina__3214EC27AB6DE2A9", x => x.ID);
                    table.ForeignKey(
                        name: "FK_CoordRule_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_CoordRule_DormitoryCity",
                        column: x => x.DormitoryCityID,
                        principalTable: "DormitoryCity",
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
                    Faculty = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MaxQuota = table.Column<int>(type: "int", nullable: false),
                    MinQuota = table.Column<int>(type: "int", nullable: false),
                    CurrentCount = table.Column<int>(type: "int", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "HousingInstruction",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DormitoryCityID = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InstructionType = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    SortOrder = table.Column<byte>(type: "tinyint", nullable: true, defaultValue: (byte)0),
                    IsActive = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__HousingI__3214EC274B1664FE", x => x.ID);
                    table.ForeignKey(
                        name: "FK_HousingInstruction_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_HousingInstruction_DormitoryCity",
                        column: x => x.DormitoryCityID,
                        principalTable: "DormitoryCity",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "Meal",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentID = table.Column<int>(type: "int", nullable: false),
                    DormitoryCityID = table.Column<int>(type: "int", nullable: false),
                    MealDate = table.Column<DateOnly>(type: "date", nullable: false),
                    MealType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    IsBooked = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    IsConsumed = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    Price = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    CancelReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Meal__3214EC27623917A5", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Meal_DormitoryCity",
                        column: x => x.DormitoryCityID,
                        principalTable: "DormitoryCity",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Meal_Student",
                        column: x => x.StudentID,
                        principalTable: "Student",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "MealBlock",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentID = table.Column<int>(type: "int", nullable: false),
                    DormitoryCityID = table.Column<int>(type: "int", nullable: false),
                    FromDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ToDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__MealBloc__3214EC27A368D3D5", x => x.ID);
                    table.ForeignKey(
                        name: "FK_MealBlock_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_MealBlock_DormitoryCity",
                        column: x => x.DormitoryCityID,
                        principalTable: "DormitoryCity",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_MealBlock_Student",
                        column: x => x.StudentID,
                        principalTable: "Student",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "MealCancellation",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentID = table.Column<int>(type: "int", nullable: true),
                    DormitoryCityID = table.Column<int>(type: "int", nullable: false),
                    FromDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ToDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CancellationType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__MealCanc__3214EC271DFA6834", x => x.ID);
                    table.ForeignKey(
                        name: "FK_MealCancellation_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_MealCancellation_DormitoryCity",
                        column: x => x.DormitoryCityID,
                        principalTable: "DormitoryCity",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_MealCancellation_Student",
                        column: x => x.StudentID,
                        principalTable: "Student",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "MealSchedule",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DormitoryCityID = table.Column<int>(type: "int", nullable: false),
                    ScheduleDate = table.Column<DateOnly>(type: "date", nullable: false),
                    MealType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    SpecialPrice = table.Column<decimal>(type: "decimal(8,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__MealSche__3214EC2775A604ED", x => x.ID);
                    table.ForeignKey(
                        name: "FK_MealSchedule_DormitoryCity",
                        column: x => x.DormitoryCityID,
                        principalTable: "DormitoryCity",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "UniversityPhoto",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DormitoryCityID = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PhotoType = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true, defaultValue: "Campus"),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<byte>(type: "tinyint", nullable: true, defaultValue: (byte)0),
                    IsActive = table.Column<bool>(type: "bit", nullable: true, defaultValue: true)
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
                name: "Violation",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentID = table.Column<int>(type: "int", nullable: false),
                    DormitoryCityID = table.Column<int>(type: "int", nullable: false),
                    ViolationType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Severity = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    FineAmount = table.Column<decimal>(type: "decimal(8,2)", nullable: true, defaultValue: 0m),
                    FinePaid = table.Column<decimal>(type: "decimal(8,2)", nullable: true, defaultValue: 0m),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Active"),
                    IsOnBlacklist = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    RecordedBy = table.Column<int>(type: "int", nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    ResolvedBy = table.Column<int>(type: "int", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Violatio__3214EC27000AA28B", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Violation_DormitoryCity",
                        column: x => x.DormitoryCityID,
                        principalTable: "DormitoryCity",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Violation_RecordedBy",
                        column: x => x.RecordedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Violation_ResolvedBy",
                        column: x => x.ResolvedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Violation_Student",
                        column: x => x.StudentID,
                        principalTable: "Student",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "AnnouncementAttachment",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnnouncementID = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Announce__3214EC2747AE27AE", x => x.ID);
                    table.ForeignKey(
                        name: "FK_AnnouncementAttach_Announcement",
                        column: x => x.AnnouncementID,
                        principalTable: "Announcement",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "CoordinationResult",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationID = table.Column<int>(type: "int", nullable: false),
                    StudentID = table.Column<int>(type: "int", nullable: false),
                    DormitoryCityID = table.Column<int>(type: "int", nullable: false),
                    AcademicYear = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    DistanceScore = table.Column<decimal>(type: "decimal(5,2)", nullable: true, defaultValue: 0m),
                    GradeScore = table.Column<decimal>(type: "decimal(5,2)", nullable: true, defaultValue: 0m),
                    AgeScore = table.Column<decimal>(type: "decimal(5,2)", nullable: true, defaultValue: 0m),
                    SpecialBonus = table.Column<decimal>(type: "decimal(5,2)", nullable: true, defaultValue: 0m),
                    TotalScore = table.Column<decimal>(type: "decimal(8,2)", nullable: true, defaultValue: 0m),
                    Rank = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true, defaultValue: "Pending"),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Coordina__3214EC2716F99F24", x => x.ID);
                    table.ForeignKey(
                        name: "FK_CoordResult_Application",
                        column: x => x.ApplicationID,
                        principalTable: "Application",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_CoordResult_DormitoryCity",
                        column: x => x.DormitoryCityID,
                        principalTable: "DormitoryCity",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_CoordResult_ProcessedBy",
                        column: x => x.ProcessedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_CoordResult_Student",
                        column: x => x.StudentID,
                        principalTable: "Student",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "Document",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentID = table.Column<int>(type: "int", nullable: false),
                    ApplicationID = table.Column<int>(type: "int", nullable: true),
                    DocumentType = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsVerified = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    VerifiedBy = table.Column<int>(type: "int", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())")
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
                name: "SpecialCase",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationID = table.Column<int>(type: "int", nullable: false),
                    StudentID = table.Column<int>(type: "int", nullable: false),
                    CaseType = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SupportingDocuments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true, defaultValue: "Pending"),
                    ReviewedBy = table.Column<int>(type: "int", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())")
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
                name: "CityRoom",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CityBuildingID = table.Column<int>(type: "int", nullable: false),
                    RoomNumber = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    FloorNumber = table.Column<byte>(type: "tinyint", nullable: false),
                    BedsCount = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)4),
                    CurrentOccupancy = table.Column<byte>(type: "tinyint", nullable: false),
                    RoomType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true, defaultValue: "Standard"),
                    HasAC = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    HasBalcony = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    HasPrivateBathroom = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    HasFridge = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__CityRoom__3214EC27A9B9513C", x => x.ID);
                    table.ForeignKey(
                        name: "FK_CityRoom_CityBuilding",
                        column: x => x.CityBuildingID,
                        principalTable: "CityBuilding",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_CityRoom_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_CityRoom_LastUpdatedBy",
                        column: x => x.LastUpdatedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "DormitoryBlock",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CityBuildingID = table.Column<int>(type: "int", nullable: false),
                    FloorNumber = table.Column<byte>(type: "tinyint", nullable: true),
                    Faculty = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AcademicYear = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    MaxStudents = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Dormitor__3214EC27860406F6", x => x.ID);
                    table.ForeignKey(
                        name: "FK_DormitoryBlock_CityBuilding",
                        column: x => x.CityBuildingID,
                        principalTable: "CityBuilding",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "HousingInstructionAttachment",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HousingInstructionID = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileType = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    SortOrder = table.Column<byte>(type: "tinyint", nullable: true, defaultValue: (byte)0),
                    IsActive = table.Column<bool>(type: "bit", nullable: true, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__HousingI__3214EC2723971597", x => x.ID);
                    table.ForeignKey(
                        name: "FK_HousingInstructionAttach_Instruction",
                        column: x => x.HousingInstructionID,
                        principalTable: "HousingInstruction",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "MealConsumption",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentID = table.Column<int>(type: "int", nullable: false),
                    MealID = table.Column<int>(type: "int", nullable: false),
                    DormitoryCityID = table.Column<int>(type: "int", nullable: false),
                    MealDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ScanMethod = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    RecordedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__MealCons__3214EC2764233F48", x => x.ID);
                    table.ForeignKey(
                        name: "FK_MealConsumption_DormitoryCity",
                        column: x => x.DormitoryCityID,
                        principalTable: "DormitoryCity",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_MealConsumption_Meal",
                        column: x => x.MealID,
                        principalTable: "Meal",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_MealConsumption_RecordedBy",
                        column: x => x.RecordedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_MealConsumption_Student",
                        column: x => x.StudentID,
                        principalTable: "Student",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "Allocation",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationID = table.Column<int>(type: "int", nullable: false),
                    StudentID = table.Column<int>(type: "int", nullable: false),
                    CityRoomID = table.Column<int>(type: "int", nullable: false),
                    BedNumber = table.Column<byte>(type: "tinyint", nullable: false),
                    AcademicYear = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Active"),
                    AllocatedBy = table.Column<int>(type: "int", nullable: true),
                    AllocatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Allocati__3214EC27971EDB22", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Allocation_AllocatedBy",
                        column: x => x.AllocatedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Allocation_Application",
                        column: x => x.ApplicationID,
                        principalTable: "Application",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Allocation_CityRoom",
                        column: x => x.CityRoomID,
                        principalTable: "CityRoom",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Allocation_Student",
                        column: x => x.StudentID,
                        principalTable: "Student",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceRequest",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentID = table.Column<int>(type: "int", nullable: false),
                    CityRoomID = table.Column<int>(type: "int", nullable: false),
                    DormitoryCityID = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Priority = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Normal"),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Pending"),
                    AssignedTo = table.Column<int>(type: "int", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Maintena__3214EC275CDF3008", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Maintenance_AssignedTo",
                        column: x => x.AssignedTo,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Maintenance_CityRoom",
                        column: x => x.CityRoomID,
                        principalTable: "CityRoom",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Maintenance_DormitoryCity",
                        column: x => x.DormitoryCityID,
                        principalTable: "DormitoryCity",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Maintenance_Student",
                        column: x => x.StudentID,
                        principalTable: "Student",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "EvictionNotice",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentID = table.Column<int>(type: "int", nullable: false),
                    AllocationID = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EvictionType = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true, defaultValue: "Pending"),
                    IssuedBy = table.Column<int>(type: "int", nullable: true),
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    ExecutedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Eviction__3214EC27F03D648E", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Eviction_Allocation",
                        column: x => x.AllocationID,
                        principalTable: "Allocation",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Eviction_IssuedBy",
                        column: x => x.IssuedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Eviction_Student",
                        column: x => x.StudentID,
                        principalTable: "Student",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "Payment",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentID = table.Column<int>(type: "int", nullable: false),
                    ApplicationID = table.Column<int>(type: "int", nullable: true),
                    AllocationID = table.Column<int>(type: "int", nullable: true),
                    PaymentType = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Pending"),
                    PaymentMethod = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    ReceiptNumber = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    AcademicYear = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    RecordedBy = table.Column<int>(type: "int", nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Payment__3214EC27C37C5727", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Payment_Allocation",
                        column: x => x.AllocationID,
                        principalTable: "Allocation",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Payment_Application",
                        column: x => x.ApplicationID,
                        principalTable: "Application",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Payment_RecordedBy",
                        column: x => x.RecordedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Payment_Student",
                        column: x => x.StudentID,
                        principalTable: "Student",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "StudentInventory",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentID = table.Column<int>(type: "int", nullable: false),
                    InventoryItemID = table.Column<int>(type: "int", nullable: false),
                    AllocationID = table.Column<int>(type: "int", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Condition = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true, defaultValue: "Good"),
                    DeductionAmount = table.Column<decimal>(type: "decimal(8,2)", nullable: true, defaultValue: 0m),
                    IsReturned = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    AssignedBy = table.Column<int>(type: "int", nullable: true),
                    ReturnedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReturnedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__StudentI__3214EC27C50AF97A", x => x.ID);
                    table.ForeignKey(
                        name: "FK_StudentInventory_Allocation",
                        column: x => x.AllocationID,
                        principalTable: "Allocation",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_StudentInventory_AssignedBy",
                        column: x => x.AssignedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_StudentInventory_Item",
                        column: x => x.InventoryItemID,
                        principalTable: "InventoryItem",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_StudentInventory_ReturnedBy",
                        column: x => x.ReturnedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_StudentInventory_Student",
                        column: x => x.StudentID,
                        principalTable: "Student",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "PaymentGatewayLog",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentID = table.Column<int>(type: "int", nullable: false),
                    StudentID = table.Column<int>(type: "int", nullable: false),
                    GatewayType = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    TransactionID = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    GatewayResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__PaymentG__3214EC2761DD9E30", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PaymentGateway_Payment",
                        column: x => x.PaymentID,
                        principalTable: "Payment",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_PaymentGateway_Student",
                        column: x => x.StudentID,
                        principalTable: "Student",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Absence_DormitoryCityID",
                table: "Absence",
                column: "DormitoryCityID");

            migrationBuilder.CreateIndex(
                name: "IX_Absence_ReviewedBy",
                table: "Absence",
                column: "ReviewedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Absence_Status",
                table: "Absence",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Absence_StudentID",
                table: "Absence",
                column: "StudentID");

            migrationBuilder.CreateIndex(
                name: "IX_Absence_ToDate",
                table: "Absence",
                column: "ToDate");

            migrationBuilder.CreateIndex(
                name: "IX_Allocation_AcademicYear",
                table: "Allocation",
                column: "AcademicYear");

            migrationBuilder.CreateIndex(
                name: "IX_Allocation_AllocatedBy",
                table: "Allocation",
                column: "AllocatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Allocation_CityRoomID",
                table: "Allocation",
                column: "CityRoomID");

            migrationBuilder.CreateIndex(
                name: "IX_Allocation_Status",
                table: "Allocation",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Allocation_StudentID",
                table: "Allocation",
                column: "StudentID");

            migrationBuilder.CreateIndex(
                name: "UQ_Allocation_Application",
                table: "Allocation",
                column: "ApplicationID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Allocation_Bed",
                table: "Allocation",
                columns: new[] { "CityRoomID", "BedNumber", "AcademicYear" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Announcement_CreatedBy",
                table: "Announcement",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Announcement_DormitoryCityID",
                table: "Announcement",
                column: "DormitoryCityID");

            migrationBuilder.CreateIndex(
                name: "IX_AnnouncementAttachment_AnnouncementID",
                table: "AnnouncementAttachment",
                column: "AnnouncementID");

            migrationBuilder.CreateIndex(
                name: "IX_Application_AcademicYear",
                table: "Application",
                column: "AcademicYear");

            migrationBuilder.CreateIndex(
                name: "IX_Application_DormitoryCityID",
                table: "Application",
                column: "DormitoryCityID");

            migrationBuilder.CreateIndex(
                name: "IX_Application_LastUpdatedBy",
                table: "Application",
                column: "LastUpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Application_ReviewedBy",
                table: "Application",
                column: "ReviewedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Application_ServerVerificationBy",
                table: "Application",
                column: "ServerVerificationBy");

            migrationBuilder.CreateIndex(
                name: "IX_Application_Status",
                table: "Application",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Application_StudentID",
                table: "Application",
                column: "StudentID");

            migrationBuilder.CreateIndex(
                name: "UQ_Application_StudentYear",
                table: "Application",
                columns: new[] { "StudentID", "AcademicYear" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationSchedule_DormitoryCityID",
                table: "ApplicationSchedule",
                column: "DormitoryCityID");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_CreatedAt",
                table: "AuditLog",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_TableName",
                table: "AuditLog",
                column: "TableName");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_UserID",
                table: "AuditLog",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_BulkOperationLog_CreatedBy",
                table: "BulkOperationLog",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CardPrintQueue_DormitoryCityID",
                table: "CardPrintQueue",
                column: "DormitoryCityID");

            migrationBuilder.CreateIndex(
                name: "IX_CardPrintQueue_PrintedBy",
                table: "CardPrintQueue",
                column: "PrintedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CardPrintQueue_StudentID",
                table: "CardPrintQueue",
                column: "StudentID");

            migrationBuilder.CreateIndex(
                name: "IX_CityBuilding_CreatedBy",
                table: "CityBuilding",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CityBuilding_DormitoryCityID",
                table: "CityBuilding",
                column: "DormitoryCityID");

            migrationBuilder.CreateIndex(
                name: "IX_CityBuilding_LastUpdatedBy",
                table: "CityBuilding",
                column: "LastUpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CityConfiguration_DormitoryCityID",
                table: "CityConfiguration",
                column: "DormitoryCityID");

            migrationBuilder.CreateIndex(
                name: "IX_CityConfiguration_LastUpdatedBy",
                table: "CityConfiguration",
                column: "LastUpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CityRoom_CityBuildingID",
                table: "CityRoom",
                column: "CityBuildingID");

            migrationBuilder.CreateIndex(
                name: "IX_CityRoom_CreatedBy",
                table: "CityRoom",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CityRoom_LastUpdatedBy",
                table: "CityRoom",
                column: "LastUpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CityStaff_AssignedBy",
                table: "CityStaff",
                column: "AssignedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CityStaff_DormitoryCityID",
                table: "CityStaff",
                column: "DormitoryCityID");

            migrationBuilder.CreateIndex(
                name: "UQ_CityStaff_UserCity",
                table: "CityStaff",
                columns: new[] { "SystemUserID", "DormitoryCityID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoordinationResult_ApplicationID",
                table: "CoordinationResult",
                column: "ApplicationID");

            migrationBuilder.CreateIndex(
                name: "IX_CoordinationResult_DormitoryCityID",
                table: "CoordinationResult",
                column: "DormitoryCityID");

            migrationBuilder.CreateIndex(
                name: "IX_CoordinationResult_ProcessedBy",
                table: "CoordinationResult",
                column: "ProcessedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CoordinationResult_StudentID",
                table: "CoordinationResult",
                column: "StudentID");

            migrationBuilder.CreateIndex(
                name: "IX_CoordinationRule_CreatedBy",
                table: "CoordinationRule",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CoordinationRule_DormitoryCityID",
                table: "CoordinationRule",
                column: "DormitoryCityID");

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
                name: "IX_DormitoryBlock_CityBuildingID",
                table: "DormitoryBlock",
                column: "CityBuildingID");

            migrationBuilder.CreateIndex(
                name: "IX_DormitoryCity_CreatedBy",
                table: "DormitoryCity",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DormitoryCity_LastUpdatedBy",
                table: "DormitoryCity",
                column: "LastUpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DormitoryCity_UniversityID",
                table: "DormitoryCity",
                column: "UniversityID");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLog_Status",
                table: "EmailLog",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLog_StudentID",
                table: "EmailLog",
                column: "StudentID");

            migrationBuilder.CreateIndex(
                name: "IX_EvictionNotice_AllocationID",
                table: "EvictionNotice",
                column: "AllocationID");

            migrationBuilder.CreateIndex(
                name: "IX_EvictionNotice_IssuedBy",
                table: "EvictionNotice",
                column: "IssuedBy");

            migrationBuilder.CreateIndex(
                name: "IX_EvictionNotice_StudentID",
                table: "EvictionNotice",
                column: "StudentID");

            migrationBuilder.CreateIndex(
                name: "IX_FacultyQuota_DormitoryCityID",
                table: "FacultyQuota",
                column: "DormitoryCityID");

            migrationBuilder.CreateIndex(
                name: "IX_Guardian_StudentID",
                table: "Guardian",
                column: "StudentID");

            migrationBuilder.CreateIndex(
                name: "IX_HousingInstruction_CreatedBy",
                table: "HousingInstruction",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_HousingInstruction_DormitoryCityID",
                table: "HousingInstruction",
                column: "DormitoryCityID");

            migrationBuilder.CreateIndex(
                name: "IX_HousingInstructionAttachment_HousingInstructionID",
                table: "HousingInstructionAttachment",
                column: "HousingInstructionID");

            migrationBuilder.CreateIndex(
                name: "IX_IDCard_PrintedBy",
                table: "IDCard",
                column: "PrintedBy");

            migrationBuilder.CreateIndex(
                name: "IX_IDCard_StudentID",
                table: "IDCard",
                column: "StudentID");

            migrationBuilder.CreateIndex(
                name: "UQ_IDCard_CardNumber",
                table: "IDCard",
                column: "CardNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_InventoryItem_Code",
                table: "InventoryItem",
                column: "ItemCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRequest_AssignedTo",
                table: "MaintenanceRequest",
                column: "AssignedTo");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRequest_CityRoomID",
                table: "MaintenanceRequest",
                column: "CityRoomID");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRequest_DormitoryCityID",
                table: "MaintenanceRequest",
                column: "DormitoryCityID");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRequest_StudentID",
                table: "MaintenanceRequest",
                column: "StudentID");

            migrationBuilder.CreateIndex(
                name: "IX_Meal_DormitoryCityID",
                table: "Meal",
                column: "DormitoryCityID");

            migrationBuilder.CreateIndex(
                name: "IX_Meal_MealDate",
                table: "Meal",
                column: "MealDate");

            migrationBuilder.CreateIndex(
                name: "IX_Meal_StudentID",
                table: "Meal",
                column: "StudentID");

            migrationBuilder.CreateIndex(
                name: "IX_MealBlock_CreatedBy",
                table: "MealBlock",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_MealBlock_DormitoryCityID",
                table: "MealBlock",
                column: "DormitoryCityID");

            migrationBuilder.CreateIndex(
                name: "IX_MealBlock_StudentID",
                table: "MealBlock",
                column: "StudentID");

            migrationBuilder.CreateIndex(
                name: "IX_MealCancellation_CreatedBy",
                table: "MealCancellation",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_MealCancellation_DormitoryCityID",
                table: "MealCancellation",
                column: "DormitoryCityID");

            migrationBuilder.CreateIndex(
                name: "IX_MealCancellation_StudentID",
                table: "MealCancellation",
                column: "StudentID");

            migrationBuilder.CreateIndex(
                name: "IX_MealConsumption_DormitoryCityID",
                table: "MealConsumption",
                column: "DormitoryCityID");

            migrationBuilder.CreateIndex(
                name: "IX_MealConsumption_MealID",
                table: "MealConsumption",
                column: "MealID");

            migrationBuilder.CreateIndex(
                name: "IX_MealConsumption_RecordedBy",
                table: "MealConsumption",
                column: "RecordedBy");

            migrationBuilder.CreateIndex(
                name: "IX_MealConsumption_StudentID",
                table: "MealConsumption",
                column: "StudentID");

            migrationBuilder.CreateIndex(
                name: "IX_MealSchedule_DormitoryCityID",
                table: "MealSchedule",
                column: "DormitoryCityID");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_AcademicYear",
                table: "Payment",
                column: "AcademicYear");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_AllocationID",
                table: "Payment",
                column: "AllocationID");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_ApplicationID",
                table: "Payment",
                column: "ApplicationID");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_RecordedBy",
                table: "Payment",
                column: "RecordedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_Status",
                table: "Payment",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_StudentID",
                table: "Payment",
                column: "StudentID");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentGatewayLog_PaymentID",
                table: "PaymentGatewayLog",
                column: "PaymentID");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentGatewayLog_StudentID",
                table: "PaymentGatewayLog",
                column: "StudentID");

            migrationBuilder.CreateIndex(
                name: "IX_Permission_GroupID",
                table: "Permission",
                column: "GroupID");

            migrationBuilder.CreateIndex(
                name: "UQ_Permission_Key",
                table: "Permission",
                column: "PermissionKey",
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
                name: "IX_Student_Faculty",
                table: "Student",
                column: "Faculty");

            migrationBuilder.CreateIndex(
                name: "IX_Student_Gender",
                table: "Student",
                column: "Gender");

            migrationBuilder.CreateIndex(
                name: "IX_Student_IsDeleted",
                table: "Student",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Student_LastUpdatedBy",
                table: "Student",
                column: "LastUpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Student_NationalID",
                table: "Student",
                column: "NationalID");

            migrationBuilder.CreateIndex(
                name: "UQ_Student_NationalID",
                table: "Student",
                column: "NationalID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentDownloadLog_DownloadedBy",
                table: "StudentDownloadLog",
                column: "DownloadedBy");

            migrationBuilder.CreateIndex(
                name: "IX_StudentDownloadLog_StudentID",
                table: "StudentDownloadLog",
                column: "StudentID");

            migrationBuilder.CreateIndex(
                name: "IX_StudentInventory_AllocationID",
                table: "StudentInventory",
                column: "AllocationID");

            migrationBuilder.CreateIndex(
                name: "IX_StudentInventory_AssignedBy",
                table: "StudentInventory",
                column: "AssignedBy");

            migrationBuilder.CreateIndex(
                name: "IX_StudentInventory_InventoryItemID",
                table: "StudentInventory",
                column: "InventoryItemID");

            migrationBuilder.CreateIndex(
                name: "IX_StudentInventory_ReturnedBy",
                table: "StudentInventory",
                column: "ReturnedBy");

            migrationBuilder.CreateIndex(
                name: "IX_StudentInventory_StudentID",
                table: "StudentInventory",
                column: "StudentID");

            migrationBuilder.CreateIndex(
                name: "UQ_StudentLogin_Student",
                table: "StudentLogin",
                column: "StudentID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_StudentLogin_Username",
                table: "StudentLogin",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentValidationLog_ResolvedBy",
                table: "StudentValidationLog",
                column: "ResolvedBy");

            migrationBuilder.CreateIndex(
                name: "IX_StudentValidationLog_StudentID",
                table: "StudentValidationLog",
                column: "StudentID");

            migrationBuilder.CreateIndex(
                name: "IX_SystemUser_CreatedBy",
                table: "SystemUser",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SystemUser_LastUpdatedBy",
                table: "SystemUser",
                column: "LastUpdatedBy");

            migrationBuilder.CreateIndex(
                name: "UQ_SystemUser_NationalID",
                table: "SystemUser",
                column: "NationalID",
                unique: true,
                filter: "[NationalID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UniversityAPISync_SyncedBy",
                table: "UniversityAPISync",
                column: "SyncedBy");

            migrationBuilder.CreateIndex(
                name: "IX_UniversityPhoto_DormitoryCityID",
                table: "UniversityPhoto",
                column: "DormitoryCityID");

            migrationBuilder.CreateIndex(
                name: "IX_UserDataScope_DataScopeID",
                table: "UserDataScope",
                column: "DataScopeID");

            migrationBuilder.CreateIndex(
                name: "IX_UserDataScope_DataScopeID1",
                table: "UserDataScope",
                column: "DataScopeID1");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermission_GrantedBy",
                table: "UserPermission",
                column: "GrantedBy");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermission_PermissionID",
                table: "UserPermission",
                column: "PermissionID");

            migrationBuilder.CreateIndex(
                name: "UQ_UserPermission_UserPerm",
                table: "UserPermission",
                columns: new[] { "SystemUserID", "PermissionID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Violation_DormitoryCityID",
                table: "Violation",
                column: "DormitoryCityID");

            migrationBuilder.CreateIndex(
                name: "IX_Violation_RecordedBy",
                table: "Violation",
                column: "RecordedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Violation_ResolvedBy",
                table: "Violation",
                column: "ResolvedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Violation_StudentID",
                table: "Violation",
                column: "StudentID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Absence");

            migrationBuilder.DropTable(
                name: "AnnouncementAttachment");

            migrationBuilder.DropTable(
                name: "ApplicationSchedule");

            migrationBuilder.DropTable(
                name: "AuditLog");

            migrationBuilder.DropTable(
                name: "BulkOperationLog");

            migrationBuilder.DropTable(
                name: "CardPrintQueue");

            migrationBuilder.DropTable(
                name: "CityConfiguration");

            migrationBuilder.DropTable(
                name: "CityStaff");

            migrationBuilder.DropTable(
                name: "CoordinationResult");

            migrationBuilder.DropTable(
                name: "CoordinationRule");

            migrationBuilder.DropTable(
                name: "Document");

            migrationBuilder.DropTable(
                name: "DormitoryBlock");

            migrationBuilder.DropTable(
                name: "EmailLog");

            migrationBuilder.DropTable(
                name: "EvictionNotice");

            migrationBuilder.DropTable(
                name: "FacultyQuota");

            migrationBuilder.DropTable(
                name: "Guardian");

            migrationBuilder.DropTable(
                name: "HousingInstructionAttachment");

            migrationBuilder.DropTable(
                name: "IDCard");

            migrationBuilder.DropTable(
                name: "MaintenanceRequest");

            migrationBuilder.DropTable(
                name: "MealBlock");

            migrationBuilder.DropTable(
                name: "MealCancellation");

            migrationBuilder.DropTable(
                name: "MealConsumption");

            migrationBuilder.DropTable(
                name: "MealSchedule");

            migrationBuilder.DropTable(
                name: "PaymentGatewayLog");

            migrationBuilder.DropTable(
                name: "SocialCase");

            migrationBuilder.DropTable(
                name: "SpecialCase");

            migrationBuilder.DropTable(
                name: "StudentDownloadLog");

            migrationBuilder.DropTable(
                name: "StudentInventory");

            migrationBuilder.DropTable(
                name: "StudentLogin");

            migrationBuilder.DropTable(
                name: "StudentValidationLog");

            migrationBuilder.DropTable(
                name: "UniversityAPISync");

            migrationBuilder.DropTable(
                name: "UniversityPhoto");

            migrationBuilder.DropTable(
                name: "UserDataScope");

            migrationBuilder.DropTable(
                name: "UserPermission");

            migrationBuilder.DropTable(
                name: "Violation");

            migrationBuilder.DropTable(
                name: "Announcement");

            migrationBuilder.DropTable(
                name: "HousingInstruction");

            migrationBuilder.DropTable(
                name: "Meal");

            migrationBuilder.DropTable(
                name: "Payment");

            migrationBuilder.DropTable(
                name: "InventoryItem");

            migrationBuilder.DropTable(
                name: "DataScope");

            migrationBuilder.DropTable(
                name: "Permission");

            migrationBuilder.DropTable(
                name: "Allocation");

            migrationBuilder.DropTable(
                name: "PermissionGroup");

            migrationBuilder.DropTable(
                name: "Application");

            migrationBuilder.DropTable(
                name: "CityRoom");

            migrationBuilder.DropTable(
                name: "Student");

            migrationBuilder.DropTable(
                name: "CityBuilding");

            migrationBuilder.DropTable(
                name: "DormitoryCity");

            migrationBuilder.DropTable(
                name: "SystemUser");

            migrationBuilder.DropTable(
                name: "University");
        }
    }
}
