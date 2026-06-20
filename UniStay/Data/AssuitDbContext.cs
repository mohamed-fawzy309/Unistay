using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using UniStay.Models;

namespace UniStay.Data;

public partial class AssuitDbContext : DbContext
{
    public AssuitDbContext(DbContextOptions<AssuitDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Absence> Absences { get; set; }

    public virtual DbSet<Allocation> Allocations { get; set; }

    public virtual DbSet<Announcement> Announcements { get; set; }

    public virtual DbSet<AnnouncementAttachment> AnnouncementAttachments { get; set; }

    public virtual DbSet<Application> Applications { get; set; }

    public virtual DbSet<ApplicationSchedule> ApplicationSchedules { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<BulkOperationLog> BulkOperationLogs { get; set; }

    public virtual DbSet<CardPrintQueue> CardPrintQueues { get; set; }

    public virtual DbSet<CityBuilding> CityBuildings { get; set; }

    public virtual DbSet<CityConfiguration> CityConfigurations { get; set; }

    public virtual DbSet<CityRoom> CityRooms { get; set; }

    public virtual DbSet<CityStaff> CityStaffs { get; set; }

    public virtual DbSet<CoordinationResult> CoordinationResults { get; set; }

    public virtual DbSet<CoordinationRule> CoordinationRules { get; set; }

    public virtual DbSet<DataScope> DataScopes { get; set; }

    public virtual DbSet<Document> Documents { get; set; }

    public virtual DbSet<DormitoryBlock> DormitoryBlocks { get; set; }

    public virtual DbSet<DormitoryCity> DormitoryCities { get; set; }

    public virtual DbSet<EmailLog> EmailLogs { get; set; }

    public virtual DbSet<EvictionNotice> EvictionNotices { get; set; }

    public virtual DbSet<FacultyQuotum> FacultyQuota { get; set; }

    public virtual DbSet<Guardian> Guardians { get; set; }

    public virtual DbSet<HousingInstruction> HousingInstructions { get; set; }

    public virtual DbSet<HousingInstructionAttachment> HousingInstructionAttachments { get; set; }

    public virtual DbSet<IDCard> IDCards { get; set; }

    public virtual DbSet<InventoryItem> InventoryItems { get; set; }

    public virtual DbSet<MaintenanceRequest> MaintenanceRequests { get; set; }

    public virtual DbSet<Meal> Meals { get; set; }

    public virtual DbSet<MealBlock> MealBlocks { get; set; }

    public virtual DbSet<MealCancellation> MealCancellations { get; set; }

    public virtual DbSet<MealConsumption> MealConsumptions { get; set; }

    public virtual DbSet<MealSchedule> MealSchedules { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<PaymentGatewayLog> PaymentGatewayLogs { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<PermissionGroup> PermissionGroups { get; set; }

    public virtual DbSet<SocialCase> SocialCases { get; set; }

    public virtual DbSet<SpecialCase> SpecialCases { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<StudentDownloadLog> StudentDownloadLogs { get; set; }

    public virtual DbSet<StudentInventory> StudentInventories { get; set; }

    public virtual DbSet<StudentLogin> StudentLogins { get; set; }

    public virtual DbSet<StudentValidationLog> StudentValidationLogs { get; set; }

    public virtual DbSet<SystemUser> SystemUsers { get; set; }

    public virtual DbSet<University> Universities { get; set; }

    public virtual DbSet<UniversityAPISync> UniversityAPISyncs { get; set; }

    public virtual DbSet<UniversityPhoto> UniversityPhotos { get; set; }

    public virtual DbSet<UserPermission> UserPermissions { get; set; }

    public virtual DbSet<Violation> Violations { get; set; }

    public virtual DbSet<Village> Villages { get; set; }

    public virtual DbSet<HousingType> HousingTypes { get; set; }

    public virtual DbSet<MealType> MealTypes { get; set; }

    public virtual DbSet<FeeType> FeeTypes { get; set; }

    public virtual DbSet<FeeConfiguration> FeeConfigurations { get; set; }

    public virtual DbSet<Country> Countries { get; set; }

    public virtual DbSet<StudentCategory> StudentCategories { get; set; }

    public virtual DbSet<ApplicationConfiguration> ApplicationConfigurations { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<RolePermission> RolePermissions { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    
   
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Arabic_CI_AS");

        modelBuilder.Entity<Absence>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__Absence__3214EC278A1614AC");

            entity.ToTable("Absence");

            entity.HasIndex(e => e.Status, "IX_Absence_Status");

            entity.HasIndex(e => e.StudentID, "IX_Absence_StudentID");

            entity.HasIndex(e => e.ToDate, "IX_Absence_ToDate");

            entity.Property(e => e.AbsenceType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.GuardianName).HasMaxLength(200);
            entity.Property(e => e.GuardianPhone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.GuardianRelation).HasMaxLength(50);
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.RequestedBy)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Student");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending");

            entity.HasOne(d => d.DormitoryCity).WithMany(p => p.Absences)
                .HasForeignKey(d => d.DormitoryCityID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Absence_DormitoryCity");

            entity.HasOne(d => d.ReviewedByNavigation).WithMany(p => p.Absences)
                .HasForeignKey(d => d.ReviewedBy)
                .HasConstraintName("FK_Absence_ReviewedBy");

            entity.HasOne(d => d.Student).WithMany(p => p.Absences)
                .HasForeignKey(d => d.StudentID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Absence_Student");
        });

        modelBuilder.Entity<Allocation>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__Allocati__3214EC27971EDB22");

            entity.ToTable("Allocation");

            entity.HasIndex(e => e.AcademicYear, "IX_Allocation_AcademicYear");

            entity.HasIndex(e => e.CityRoomID, "IX_Allocation_CityRoomID");

            entity.HasIndex(e => e.Status, "IX_Allocation_Status");

            entity.HasIndex(e => e.StudentID, "IX_Allocation_StudentID");

            entity.HasIndex(e => e.ApplicationID, "UQ_Allocation_Application").IsUnique();

            entity.HasIndex(e => new { e.CityRoomID, e.BedNumber, e.AcademicYear }, "UQ_Allocation_Bed").IsUnique();

            entity.Property(e => e.AcademicYear).HasMaxLength(10);
            entity.Property(e => e.AllocatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Active");

            entity.HasOne(d => d.AllocatedByNavigation).WithMany(p => p.Allocations)
                .HasForeignKey(d => d.AllocatedBy)
                .HasConstraintName("FK_Allocation_AllocatedBy");

            entity.HasOne(d => d.Application).WithOne(p => p.Allocation)
                .HasForeignKey<Allocation>(d => d.ApplicationID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Allocation_Application");

            entity.HasOne(d => d.CityRoom).WithMany(p => p.Allocations)
                .HasForeignKey(d => d.CityRoomID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Allocation_CityRoom");

            entity.HasOne(d => d.Student).WithMany(p => p.Allocations)
                .HasForeignKey(d => d.StudentID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Allocation_Student");
        });

        modelBuilder.Entity<Announcement>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__Announce__3214EC270FA560D0");

            entity.ToTable("Announcement");

            entity.Property(e => e.AnnouncementType)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsPublished).HasDefaultValue(false);
            entity.Property(e => e.TargetAudience)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("All");
            entity.Property(e => e.Title).HasMaxLength(300);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Announcements)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_Announcement_CreatedBy");

            entity.HasOne(d => d.DormitoryCity).WithMany(p => p.Announcements)
                .HasForeignKey(d => d.DormitoryCityID)
                .HasConstraintName("FK_Announcement_DormitoryCity");
        });

        modelBuilder.Entity<AnnouncementAttachment>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__Announce__3214EC2747AE27AE");

            entity.ToTable("AnnouncementAttachment");

            entity.Property(e => e.FileName).HasMaxLength(200);
            entity.Property(e => e.FilePath).HasMaxLength(500);

            entity.HasOne(d => d.Announcement).WithMany(p => p.AnnouncementAttachments)
                .HasForeignKey(d => d.AnnouncementID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AnnouncementAttach_Announcement");
        });

        modelBuilder.Entity<Application>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__Applicat__3214EC27488E77E6");

            entity.ToTable("Application");

            entity.HasIndex(e => e.AcademicYear, "IX_Application_AcademicYear");

            entity.HasIndex(e => e.DormitoryCityID, "IX_Application_DormitoryCityID");

            entity.HasIndex(e => e.Status, "IX_Application_Status");

            entity.HasIndex(e => e.StudentID, "IX_Application_StudentID");

            entity.HasIndex(e => new { e.StudentID, e.AcademicYear }, "UQ_Application_StudentYear").IsUnique();

            entity.Property(e => e.AcademicYear).HasMaxLength(10);
            entity.Property(e => e.AdminNotes).HasMaxLength(1000);
            entity.Property(e => e.CoordinationScore).HasColumnType("decimal(8, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.HasSpecialNeeds).HasDefaultValue(false);
            entity.Property(e => e.HousingType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.MealSubscription).HasDefaultValue(false);
            entity.Property(e => e.RejectionReason).HasMaxLength(1000);
            entity.Property(e => e.ServerVerificationStatus)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("NotChecked");
            entity.Property(e => e.SpecialNeedsDescription).HasMaxLength(500);
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("Pending");
            entity.Property(e => e.StudentType)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.DormitoryCity).WithMany(p => p.Applications)
                .HasForeignKey(d => d.DormitoryCityID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Application_DormitoryCity");

            entity.HasOne(d => d.LastUpdatedByNavigation).WithMany(p => p.ApplicationLastUpdatedByNavigations)
                .HasForeignKey(d => d.LastUpdatedBy)
                .HasConstraintName("FK_Application_LastUpdatedBy");

            entity.HasOne(d => d.ReviewedByNavigation).WithMany(p => p.ApplicationReviewedByNavigations)
                .HasForeignKey(d => d.ReviewedBy)
                .HasConstraintName("FK_Application_ReviewedBy");

            entity.HasOne(d => d.ServerVerificationByNavigation).WithMany(p => p.ApplicationServerVerificationByNavigations)
                .HasForeignKey(d => d.ServerVerificationBy)
                .HasConstraintName("FK_Application_ServerVerBy");

            entity.HasOne(d => d.Student).WithMany(p => p.Applications)
                .HasForeignKey(d => d.StudentID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Application_Student");
        });

        

        modelBuilder.Entity<ApplicationSchedule>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__Applicat__3214EC27452C7B0C");

            entity.ToTable("ApplicationSchedule");

            entity.Property(e => e.AcademicYear).HasMaxLength(10);

            entity.Property(e => e.IsOpen).HasDefaultValue(true);

            entity.HasOne(d => d.DormitoryCity).WithMany(p => p.ApplicationSchedules)
                .HasForeignKey(d => d.DormitoryCityID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AppSchedule_DormitoryCity");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__AuditLog__3214EC2744A6BC61");

            entity.ToTable("AuditLog");

            entity.HasIndex(e => e.CreatedAt, "IX_AuditLog_CreatedAt");

            entity.HasIndex(e => e.TableName, "IX_AuditLog_TableName");

            entity.HasIndex(e => e.UserID, "IX_AuditLog_UserID");

            entity.Property(e => e.Action).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IPAddress)
                .HasMaxLength(45)
                .IsUnicode(false);
            entity.Property(e => e.TableName).HasMaxLength(100);
            entity.Property(e => e.UserType)
                .HasMaxLength(10)
                .IsUnicode(false);
        });

        modelBuilder.Entity<BulkOperationLog>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__BulkOper__3214EC2793E8F8BD");

            entity.ToTable("BulkOperationLog");

            entity.Property(e => e.AffectedCount).HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.FailedCount).HasDefaultValue(0);
            entity.Property(e => e.OperationType).HasMaxLength(200);
            entity.Property(e => e.SuccessCount).HasDefaultValue(0);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.BulkOperationLogs)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_BulkOperationLog_CreatedBy");
        });

        modelBuilder.Entity<CardPrintQueue>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__CardPrin__3214EC274810518E");

            entity.ToTable("CardPrintQueue");

            entity.Property(e => e.QueuedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending");

            entity.HasOne(d => d.DormitoryCity).WithMany(p => p.CardPrintQueues)
                .HasForeignKey(d => d.DormitoryCityID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CardPrintQueue_DormitoryCity");

            entity.HasOne(d => d.PrintedByNavigation).WithMany(p => p.CardPrintQueues)
                .HasForeignKey(d => d.PrintedBy)
                .HasConstraintName("FK_CardPrintQueue_PrintedBy");

            entity.HasOne(d => d.Student).WithMany(p => p.CardPrintQueues)
                .HasForeignKey(d => d.StudentID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CardPrintQueue_Student");
        });

        modelBuilder.Entity<CityBuilding>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__CityBuil__3214EC27E20E5827");

            entity.ToTable("CityBuilding");

            entity.Property(e => e.BuildingName).HasMaxLength(100);
            entity.Property(e => e.BuildingType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.FloorCount).HasDefaultValue((byte)1);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.CityBuildingCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_CityBuilding_CreatedBy");

            entity.HasOne(d => d.DormitoryCity).WithMany(p => p.CityBuildings)
                .HasForeignKey(d => d.DormitoryCityID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CityBuilding_DormitoryCity");

            entity.HasOne(d => d.LastUpdatedByNavigation).WithMany(p => p.CityBuildingLastUpdatedByNavigations)
                .HasForeignKey(d => d.LastUpdatedBy)
                .HasConstraintName("FK_CityBuilding_LastUpdatedBy");
        });

        modelBuilder.Entity<CityConfiguration>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__CityConf__3214EC276A904117");

            entity.ToTable("CityConfiguration");

            entity.Property(e => e.AllowStudentBedSelection).HasDefaultValue(false);
            entity.Property(e => e.AutoCoordinationEnabled).HasDefaultValue(false);
            entity.Property(e => e.ChristianMealFee)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(10, 2)");
            entity.Property(e => e.ForeignStudentFee)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(10, 2)");
            entity.Property(e => e.MaxAge).HasDefaultValue((byte)28);
            entity.Property(e => e.MaxBedsPerRoom).HasDefaultValue((byte)4);
            entity.Property(e => e.MealFee)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(10, 2)");
            entity.Property(e => e.MinDistanceKm)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(8, 2)");
            entity.Property(e => e.MinGradePercentage)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(5, 2)");
            entity.Property(e => e.PremiumFee)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(10, 2)");
            entity.Property(e => e.RamadanMealFee)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(10, 2)");
            entity.Property(e => e.SecurityDeposit)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(10, 2)");
            entity.Property(e => e.StandardFee)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(10, 2)");
            entity.Property(e => e.VIPFee)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.DormitoryCity).WithMany(p => p.CityConfigurations)
                .HasForeignKey(d => d.DormitoryCityID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CityConfig_DormitoryCity");

            entity.HasOne(d => d.LastUpdatedByNavigation).WithMany(p => p.CityConfigurations)
                .HasForeignKey(d => d.LastUpdatedBy)
                .HasConstraintName("FK_CityConfig_LastUpdatedBy");
        });

        modelBuilder.Entity<CityRoom>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__CityRoom__3214EC27A9B9513C");

            entity.ToTable("CityRoom");

            entity.Property(e => e.BedsCount).HasDefaultValue((byte)4);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.HasAC).HasDefaultValue(false);
            entity.Property(e => e.HasBalcony).HasDefaultValue(false);
            entity.Property(e => e.HasFridge).HasDefaultValue(false);
            entity.Property(e => e.HasPrivateBathroom).HasDefaultValue(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.RoomNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.RoomType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Standard");

            entity.HasOne(d => d.CityBuilding).WithMany(p => p.CityRooms)
                .HasForeignKey(d => d.CityBuildingID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CityRoom_CityBuilding");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.CityRoomCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_CityRoom_CreatedBy");

            entity.HasOne(d => d.LastUpdatedByNavigation).WithMany(p => p.CityRoomLastUpdatedByNavigations)
                .HasForeignKey(d => d.LastUpdatedBy)
                .HasConstraintName("FK_CityRoom_LastUpdatedBy");
        });

        modelBuilder.Entity<CityStaff>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__CityStaf__3214EC275F511772");

            entity.ToTable("CityStaff");

            entity.HasIndex(e => new { e.SystemUserID, e.DormitoryCityID }, "UQ_CityStaff_UserCity").IsUnique();

            entity.Property(e => e.AssignedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsPrimary).HasDefaultValue(true);
            entity.Property(e => e.RoleInCity)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.AssignedByNavigation).WithMany(p => p.CityStaffAssignedByNavigations)
                .HasForeignKey(d => d.AssignedBy)
                .HasConstraintName("FK_CityStaff_AssignedBy");

            entity.HasOne(d => d.DormitoryCity).WithMany(p => p.CityStaffs)
                .HasForeignKey(d => d.DormitoryCityID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CityStaff_DormitoryCity");

            entity.HasOne(d => d.SystemUser).WithMany(p => p.CityStaffSystemUsers)
                .HasForeignKey(d => d.SystemUserID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CityStaff_SystemUser");
        });

        modelBuilder.Entity<CoordinationResult>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__Coordina__3214EC2716F99F24");

            entity.ToTable("CoordinationResult");

            entity.HasIndex(e => e.ApplicationID, "IX_CoordinationResult_ApplicationID");

            entity.Property(e => e.AcademicYear).HasMaxLength(10);
            entity.Property(e => e.AgeScore)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(5, 2)");
            entity.Property(e => e.DistanceScore)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(5, 2)");
            entity.Property(e => e.GradeScore)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(5, 2)");
            entity.Property(e => e.RejectionReason).HasMaxLength(500);
            entity.Property(e => e.SpecialBonus)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending");
            entity.Property(e => e.TotalScore)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(8, 2)");

            entity.HasOne(d => d.Application).WithMany(p => p.CoordinationResults)
                .HasForeignKey(d => d.ApplicationID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CoordResult_Application");

            entity.HasOne(d => d.DormitoryCity).WithMany(p => p.CoordinationResults)
                .HasForeignKey(d => d.DormitoryCityID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CoordResult_DormitoryCity");

            entity.HasOne(d => d.ProcessedByNavigation).WithMany(p => p.CoordinationResults)
                .HasForeignKey(d => d.ProcessedBy)
                .HasConstraintName("FK_CoordResult_ProcessedBy");

            entity.HasOne(d => d.Student).WithMany(p => p.CoordinationResults)
                .HasForeignKey(d => d.StudentID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CoordResult_Student");
        });

        modelBuilder.Entity<CoordinationRule>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__Coordina__3214EC27AB6DE2A9");

            entity.ToTable("CoordinationRule");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.RuleName).HasMaxLength(200);
            entity.Property(e => e.RuleType)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Weight).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.CoordinationRules)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_CoordRule_CreatedBy");

            entity.HasOne(d => d.DormitoryCity).WithMany(p => p.CoordinationRules)
                .HasForeignKey(d => d.DormitoryCityID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CoordRule_DormitoryCity");
        });

        modelBuilder.Entity<DataScope>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__DataScop__3214EC276F43A621");

            entity.ToTable("DataScope");

            entity.Property(e => e.ScopeType)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.ScopeValue).HasMaxLength(200);
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__Document__3214EC273EA5E377");

            entity.ToTable("Document");

            entity.HasIndex(e => e.StudentID, "IX_Document_StudentID");

            entity.HasIndex(e => e.ApplicationID, "IX_Document_ApplicationID");

            entity.Property(e => e.DocumentType)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.FileName).HasMaxLength(200);
            entity.Property(e => e.FilePath).HasMaxLength(500);
            entity.Property(e => e.IsVerified).HasDefaultValue(false);
            entity.Property(e => e.UploadedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Application).WithMany(p => p.Documents)
                .HasForeignKey(d => d.ApplicationID)
                .HasConstraintName("FK_Document_Application");

            entity.HasOne(d => d.Student).WithMany(p => p.Documents)
                .HasForeignKey(d => d.StudentID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Document_Student");

            entity.HasOne(d => d.VerifiedByNavigation).WithMany(p => p.Documents)
                .HasForeignKey(d => d.VerifiedBy)
                .HasConstraintName("FK_Document_VerifiedBy");
        });

        modelBuilder.Entity<DormitoryBlock>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__Dormitor__3214EC27860406F6");

            entity.ToTable("DormitoryBlock");

            entity.Property(e => e.AcademicYear).HasMaxLength(10);
            entity.Property(e => e.Faculty).HasMaxLength(100);

            entity.HasOne(d => d.CityBuilding).WithMany(p => p.DormitoryBlocks)
                .HasForeignKey(d => d.CityBuildingID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DormitoryBlock_CityBuilding");
        });

        modelBuilder.Entity<DormitoryCity>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__Dormitor__3214EC27B65B16A9");

            entity.ToTable("DormitoryCity");

            entity.Property(e => e.CityType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Location).HasMaxLength(300);
            entity.Property(e => e.Name).HasMaxLength(200);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.DormitoryCityCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_DormitoryCity_CreatedBy");

            entity.HasOne(d => d.LastUpdatedByNavigation).WithMany(p => p.DormitoryCityLastUpdatedByNavigations)
                .HasForeignKey(d => d.LastUpdatedBy)
                .HasConstraintName("FK_DormitoryCity_LastUpdatedBy");

            entity.HasOne(d => d.University).WithMany(p => p.DormitoryCities)
                .HasForeignKey(d => d.UniversityID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DormitoryCity_University");
        });

        modelBuilder.Entity<EmailLog>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__EmailLog__3214EC27E8524F0C");

            entity.ToTable("EmailLog");

            entity.HasIndex(e => e.Status, "IX_EmailLog_Status");

            entity.HasIndex(e => e.StudentID, "IX_EmailLog_StudentID");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.EmailType)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ErrorMessage).HasMaxLength(500);
            entity.Property(e => e.RecipientEmail)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending");
            entity.Property(e => e.Subject).HasMaxLength(300);

            entity.HasOne(d => d.Student).WithMany(p => p.EmailLogs)
                .HasForeignKey(d => d.StudentID)
                .HasConstraintName("FK_EmailLog_Student");
        });

        modelBuilder.Entity<EvictionNotice>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__Eviction__3214EC27F03D648E");

            entity.ToTable("EvictionNotice");

            entity.Property(e => e.EvictionType)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.IssuedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Reason).HasMaxLength(1000);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending");

            entity.HasOne(d => d.Allocation).WithMany(p => p.EvictionNotices)
                .HasForeignKey(d => d.AllocationID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Eviction_Allocation");

            entity.HasOne(d => d.IssuedByNavigation).WithMany(p => p.EvictionNotices)
                .HasForeignKey(d => d.IssuedBy)
                .HasConstraintName("FK_Eviction_IssuedBy");

            entity.HasOne(d => d.Student).WithMany(p => p.EvictionNotices)
                .HasForeignKey(d => d.StudentID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Eviction_Student");
        });

        modelBuilder.Entity<FacultyQuotum>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__FacultyQ__3214EC2724A3F6E5");

            entity.Property(e => e.AcademicYear).HasMaxLength(10);
            entity.Property(e => e.Faculty).HasMaxLength(100);

            entity.HasOne(d => d.DormitoryCity).WithMany(p => p.FacultyQuota)
                .HasForeignKey(d => d.DormitoryCityID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FacultyQuota_DormitoryCity");
        });

        modelBuilder.Entity<Guardian>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__Guardian__3214EC27D8FB476A");

            entity.ToTable("Guardian");

            entity.HasIndex(e => e.StudentID, "IX_Guardian_StudentID");

            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.FullName).HasMaxLength(200);
            entity.Property(e => e.GuardianType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.IsDeceased).HasDefaultValue(false);
            entity.Property(e => e.Job).HasMaxLength(100);
            entity.Property(e => e.NationalID)
                .HasMaxLength(14)
                .IsUnicode(false);
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Phone2)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Student).WithMany(p => p.Guardians)
                .HasForeignKey(d => d.StudentID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Guardian_Student");
        });

        modelBuilder.Entity<HousingInstruction>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__HousingI__3214EC274B1664FE");

            entity.ToTable("HousingInstruction");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.InstructionType)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.SortOrder).HasDefaultValue((byte)0);
            entity.Property(e => e.Title).HasMaxLength(300);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.HousingInstructions)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_HousingInstruction_CreatedBy");

            entity.HasOne(d => d.DormitoryCity).WithMany(p => p.HousingInstructions)
                .HasForeignKey(d => d.DormitoryCityID)
                .HasConstraintName("FK_HousingInstruction_DormitoryCity");
        });

        modelBuilder.Entity<HousingInstructionAttachment>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__HousingI__3214EC2723971597");

            entity.ToTable("HousingInstructionAttachment");

            entity.Property(e => e.FileName).HasMaxLength(200);
            entity.Property(e => e.FilePath).HasMaxLength(500);
            entity.Property(e => e.FileType)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.SortOrder).HasDefaultValue((byte)0);

            entity.HasOne(d => d.HousingInstruction).WithMany(p => p.HousingInstructionAttachments)
                .HasForeignKey(d => d.HousingInstructionID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HousingInstructionAttach_Instruction");
        });

        modelBuilder.Entity<IDCard>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__IDCard__3214EC270F63707B");

            entity.ToTable("IDCard");

            entity.HasIndex(e => e.StudentID, "IX_IDCard_StudentID");

            entity.HasIndex(e => e.CardNumber, "UQ_IDCard_CardNumber").IsUnique();

            entity.Property(e => e.Barcode)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.CardNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsPrinted).HasDefaultValue(false);
            entity.Property(e => e.ReprintCount).HasDefaultValue((byte)0);

            entity.HasOne(d => d.PrintedByNavigation).WithMany(p => p.IDCards)
                .HasForeignKey(d => d.PrintedBy)
                .HasConstraintName("FK_IDCard_PrintedBy");

            entity.HasOne(d => d.Student).WithMany(p => p.IDCards)
                .HasForeignKey(d => d.StudentID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_IDCard_Student");
        });

        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__Inventor__3214EC27C2B8EAF1");

            entity.ToTable("InventoryItem");

            entity.HasIndex(e => e.ItemCode, "UQ_InventoryItem_Code").IsUnique();

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ItemCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ItemName).HasMaxLength(200);
            entity.Property(e => e.ItemValue).HasColumnType("decimal(8, 2)");
        });

        modelBuilder.Entity<MaintenanceRequest>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__Maintena__3214EC275CDF3008");

            entity.ToTable("MaintenanceRequest");

            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Priority)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Normal");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending");

            entity.HasOne(d => d.AssignedToNavigation).WithMany(p => p.MaintenanceRequests)
                .HasForeignKey(d => d.AssignedTo)
                .HasConstraintName("FK_Maintenance_AssignedTo");

            entity.HasOne(d => d.CityRoom).WithMany(p => p.MaintenanceRequests)
                .HasForeignKey(d => d.CityRoomID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Maintenance_CityRoom");

            entity.HasOne(d => d.DormitoryCity).WithMany(p => p.MaintenanceRequests)
                .HasForeignKey(d => d.DormitoryCityID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Maintenance_DormitoryCity");

            entity.HasOne(d => d.Student).WithMany(p => p.MaintenanceRequests)
                .HasForeignKey(d => d.StudentID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Maintenance_Student");
        });

        modelBuilder.Entity<Meal>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__Meal__3214EC27623917A5");

            entity.ToTable("Meal");

            entity.HasIndex(e => e.DormitoryCityID, "IX_Meal_DormitoryCityID");

            entity.HasIndex(e => e.MealDate, "IX_Meal_MealDate");

            entity.HasIndex(e => e.StudentID, "IX_Meal_StudentID");

            entity.Property(e => e.CancelReason).HasMaxLength(200);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsBooked).HasDefaultValue(true);
            entity.Property(e => e.IsConsumed).HasDefaultValue(false);
            entity.Property(e => e.MealType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Price).HasColumnType("decimal(8, 2)");

            entity.HasOne(d => d.DormitoryCity).WithMany(p => p.Meals)
                .HasForeignKey(d => d.DormitoryCityID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Meal_DormitoryCity");

            entity.HasOne(d => d.Student).WithMany(p => p.Meals)
                .HasForeignKey(d => d.StudentID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Meal_Student");
        });

        modelBuilder.Entity<MealBlock>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__MealBloc__3214EC27A368D3D5");

            entity.ToTable("MealBlock");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MealType).HasMaxLength(20).IsUnicode(false);
            entity.Property(e => e.Reason).HasMaxLength(500);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.MealBlocks)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_MealBlock_CreatedBy");

            entity.HasOne(d => d.DormitoryCity).WithMany(p => p.MealBlocks)
                .HasForeignKey(d => d.DormitoryCityID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MealBlock_DormitoryCity");

            entity.HasOne(d => d.Student).WithMany(p => p.MealBlocks)
                .HasForeignKey(d => d.StudentID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MealBlock_Student");
        });

        modelBuilder.Entity<MealCancellation>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__MealCanc__3214EC271DFA6834");

            entity.ToTable("MealCancellation");

            entity.Property(e => e.CancellationType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.MealCancellations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_MealCancellation_CreatedBy");

            entity.HasOne(d => d.DormitoryCity).WithMany(p => p.MealCancellations)
                .HasForeignKey(d => d.DormitoryCityID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MealCancellation_DormitoryCity");

            entity.HasOne(d => d.Student).WithMany(p => p.MealCancellations)
                .HasForeignKey(d => d.StudentID)
                .HasConstraintName("FK_MealCancellation_Student");
        });

        modelBuilder.Entity<MealConsumption>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__MealCons__3214EC2764233F48");

            entity.ToTable("MealConsumption");

            entity.Property(e => e.ConsumedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ScanMethod)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.DormitoryCity).WithMany(p => p.MealConsumptions)
                .HasForeignKey(d => d.DormitoryCityID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MealConsumption_DormitoryCity");

            entity.HasOne(d => d.Meal).WithMany(p => p.MealConsumptions)
                .HasForeignKey(d => d.MealID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MealConsumption_Meal");

            entity.HasOne(d => d.RecordedByNavigation).WithMany(p => p.MealConsumptions)
                .HasForeignKey(d => d.RecordedBy)
                .HasConstraintName("FK_MealConsumption_RecordedBy");

            entity.HasOne(d => d.Student).WithMany(p => p.MealConsumptions)
                .HasForeignKey(d => d.StudentID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MealConsumption_Student");
        });

        modelBuilder.Entity<MealSchedule>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__MealSche__3214EC2775A604ED");

            entity.ToTable("MealSchedule");

            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MealType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.SpecialPrice).HasColumnType("decimal(8, 2)");

            entity.HasOne(d => d.DormitoryCity).WithMany(p => p.MealSchedules)
                .HasForeignKey(d => d.DormitoryCityID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MealSchedule_DormitoryCity");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__Payment__3214EC27C37C5727");

            entity.ToTable("Payment");

            entity.HasIndex(e => e.AcademicYear, "IX_Payment_AcademicYear");

            entity.HasIndex(e => e.Status, "IX_Payment_Status");

            entity.HasIndex(e => e.StudentID, "IX_Payment_StudentID");

            entity.Property(e => e.AcademicYear).HasMaxLength(10);
            entity.Property(e => e.Amount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.PaidAmount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.PaymentType)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.ReceiptNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RecordedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending");

            entity.HasOne(d => d.Allocation).WithMany(p => p.Payments)
                .HasForeignKey(d => d.AllocationID)
                .HasConstraintName("FK_Payment_Allocation");

            entity.HasOne(d => d.Application).WithMany(p => p.Payments)
                .HasForeignKey(d => d.ApplicationID)
                .HasConstraintName("FK_Payment_Application");

            entity.HasOne(d => d.RecordedByNavigation).WithMany(p => p.Payments)
                .HasForeignKey(d => d.RecordedBy)
                .HasConstraintName("FK_Payment_RecordedBy");

            entity.HasOne(d => d.Student).WithMany(p => p.Payments)
                .HasForeignKey(d => d.StudentID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payment_Student");
        });

        modelBuilder.Entity<PaymentGatewayLog>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__PaymentG__3214EC2761DD9E30");

            entity.ToTable("PaymentGatewayLog");

            entity.Property(e => e.Amount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.GatewayType)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.TransactionID)
                .HasMaxLength(200)
                .IsUnicode(false);

            entity.HasOne(d => d.Payment).WithMany(p => p.PaymentGatewayLogs)
                .HasForeignKey(d => d.PaymentID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PaymentGateway_Payment");

            entity.HasOne(d => d.Student).WithMany(p => p.PaymentGatewayLogs)
                .HasForeignKey(d => d.StudentID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PaymentGateway_Student");
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__Permissi__3214EC2761DB10DB");

            entity.ToTable("Permission");

            entity.HasIndex(e => e.PermissionKey, "UQ_Permission_Key").IsUnique();

            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.DisplayName).HasMaxLength(200);
            entity.Property(e => e.PermissionKey)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.Group).WithMany(p => p.Permissions)
                .HasForeignKey(d => d.GroupID)
                .HasConstraintName("FK_Permission_Group");
        });

        modelBuilder.Entity<PermissionGroup>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__Permissi__3214EC27E614522F");

            entity.ToTable("PermissionGroup");

            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.GroupName).HasMaxLength(200);
        });

        modelBuilder.Entity<SocialCase>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__SocialCa__3214EC2716577C8C");

            entity.ToTable("SocialCase");

            entity.HasIndex(e => e.StudentID, "IX_SocialCase_StudentID");

            entity.Property(e => e.CaseType).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Priority)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Open");

            entity.HasOne(d => d.AssignedToNavigation).WithMany(p => p.SocialCases)
                .HasForeignKey(d => d.AssignedTo)
                .HasConstraintName("FK_SocialCase_AssignedTo");

            entity.HasOne(d => d.Student).WithMany(p => p.SocialCases)
                .HasForeignKey(d => d.StudentID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SocialCase_Student");
        });

        modelBuilder.Entity<SpecialCase>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__SpecialC__3214EC2755A318C5");

            entity.ToTable("SpecialCase");

            entity.HasIndex(e => e.StudentID, "IX_SpecialCase_StudentID");

            entity.HasIndex(e => e.ApplicationID, "IX_SpecialCase_ApplicationID");

            entity.Property(e => e.CaseType)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.ReviewNotes).HasMaxLength(500);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending");

            entity.HasOne(d => d.Application).WithMany(p => p.SpecialCases)
                .HasForeignKey(d => d.ApplicationID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SpecialCase_Application");

            entity.HasOne(d => d.ReviewedByNavigation).WithMany(p => p.SpecialCases)
                .HasForeignKey(d => d.ReviewedBy)
                .HasConstraintName("FK_SpecialCase_ReviewedBy");

            entity.HasOne(d => d.Student).WithMany(p => p.SpecialCases)
                .HasForeignKey(d => d.StudentID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SpecialCase_Student");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__Student__3214EC2710B21BF6");

            entity.ToTable("Student");

            entity.HasIndex(e => e.Faculty, "IX_Student_Faculty");

            entity.HasIndex(e => e.Gender, "IX_Student_Gender");

            entity.HasIndex(e => e.IsDeleted, "IX_Student_IsDeleted");

            entity.HasIndex(e => e.NationalID, "IX_Student_NationalID");

            entity.HasIndex(e => e.NationalID, "UQ_Student_NationalID").IsUnique();

            entity.HasIndex(e => e.Email, "IX_Student_Email");

            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Markaz).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Department).HasMaxLength(100);
            entity.Property(e => e.DistanceFromUniv).HasColumnType("decimal(8, 2)");
            entity.Property(e => e.Email)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Faculty).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(200);
            entity.Property(e => e.Gender)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Governorate).HasMaxLength(100);
            entity.Property(e => e.GradePercentage).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.GradeText).HasMaxLength(50);
            entity.Property(e => e.HasDisability).HasDefaultValue(false);
            entity.Property(e => e.HasFamilyAbroad).HasDefaultValue(false);
            entity.Property(e => e.HasMedicalCondition).HasDefaultValue(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.IsEnrolled).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.IsForeign).HasDefaultValue(false);
            entity.Property(e => e.IsLowIncome).HasDefaultValue(false);
            entity.Property(e => e.IsOrphan).HasDefaultValue(false);
            entity.Property(e => e.MedicalDescription).HasMaxLength(500);
            entity.Property(e => e.NationalID)
                .HasMaxLength(14)
                .IsUnicode(false);
            entity.Property(e => e.Nationality)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("Egyptian");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Photo).HasMaxLength(500);
            entity.Property(e => e.Religion)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.StudentCode)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.LastUpdatedByNavigation).WithMany(p => p.Students)
                .HasForeignKey(d => d.LastUpdatedBy)
                .HasConstraintName("FK_Student_LastUpdatedBy");
        });

        modelBuilder.Entity<StudentDownloadLog>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__StudentD__3214EC27846588A5");

            entity.ToTable("StudentDownloadLog");

            entity.Property(e => e.DownloadedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.FormType)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.DownloadedByNavigation).WithMany(p => p.StudentDownloadLogs)
                .HasForeignKey(d => d.DownloadedBy)
                .HasConstraintName("FK_StudentDownloadLog_DownloadedBy");

            entity.HasOne(d => d.Student).WithMany(p => p.StudentDownloadLogs)
                .HasForeignKey(d => d.StudentID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StudentDownloadLog_Student");
        });

        modelBuilder.Entity<StudentInventory>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__StudentI__3214EC27C50AF97A");

            entity.ToTable("StudentInventory");

            entity.Property(e => e.AssignedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Condition)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Good");
            entity.Property(e => e.DeductionAmount)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(8, 2)");
            entity.Property(e => e.IsReturned).HasDefaultValue(false);
            entity.Property(e => e.Quantity).HasDefaultValue(1);

            entity.HasOne(d => d.Allocation).WithMany(p => p.StudentInventories)
                .HasForeignKey(d => d.AllocationID)
                .HasConstraintName("FK_StudentInventory_Allocation");

            entity.HasOne(d => d.AssignedByNavigation).WithMany(p => p.StudentInventoryAssignedByNavigations)
                .HasForeignKey(d => d.AssignedBy)
                .HasConstraintName("FK_StudentInventory_AssignedBy");

            entity.HasOne(d => d.InventoryItem).WithMany(p => p.StudentInventories)
                .HasForeignKey(d => d.InventoryItemID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StudentInventory_Item");

            entity.HasOne(d => d.ReturnedByNavigation).WithMany(p => p.StudentInventoryReturnedByNavigations)
                .HasForeignKey(d => d.ReturnedBy)
                .HasConstraintName("FK_StudentInventory_ReturnedBy");

            entity.HasOne(d => d.Student).WithMany(p => p.StudentInventories)
                .HasForeignKey(d => d.StudentID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StudentInventory_Student");
        });

        modelBuilder.Entity<StudentLogin>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__StudentL__3214EC2766FB3B4D");

            entity.ToTable("StudentLogin");

            entity.HasIndex(e => e.StudentID, "UQ_StudentLogin_Student").IsUnique();

            entity.HasIndex(e => e.Username, "UQ_StudentLogin_Username").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.FailedAttempts).HasDefaultValue((byte)0);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MustChangePassword).HasDefaultValue(false);
            entity.Property(e => e.Username)
                .HasMaxLength(14)
                .IsUnicode(false);

            entity.HasOne(d => d.Student).WithOne(p => p.StudentLogin)
                .HasForeignKey<StudentLogin>(d => d.StudentID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StudentLogin_Student");
        });

        modelBuilder.Entity<StudentValidationLog>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__StudentV__3214EC27B79FA509");

            entity.ToTable("StudentValidationLog");

            entity.HasIndex(e => e.StudentID, "IX_StudentValidationLog_StudentID");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsResolved).HasDefaultValue(false);
            entity.Property(e => e.IssueDescription).HasMaxLength(500);
            entity.Property(e => e.IssueSeverity)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ValidationType)
                .HasMaxLength(30)
                .IsUnicode(false);

            entity.HasOne(d => d.ResolvedByNavigation).WithMany(p => p.StudentValidationLogs)
                .HasForeignKey(d => d.ResolvedBy)
                .HasConstraintName("FK_StudentValidationLog_ResolvedBy");

            entity.HasOne(d => d.Student).WithMany(p => p.StudentValidationLogs)
                .HasForeignKey(d => d.StudentID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StudentValidationLog_Student");
        });

        modelBuilder.Entity<SystemUser>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__SystemUs__3214EC27F5EC8BFF");

            entity.ToTable("SystemUser");

            entity.HasIndex(e => e.NationalID, "UQ_SystemUser_NationalID").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Email)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MustChangePassword).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.NationalID)
                .HasMaxLength(14)
                .IsUnicode(false);
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.InverseCreatedByNavigation)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_SystemUser_CreatedBy");

            entity.HasOne(d => d.LastUpdatedByNavigation).WithMany(p => p.InverseLastUpdatedByNavigation)
                .HasForeignKey(d => d.LastUpdatedBy)
                .HasConstraintName("FK_SystemUser_LastUpdatedBy");

            // ✅ بعد
            entity.HasMany(d => d.DataScopes).WithMany(p => p.SystemUsers)
               .UsingEntity<UserDataScope>(
                    r => r.HasOne(ud => ud.DataScope).WithMany()
                        .HasForeignKey(ud => ud.DataScopeID)
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_UserDataScope_DataScope"),
                    l => l.HasOne(ud => ud.SystemUser).WithMany()
                        .HasForeignKey(ud => ud.SystemUserID)
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_UserDataScope_SystemUser"),
                    j =>
                    {
                        j.HasKey(ud => new { ud.SystemUserID, ud.DataScopeID })
                         .HasName("PK__UserData__FDB49D8B7483810F");
                        j.ToTable("UserDataScope");
                    });
        });

        modelBuilder.Entity<University>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__Universi__3214EC27362CEB9C");

            entity.ToTable("University");

            entity.Property(e => e.APIBaseUrl)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.APIKey)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.Email)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Logo).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.NameEn).HasMaxLength(200);
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Website)
                .HasMaxLength(200)
                .IsUnicode(false);
        });

        modelBuilder.Entity<UniversityAPISync>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__Universi__3214EC275A106C57");

            entity.ToTable("UniversityAPISync");

            entity.Property(e => e.NationalID)
                .HasMaxLength(14)
                .IsUnicode(false);
            entity.Property(e => e.StudentCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.SyncType)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.SyncedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.SyncedByNavigation).WithMany(p => p.UniversityAPISyncs)
                .HasForeignKey(d => d.SyncedBy)
                .HasConstraintName("FK_UniversityAPISync_SyncedBy");
        });

        modelBuilder.Entity<UniversityPhoto>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__Universi__3214EC27620AC0CD");

            entity.ToTable("UniversityPhoto");

            entity.Property(e => e.FilePath).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PhotoType)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("Campus");
            entity.Property(e => e.SortOrder).HasDefaultValue((byte)0);
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.DormitoryCity).WithMany(p => p.UniversityPhotos)
                .HasForeignKey(d => d.DormitoryCityID)
                .HasConstraintName("FK_UniversityPhoto_DormitoryCity");
        });

        modelBuilder.Entity<UserPermission>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__UserPerm__3214EC273AA19E9B");

            entity.ToTable("UserPermission");

            entity.HasIndex(e => new { e.SystemUserID, e.PermissionID }, "UQ_UserPermission_UserPerm").IsUnique();

            entity.Property(e => e.CanCreate).HasDefaultValue(false);
            entity.Property(e => e.CanDelete).HasDefaultValue(false);
            entity.Property(e => e.CanEdit).HasDefaultValue(false);
            entity.Property(e => e.CanView).HasDefaultValue(false);
            entity.Property(e => e.GrantedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.GrantedByNavigation).WithMany(p => p.UserPermissionGrantedByNavigations)
                .HasForeignKey(d => d.GrantedBy)
                .HasConstraintName("FK_UserPermission_GrantedBy");

            entity.HasOne(d => d.Permission).WithMany(p => p.UserPermissions)
                .HasForeignKey(d => d.PermissionID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserPermission_Permission");

            entity.HasOne(d => d.SystemUser).WithMany(p => p.UserPermissionSystemUsers)
                .HasForeignKey(d => d.SystemUserID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserPermission_SystemUser");
        });

        modelBuilder.Entity<Violation>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__Violatio__3214EC27000AA28B");

            entity.ToTable("Violation");

            entity.HasIndex(e => e.StudentID, "IX_Violation_StudentID");

            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.FineAmount)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(8, 2)");
            entity.Property(e => e.FinePaid)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(8, 2)");
            entity.Property(e => e.IsOnBlacklist).HasDefaultValue(false);
            entity.Property(e => e.RecordedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Severity)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Active");
            entity.Property(e => e.ViolationType).HasMaxLength(200);

            entity.HasOne(d => d.DormitoryCity).WithMany(p => p.Violations)
                .HasForeignKey(d => d.DormitoryCityID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Violation_DormitoryCity");

            entity.HasOne(d => d.RecordedByNavigation).WithMany(p => p.ViolationRecordedByNavigations)
                .HasForeignKey(d => d.RecordedBy)
                .HasConstraintName("FK_Violation_RecordedBy");

            entity.HasOne(d => d.ResolvedByNavigation).WithMany(p => p.ViolationResolvedByNavigations)
                .HasForeignKey(d => d.ResolvedBy)
                .HasConstraintName("FK_Violation_ResolvedBy");

            entity.HasOne(d => d.Student).WithMany(p => p.Violations)
                .HasForeignKey(d => d.StudentID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Violation_Student");
        });

        modelBuilder.Entity<Village>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__Village__3214EC27");
            entity.ToTable("Village");
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.DormitoryCity).WithMany(p => p.Villages)
                .HasForeignKey(d => d.DormitoryCityID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Village_DormitoryCity");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.VillageCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_Village_CreatedBy");

            entity.HasOne(d => d.LastUpdatedByNavigation).WithMany(p => p.VillageLastUpdatedByNavigations)
                .HasForeignKey(d => d.LastUpdatedBy)
                .HasConstraintName("FK_Village_LastUpdatedBy");
        });

        modelBuilder.Entity<HousingType>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__HousingType__3214EC27");
            entity.ToTable("HousingType");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<MealType>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__MealType__3214EC27");
            entity.ToTable("MealType");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<FeeType>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__FeeType__3214EC27");
            entity.ToTable("FeeType");
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.FeeCategory).HasMaxLength(50).IsUnicode(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<FeeConfiguration>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__FeeConfig__3214EC27");
            entity.ToTable("FeeConfiguration");
            entity.Property(e => e.Amount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.AcademicYear).HasMaxLength(10);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.FeeType).WithMany(p => p.FeeConfigurations)
                .HasForeignKey(d => d.FeeTypeID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FeeConfig_FeeType");

            entity.HasOne(d => d.DormitoryCity).WithMany(p => p.FeeConfigurations)
                .HasForeignKey(d => d.DormitoryCityID)
                .HasConstraintName("FK_FeeConfig_DormitoryCity");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.FeeConfigurationCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_FeeConfig_CreatedBy");

            entity.HasOne(d => d.LastUpdatedByNavigation).WithMany(p => p.FeeConfigurationLastUpdatedByNavigations)
                .HasForeignKey(d => d.LastUpdatedBy)
                .HasConstraintName("FK_FeeConfig_LastUpdatedBy");
        });

        modelBuilder.Entity<Country>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__Country__3214EC27");
            entity.ToTable("Country");
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.NameAr).HasMaxLength(200);
            entity.Property(e => e.Code).HasMaxLength(10).IsUnicode(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<StudentCategory>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__StudentCategory__3214EC27");
            entity.ToTable("StudentCategory");
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<ApplicationConfiguration>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__AppConfig__3214EC27");
            entity.ToTable("ApplicationConfiguration");
            entity.HasIndex(e => e.ConfigKey, "UQ_AppConfig_Key").IsUnique();
            entity.Property(e => e.ConfigKey).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.ConfigValue).HasMaxLength(2000);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Category).HasMaxLength(50).IsUnicode(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__Role__3214EC27");
            entity.ToTable("Role");
            entity.HasIndex(e => e.Name, "UQ_Role_Name").IsUnique();
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__RolePermission__3214EC27");
            entity.ToTable("RolePermission");
            entity.HasIndex(e => new { e.RoleID, e.PermissionID }, "UQ_RolePermission").IsUnique();
            entity.Property(e => e.CanView).HasDefaultValue(false);
            entity.Property(e => e.CanCreate).HasDefaultValue(false);
            entity.Property(e => e.CanEdit).HasDefaultValue(false);
            entity.Property(e => e.CanDelete).HasDefaultValue(false);

            entity.HasOne(d => d.Role).WithMany(p => p.RolePermissions)
                .HasForeignKey(d => d.RoleID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RolePermission_Role");

            entity.HasOne(d => d.Permission).WithMany(p => p.RolePermissions)
                .HasForeignKey(d => d.PermissionID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RolePermission_Permission");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__UserRole__3214EC27");
            entity.ToTable("UserRole");
            entity.HasIndex(e => new { e.SystemUserID, e.RoleID }, "UQ_UserRole").IsUnique();
            entity.Property(e => e.AssignedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.SystemUser).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.SystemUserID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserRole_SystemUser");

            entity.HasOne(d => d.Role).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.RoleID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserRole_Role");

            entity.HasOne(d => d.AssignedByNavigation).WithMany(p => p.UserRoleAssignedByNavigations)
                .HasForeignKey(d => d.AssignedBy)
                .HasConstraintName("FK_UserRole_AssignedBy");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

