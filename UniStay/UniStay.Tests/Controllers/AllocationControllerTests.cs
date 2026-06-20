using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using UniStay.Controllers;
using UniStay.Data;
using UniStay.Models;
using UniStay.Services.Interfaces;
using UniStay.ViewModels.Allocation;
using Xunit;

namespace UniStay.Tests.Controllers
{
    public class AllocationControllerTests : IDisposable
    {
        private readonly AssuitDbContext _context;
        private readonly Mock<IAuditService> _mockAuditService;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly AllocationController _controller;
        private readonly int _currentUserId = 99;

        public AllocationControllerTests()
        {
            var options = new DbContextOptionsBuilder<AssuitDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AssuitDbContext(options);
            _mockAuditService = new Mock<IAuditService>();
            _mockEmailService = new Mock<IEmailService>();

            _controller = new AllocationController(
                _context,
                _mockAuditService.Object,
                _mockEmailService.Object
            );

            // Mock HttpContext with UserID Claim
            var claims = new List<Claim>
            {
                new Claim("UserID", _currentUserId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = claimsPrincipal
                }
            };

            var tempDataMock = new Mock<ITempDataDictionary>();
            _controller.TempData = tempDataMock.Object;
        }

        private async Task SeedLookupDataAsync()
        {
            // Seed Faculties
            var faculties = new List<Faculty>
            {
                new() { ID = 1, Name = "حاسبات ومعلومات", IsActive = true },
                new() { ID = 2, Name = "الهندسة", IsActive = true },
                new() { ID = 3, Name = "الطب", IsActive = false } // Inactive faculty
            };
            _context.Faculties.AddRange(faculties);

            // Seed Dormitory Cities
            var city = new DormitoryCity
            {
                ID = 1,
                Name = "مدينة أسيوط",
                IsActive = true,
                Location = "أسيوط",
                CityType = "بنين"
            };
            _context.DormitoryCities.Add(city);

            // Seed Buildings
            var buildings = new List<CityBuilding>
            {
                new() { ID = 1, DormitoryCityID = 1, BuildingName = "مبنى أ", BuildingType = "Dormitory", IsActive = true, IsDeleted = false },
                new() { ID = 2, DormitoryCityID = 1, BuildingName = "مبنى ب", BuildingType = "Dormitory", IsActive = true, IsDeleted = false },
                new() { ID = 3, DormitoryCityID = 1, BuildingName = "مبنى جـ", BuildingType = "Dormitory", IsActive = false, IsDeleted = false }, // Inactive building
                new() { ID = 4, DormitoryCityID = 1, BuildingName = "مبنى د", BuildingType = "Dormitory", IsActive = true, IsDeleted = true } // Deleted building
            };
            _context.CityBuildings.AddRange(buildings);

            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task GroupEvacuation_Get_ReturnsViewWithAllActiveAllocatedStudents_WhenNoFiltersApplied()
        {
            // Arrange
            await SeedLookupDataAsync();

            var student1 = new Student
            {
                ID = 1,
                FullName = "أحمد محمد علي",
                NationalID = "29801011234567",
                Faculty = "حاسبات ومعلومات",
                Phone = "01000000001",
                Email = "a@a.com",
                Gender = "Male",
                Nationality = "مصري",
                Religion = "مسلم",
                BirthDate = new DateOnly(2000, 1, 1)
            };
            var student2 = new Student
            {
                ID = 2,
                FullName = "محمود حسن",
                NationalID = "29801017654321",
                Faculty = "الهندسة",
                Phone = "01000000002",
                Email = "b@b.com",
                Gender = "Male",
                Nationality = "مصري",
                Religion = "مسلم",
                BirthDate = new DateOnly(2000, 1, 1)
            };
            _context.Students.AddRange(student1, student2);

            var room1 = new CityRoom { ID = 1, CityBuildingID = 1, RoomNumber = "101", BedsCount = 2, CurrentOccupancy = 1, IsActive = true };
            var room2 = new CityRoom { ID = 2, CityBuildingID = 2, RoomNumber = "102", BedsCount = 2, CurrentOccupancy = 1, IsActive = true };
            _context.CityRooms.AddRange(room1, room2);

            var app1 = new Application { ID = 1, StudentID = 1, Status = "Accepted", AcademicYear = "2025/2026", StudentType = "Old", HousingType = "Regular", DormitoryCityID = 1, ServerVerificationStatus = "Verified" };
            var app2 = new Application { ID = 2, StudentID = 2, Status = "Accepted", AcademicYear = "2025/2026", StudentType = "Old", HousingType = "Regular", DormitoryCityID = 1, ServerVerificationStatus = "Verified" };
            var app3 = new Application { ID = 3, StudentID = 2, Status = "Accepted", AcademicYear = "2024/2025", StudentType = "Old", HousingType = "Regular", DormitoryCityID = 1, ServerVerificationStatus = "Verified" };
            _context.Applications.AddRange(app1, app2, app3);

            var alloc1 = new Allocation { ID = 1, StudentID = 1, ApplicationID = 1, CityRoomID = 1, BedNumber = 1, AcademicYear = "2025/2026", Status = "Active", StartDate = DateOnly.FromDateTime(DateTime.Today) };
            var alloc2 = new Allocation { ID = 2, StudentID = 2, ApplicationID = 2, CityRoomID = 2, BedNumber = 1, AcademicYear = "2025/2026", Status = "Active", StartDate = DateOnly.FromDateTime(DateTime.Today) };
            var alloc3 = new Allocation { ID = 3, StudentID = 2, ApplicationID = 3, CityRoomID = 2, BedNumber = 2, AcademicYear = "2024/2025", Status = "Evicted" }; // Inactive allocation
            _context.Allocations.AddRange(alloc1, alloc2, alloc3);

            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GroupEvacuation(null, null, null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<GroupEvacuationViewModel>(viewResult.Model);

            // Checks that only active allocations are returned
            Assert.Equal(2, model.AllocatedStudents.Count);
            Assert.Contains(model.AllocatedStudents, s => s.StudentID == 1 && s.StudentName == "أحمد محمد علي");
            Assert.Contains(model.AllocatedStudents, s => s.StudentID == 2 && s.StudentName == "محمود حسن");

            // Lookup validations: active/deleted rules applied
            Assert.Equal(2, model.Buildings.Count); // مبنى أ, مبنى ب (active and not deleted)
            Assert.Equal(2, model.Faculties.Count); // حاسبات ومعلومات, الهندسة (active faculties)
        }

        [Fact]
        public async Task GroupEvacuation_Get_FiltersBySearchTerm_ReturnsMatchingStudents()
        {
            // Arrange
            await SeedLookupDataAsync();

            var student1 = new Student { ID = 1, FullName = "أحمد محمد علي", NationalID = "29801011234567", Faculty = "حاسبات ومعلومات", Phone = "0", Email = "a", Gender = "Male", Nationality = "مصري", Religion = "مسلم", BirthDate = new DateOnly(2000, 1, 1) };
            var student2 = new Student { ID = 2, FullName = "محمود حسن", NationalID = "29801017654321", Faculty = "الهندسة", Phone = "0", Email = "b", Gender = "Male", Nationality = "مصري", Religion = "مسلم", BirthDate = new DateOnly(2000, 1, 1) };
            _context.Students.AddRange(student1, student2);

            var room = new CityRoom { ID = 1, CityBuildingID = 1, RoomNumber = "101", BedsCount = 2, CurrentOccupancy = 2, IsActive = true };
            _context.CityRooms.Add(room);

            var app1 = new Application { ID = 1, StudentID = 1, Status = "Accepted", AcademicYear = "2025", StudentType = "Old", HousingType = "Regular", DormitoryCityID = 1, ServerVerificationStatus = "Verified" };
            var app2 = new Application { ID = 2, StudentID = 2, Status = "Accepted", AcademicYear = "2025", StudentType = "Old", HousingType = "Regular", DormitoryCityID = 1, ServerVerificationStatus = "Verified" };
            _context.Applications.AddRange(app1, app2);

            var alloc1 = new Allocation { ID = 1, StudentID = 1, ApplicationID = 1, CityRoomID = 1, BedNumber = 1, AcademicYear = "2025", Status = "Active" };
            var alloc2 = new Allocation { ID = 2, StudentID = 2, ApplicationID = 2, CityRoomID = 1, BedNumber = 2, AcademicYear = "2025", Status = "Active" };
            _context.Allocations.AddRange(alloc1, alloc2);

            await _context.SaveChangesAsync();

            // Act - Search by FullName part
            var resultByName = await _controller.GroupEvacuation("حسن", null, null);
            var modelByName = Assert.IsType<GroupEvacuationViewModel>(((ViewResult)resultByName).Model);

            // Assert - Name match
            Assert.Single(modelByName.AllocatedStudents);
            Assert.Equal("محمود حسن", modelByName.AllocatedStudents.First().StudentName);

            // Act - Search by National ID part
            var resultById = await _controller.GroupEvacuation("1234567", null, null);
            var modelById = Assert.IsType<GroupEvacuationViewModel>(((ViewResult)resultById).Model);

            // Assert - ID match
            Assert.Single(modelById.AllocatedStudents);
            Assert.Equal("أحمد محمد علي", modelById.AllocatedStudents.First().StudentName);
        }

        [Fact]
        public async Task GroupEvacuation_Get_FiltersByBuildingId_ReturnsMatchingStudents()
        {
            // Arrange
            await SeedLookupDataAsync();

            var student1 = new Student { ID = 1, FullName = "أحمد محمد علي", NationalID = "29801011234567", Faculty = "حاسبات ومعلومات", Phone = "0", Email = "a", Gender = "Male", Nationality = "مصري", Religion = "مسلم", BirthDate = new DateOnly(2000, 1, 1) };
            var student2 = new Student { ID = 2, FullName = "محمود حسن", NationalID = "29801017654321", Faculty = "الهندسة", Phone = "0", Email = "b", Gender = "Male", Nationality = "مصري", Religion = "مسلم", BirthDate = new DateOnly(2000, 1, 1) };
            _context.Students.AddRange(student1, student2);

            var room1 = new CityRoom { ID = 1, CityBuildingID = 1, RoomNumber = "101", BedsCount = 2, CurrentOccupancy = 1, IsActive = true };
            var room2 = new CityRoom { ID = 2, CityBuildingID = 2, RoomNumber = "102", BedsCount = 2, CurrentOccupancy = 1, IsActive = true };
            _context.CityRooms.AddRange(room1, room2);

            var app1 = new Application { ID = 1, StudentID = 1, Status = "Accepted", AcademicYear = "2025", StudentType = "Old", HousingType = "Regular", DormitoryCityID = 1, ServerVerificationStatus = "Verified" };
            var app2 = new Application { ID = 2, StudentID = 2, Status = "Accepted", AcademicYear = "2025", StudentType = "Old", HousingType = "Regular", DormitoryCityID = 1, ServerVerificationStatus = "Verified" };
            _context.Applications.AddRange(app1, app2);

            var alloc1 = new Allocation { ID = 1, StudentID = 1, ApplicationID = 1, CityRoomID = 1, BedNumber = 1, AcademicYear = "2025", Status = "Active" };
            var alloc2 = new Allocation { ID = 2, StudentID = 2, ApplicationID = 2, CityRoomID = 2, BedNumber = 1, AcademicYear = "2025", Status = "Active" };
            _context.Allocations.AddRange(alloc1, alloc2);

            await _context.SaveChangesAsync();

            // Act - Filter by building ID = 2 (مبنى ب)
            var result = await _controller.GroupEvacuation(null, 2, null);
            var model = Assert.IsType<GroupEvacuationViewModel>(((ViewResult)result).Model);

            // Assert
            Assert.Single(model.AllocatedStudents);
            Assert.Equal("محمود حسن", model.AllocatedStudents.First().StudentName);
        }

        [Fact]
        public async Task GroupEvacuation_Get_FiltersByFaculty_ReturnsMatchingStudents()
        {
            // Arrange
            await SeedLookupDataAsync();

            var student1 = new Student { ID = 1, FullName = "أحمد محمد علي", NationalID = "29801011234567", Faculty = "حاسبات ومعلومات", Phone = "0", Email = "a", Gender = "Male", Nationality = "مصري", Religion = "مسلم", BirthDate = new DateOnly(2000, 1, 1) };
            var student2 = new Student { ID = 2, FullName = "محمود حسن", NationalID = "29801017654321", Faculty = "الهندسة", Phone = "0", Email = "b", Gender = "Male", Nationality = "مصري", Religion = "مسلم", BirthDate = new DateOnly(2000, 1, 1) };
            _context.Students.AddRange(student1, student2);

            var room = new CityRoom { ID = 1, CityBuildingID = 1, RoomNumber = "101", BedsCount = 2, CurrentOccupancy = 2, IsActive = true };
            _context.CityRooms.Add(room);

            var app1 = new Application { ID = 1, StudentID = 1, Status = "Accepted", AcademicYear = "2025", StudentType = "Old", HousingType = "Regular", DormitoryCityID = 1, ServerVerificationStatus = "Verified" };
            var app2 = new Application { ID = 2, StudentID = 2, Status = "Accepted", AcademicYear = "2025", StudentType = "Old", HousingType = "Regular", DormitoryCityID = 1, ServerVerificationStatus = "Verified" };
            _context.Applications.AddRange(app1, app2);

            var alloc1 = new Allocation { ID = 1, StudentID = 1, ApplicationID = 1, CityRoomID = 1, BedNumber = 1, AcademicYear = "2025", Status = "Active" };
            var alloc2 = new Allocation { ID = 2, StudentID = 2, ApplicationID = 2, CityRoomID = 1, BedNumber = 2, AcademicYear = "2025", Status = "Active" };
            _context.Allocations.AddRange(alloc1, alloc2);

            await _context.SaveChangesAsync();

            // Act - Filter by Faculty "حاسبات"
            var result = await _controller.GroupEvacuation(null, null, "حاسبات");
            var model = Assert.IsType<GroupEvacuationViewModel>(((ViewResult)result).Model);

            // Assert
            Assert.Single(model.AllocatedStudents);
            Assert.Equal("أحمد محمد علي", model.AllocatedStudents.First().StudentName);
        }

        [Fact]
        public async Task GroupEvacuation_Post_InvalidModelOrNoSelectedStudents_ReturnsErrorAndRedirects()
        {
            // Arrange
            var model = new GroupEvacuationViewModel
            {
                SelectedStudentIDs = new List<int>(), // Empty list
                Reason = "إخلاء جماعي إداري",
                BuildingID = 1,
                Faculty = "الهندسة"
            };

            // Act
            var result = await _controller.GroupEvacuation(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("GroupEvacuation", redirectResult.ActionName);
            Assert.Equal(1, redirectResult.RouteValues?["buildingId"]);
            Assert.Equal("الهندسة", redirectResult.RouteValues?["faculty"]);
        }

        [Fact]
        public async Task GroupEvacuation_Post_ValidSelectedStudents_SuccessfullyEvictsStudentsAndDecrementsOccupancy()
        {
            // Arrange
            await SeedLookupDataAsync();

            var student = new Student { ID = 1, FullName = "أحمد محمد علي", NationalID = "29801011234567", Faculty = "حاسبات ومعلومات", Phone = "0", Email = "a@example.com", Gender = "Male", Nationality = "مصري", Religion = "مسلم", BirthDate = new DateOnly(2000, 1, 1) };
            _context.Students.Add(student);

            var room = new CityRoom { ID = 1, CityBuildingID = 1, RoomNumber = "101", BedsCount = 2, CurrentOccupancy = 1, IsActive = true };
            _context.CityRooms.Add(room);

            var app = new Application { ID = 1, StudentID = 1, Status = "Accepted", AcademicYear = "2025/2026", StudentType = "Old", HousingType = "Regular", IsActive = true, DormitoryCityID = 1, ServerVerificationStatus = "Verified" };
            _context.Applications.Add(app);

            var alloc = new Allocation { ID = 1, StudentID = 1, ApplicationID = 1, CityRoomID = 1, BedNumber = 1, AcademicYear = "2025/2026", Status = "Active" };
            _context.Allocations.Add(alloc);

            await _context.SaveChangesAsync();

            var model = new GroupEvacuationViewModel
            {
                SelectedStudentIDs = new List<int> { 1 },
                Reason = "نهاية العام الدراسي",
                EvictionType = "Graduation",
                BuildingID = 1,
                Faculty = "حاسبات ومعلومات"
            };

            // Act
            var result = await _controller.GroupEvacuation(model);

            // Assert
            // 1. Redirect Assertions
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("GroupEvacuation", redirectResult.ActionName);

            // 2. State update Assertions
            var updatedAlloc = await _context.Allocations.FindAsync(1);
            Assert.NotNull(updatedAlloc);
            Assert.Equal("Evicted", updatedAlloc.Status);
            Assert.Equal(DateOnly.FromDateTime(DateTime.Today), updatedAlloc.EndDate);

            var updatedApp = await _context.Applications.FindAsync(1);
            Assert.NotNull(updatedApp);
            Assert.False(updatedApp.IsActive);

            var updatedRoom = await _context.CityRooms.FindAsync(1);
            Assert.NotNull(updatedRoom);
            Assert.Equal(0, updatedRoom.CurrentOccupancy);

            // 3. Eviction Notice Created
            var notice = await _context.EvictionNotices.FirstOrDefaultAsync(n => n.StudentID == 1);
            Assert.NotNull(notice);
            Assert.Equal(1, notice.AllocationID);
            Assert.Equal("نهاية العام الدراسي", notice.Reason);
            Assert.Equal("Graduation", notice.EvictionType);
            Assert.Equal("Executed", notice.Status);
            Assert.Equal(_currentUserId, notice.IssuedBy);

            // 4. Audit Service Logged
            _mockAuditService.Verify(a => a.LogAsync(
                _currentUserId,
                "Staff",
                "Allocation.GroupEvacuation",
                "Allocation",
                1,
                It.IsAny<object>(),
                It.IsAny<object>(),
                It.IsAny<string>(),
                It.IsAny<int?>()
            ), Times.Once);
        }

        [Fact]
        public async Task GroupEvacuation_Post_SomeStudentsNotFound_EvictsFoundAndIncrementsFailCount()
        {
            // Arrange
            await SeedLookupDataAsync();

            var student = new Student { ID = 1, FullName = "أحمد محمد علي", NationalID = "29801011234567", Faculty = "حاسبات ومعلومات", Phone = "0", Email = "a@example.com", Gender = "Male", Nationality = "مصري", Religion = "مسلم", BirthDate = new DateOnly(2000, 1, 1) };
            _context.Students.Add(student);

            var room = new CityRoom { ID = 1, CityBuildingID = 1, RoomNumber = "101", BedsCount = 2, CurrentOccupancy = 1, IsActive = true };
            _context.CityRooms.Add(room);

            var app = new Application { ID = 1, StudentID = 1, Status = "Accepted", AcademicYear = "2025/2026", StudentType = "Old", HousingType = "Regular", IsActive = true, DormitoryCityID = 1, ServerVerificationStatus = "Verified" };
            _context.Applications.Add(app);

            var alloc = new Allocation { ID = 1, StudentID = 1, ApplicationID = 1, CityRoomID = 1, BedNumber = 1, AcademicYear = "2025/2026", Status = "Active" };
            _context.Allocations.Add(alloc);

            await _context.SaveChangesAsync();

            var model = new GroupEvacuationViewModel
            {
                SelectedStudentIDs = new List<int> { 1, 999 }, // 999 is invalid/doesn't exist
                Reason = "إخلاء إداري",
                EvictionType = "Administrative"
            };

            // Act
            var result = await _controller.GroupEvacuation(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("GroupEvacuation", redirectResult.ActionName);

            // Verify student 1 was evicted
            var updatedAlloc = await _context.Allocations.FindAsync(1);
            Assert.NotNull(updatedAlloc);
            Assert.Equal("Evicted", updatedAlloc.Status);

            // Verify audit logged only for student 1
            _mockAuditService.Verify(a => a.LogAsync(
                _currentUserId,
                "Staff",
                "Allocation.GroupEvacuation",
                "Allocation",
                1,
                It.IsAny<object>(),
                It.IsAny<object>(),
                It.IsAny<string>(),
                It.IsAny<int?>()
            ), Times.Once);

            _mockAuditService.Verify(a => a.LogAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                999, // Should NOT log for 999
                It.IsAny<object>(),
                It.IsAny<object>(),
                It.IsAny<string>(),
                It.IsAny<int?>()
            ), Times.Never);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
