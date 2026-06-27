using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniStay.Migrations
{
    /// <inheritdoc />
    public partial class AddNewModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationConfiguration",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConfigKey = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    ConfigValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__AppConfig__3214EC27", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "FeeType",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FeeCategory = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    CreatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__FeeType__3214EC27", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "HousingType",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__HousingType__3214EC27", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "MealType",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__MealType__3214EC27", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Role",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    CreatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Role__3214EC27", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "FeeConfiguration",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FeeTypeID = table.Column<int>(type: "int", nullable: false),
                    DormitoryCityID = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    AcademicYear = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__FeeConfig__3214EC27", x => x.ID);
                    table.ForeignKey(
                        name: "FK_FeeConfig_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_FeeConfig_DormitoryCity",
                        column: x => x.DormitoryCityID,
                        principalTable: "DormitoryCity",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_FeeConfig_FeeType",
                        column: x => x.FeeTypeID,
                        principalTable: "FeeType",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_FeeConfig_LastUpdatedBy",
                        column: x => x.LastUpdatedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "RolePermission",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleID = table.Column<int>(type: "int", nullable: false),
                    PermissionID = table.Column<int>(type: "int", nullable: false),
                    CanView = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CanCreate = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CanEdit = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CanDelete = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__RolePermission__3214EC27", x => x.ID);
                    table.ForeignKey(
                        name: "FK_RolePermission_Permission",
                        column: x => x.PermissionID,
                        principalTable: "Permission",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_RolePermission_Role",
                        column: x => x.RoleID,
                        principalTable: "Role",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "UserRole",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SystemUserID = table.Column<int>(type: "int", nullable: false),
                    RoleID = table.Column<int>(type: "int", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getdate())"),
                    AssignedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__UserRole__3214EC27", x => x.ID);
                    table.ForeignKey(
                        name: "FK_UserRole_AssignedBy",
                        column: x => x.AssignedBy,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_UserRole_Role",
                        column: x => x.RoleID,
                        principalTable: "Role",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_UserRole_SystemUser",
                        column: x => x.SystemUserID,
                        principalTable: "SystemUser",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateIndex(
                name: "UQ_AppConfig_Key",
                table: "ApplicationConfiguration",
                column: "ConfigKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeeConfiguration_CreatedBy",
                table: "FeeConfiguration",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FeeConfiguration_DormitoryCityID",
                table: "FeeConfiguration",
                column: "DormitoryCityID");

            migrationBuilder.CreateIndex(
                name: "IX_FeeConfiguration_FeeTypeID",
                table: "FeeConfiguration",
                column: "FeeTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_FeeConfiguration_LastUpdatedBy",
                table: "FeeConfiguration",
                column: "LastUpdatedBy");

            migrationBuilder.CreateIndex(
                name: "UQ_Role_Name",
                table: "Role",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermission_PermissionID",
                table: "RolePermission",
                column: "PermissionID");

            migrationBuilder.CreateIndex(
                name: "UQ_RolePermission",
                table: "RolePermission",
                columns: new[] { "RoleID", "PermissionID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRole_AssignedBy",
                table: "UserRole",
                column: "AssignedBy");

            migrationBuilder.CreateIndex(
                name: "IX_UserRole_RoleID",
                table: "UserRole",
                column: "RoleID");

            migrationBuilder.CreateIndex(
                name: "UQ_UserRole",
                table: "UserRole",
                columns: new[] { "SystemUserID", "RoleID" },
                unique: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationConfiguration");

            migrationBuilder.DropTable(
                name: "FeeConfiguration");

            migrationBuilder.DropTable(
                name: "HousingType");

            migrationBuilder.DropTable(
                name: "MealType");

            migrationBuilder.DropTable(
                name: "RolePermission");

            migrationBuilder.DropTable(
                name: "UserRole");

            migrationBuilder.DropTable(
                name: "FeeType");

            migrationBuilder.DropTable(
                name: "Role");
        }
    }
}
