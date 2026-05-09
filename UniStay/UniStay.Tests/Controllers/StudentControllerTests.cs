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
using Xunit;

namespace UniStay.Tests.Controllers
{
    public class StudentControllerTests : IDisposable
    {
        private readonly AssuitDbContext _context;
        private readonly Mock<IAuditService> _mockAuditService;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly StudentController _controller;
        private readonly int _studentId = 1;

        public StudentControllerTests()
        {
            var options = new DbContextOptionsBuilder<AssuitDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AssuitDbContext(options);
            _mockAuditService = new Mock<IAuditService>();
            _mockEmailService = new Mock<IEmailService>();

            _controller = new StudentController(
                _context,
                _mockAuditService.Object,
                _mockEmailService.Object
            );

            // Mock HttpContext with Student ID Claim
            var claims = new List<Claim>
            {
                new Claim("StudentID", _studentId.ToString())
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

        [Fact]
        public async Task Home_Get_ReturnsViewWithStudentData_WhenStudentExists()
        {
            // Arrange
            var student = new Student
            {
                ID = _studentId,
                FullName = "Test Student",
                NationalID = "12345678901234",
                PasswordHash = "hashed",
                Phone = "123456789",
                Email = "test@student.com"
            };
            var application = new Application
            {
                ID = 1,
                StudentID = _studentId,
                Status = "Accepted",
                AcademicYear = "2025/2026",
                CreatedAt = DateTime.UtcNow
            };
            _context.Students.Add(student);
            _context.Applications.Add(application);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Home();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Test Student", viewResult.ViewData["StudentName"]);
            Assert.Equal("Accepted", viewResult.ViewData["LatestStatus"]);
        }

        [Fact]
        public async Task Home_Get_RedirectsToLogin_WhenStudentNotAuthenticated()
        {
            // Arrange
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity()); // Empty user

            // Act
            var result = await _controller.Home();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
            Assert.Equal("StudentAccount", redirectResult.ControllerName);
        }

        [Fact]
        public async Task ApplicationSchedule_Get_ReturnsViewWithSchedules()
        {
            // Arrange
            var schedule = new ApplicationSchedule
            {
                ID = 1,
                AcademicYear = DateTime.Now.Year.ToString(),
                NewStudentsOpenDate = DateTime.Now.AddDays(-1),
                NewStudentsCloseDate = DateTime.Now.AddDays(10),
                DormitoryCity = new DormitoryCity { ID = 1, Name = "City 1", IsActive = true, Location = "Loc" }
            };
            _context.ApplicationSchedules.Add(schedule);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.ApplicationSchedule();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<ApplicationSchedule>>(viewResult.Model);
            Assert.Single(model);
        }
        
        [Fact]
        public async Task ReserveRoom_Post_ValidData_ReservesRoomAndCreatesPayment()
        {
            // Arrange
            var city = new DormitoryCity { ID = 1, Name = "City", IsActive = true, Location = "Loc" };
            var building = new CityBuilding { ID = 1, DormitoryCityID = 1, BuildingName = "B1", IsActive = true };
            var room = new CityRoom { ID = 1, CityBuildingID = 1, RoomNumber = "101", BedsCount = 2, CurrentOccupancy = 0, IsActive = true };
            var application = new Application { ID = 1, StudentID = _studentId, Status = "Accepted", AcademicYear = "2025", DormitoryCityID = 1 };
            
            _context.DormitoryCities.Add(city);
            _context.CityBuildings.Add(building);
            _context.CityRooms.Add(room);
            _context.Applications.Add(application);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.ReserveRoom(1, 1);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var jsonResponse = jsonResult.Value as dynamic;
            
            Assert.True((bool)jsonResponse.success);
            Assert.Equal("تم حجز الغرفة. لديك 24 ساعة للدفع.", (string)jsonResponse.message);
            
            var allocation = await _context.Allocations.FirstOrDefaultAsync(a => a.StudentID == _studentId);
            Assert.NotNull(allocation);
            Assert.Equal("Reserved", allocation.Status);
            Assert.Equal(1, allocation.CityRoomID);
            Assert.Equal(1, allocation.BedNumber);
            
            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.StudentID == _studentId);
            Assert.NotNull(payment);
            Assert.Equal("Pending", payment.Status);
            Assert.Equal(1000, payment.Amount);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
