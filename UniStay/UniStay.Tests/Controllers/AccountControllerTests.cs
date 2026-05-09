using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using UniStay.Controllers;
using UniStay.Data;
using UniStay.Models;
using UniStay.Services.Interfaces;
using UniStay.ViewModels.Account;
using Xunit;

namespace UniStay.Tests.Controllers
{
    public class AccountControllerTests : IDisposable
    {
        private readonly AssuitDbContext _context;
        private readonly Mock<IPasswordService> _mockPasswordService;
        private readonly Mock<IAuditService> _mockAuditService;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly AccountController _controller;

        public AccountControllerTests()
        {
            var options = new DbContextOptionsBuilder<AssuitDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AssuitDbContext(options);
            _mockPasswordService = new Mock<IPasswordService>();
            _mockAuditService = new Mock<IAuditService>();
            _mockEmailService = new Mock<IEmailService>();

            _controller = new AccountController(
                _context,
                _mockPasswordService.Object,
                _mockAuditService.Object,
                _mockEmailService.Object
            );

            // Mock HttpContext for Authentication
            var authServiceMock = new Mock<IAuthenticationService>();
            authServiceMock
                .Setup(a => a.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
                .ReturnsAsync(AuthenticateResult.NoResult());

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(s => s.GetService(typeof(IAuthenticationService)))
                .Returns(authServiceMock.Object);
                
            var urlHelperMock = new Mock<IUrlHelper>();
            urlHelperMock.Setup(u => u.IsLocalUrl(It.IsAny<string>())).Returns(true);
            _controller.Url = urlHelperMock.Object;

            var httpContext = new DefaultHttpContext
            {
                RequestServices = serviceProviderMock.Object
            };
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            var tempDataMock = new Mock<ITempDataDictionary>();
            _controller.TempData = tempDataMock.Object;
        }

        [Fact]
        public async Task Login_Get_ReturnsView_WhenNotAuthenticated()
        {
            // Act
            var result = await _controller.Login();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<LoginViewModel>(viewResult.Model);
        }

        [Fact]
        public async Task Login_Post_InvalidModel_ReturnsView()
        {
            // Arrange
            _controller.ModelState.AddModelError("NationalID", "Required");
            var model = new LoginViewModel();

            // Act
            var result = await _controller.Login(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
        }

        [Fact]
        public async Task Login_Post_UserNotFound_ReturnsViewWithError()
        {
            // Arrange
            var model = new LoginViewModel { NationalID = "12345678901234", Password = "Password123!" };

            // Act
            var result = await _controller.Login(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("بيانات الدخول غير صحيحة", model.ErrorMessage);
        }

        [Fact]
        public async Task Login_Post_ValidCredentials_RedirectsToAdmin_WhenSuperAdmin()
        {
            // Arrange
            var user = new SystemUser
            {
                Name = "Admin",
                NationalID = "12345678901234",
                PasswordHash = "hashedPassword",
                IsActive = true,
                IsSuperAdmin = true,
                MustChangePassword = false
            };
            _context.SystemUsers.Add(user);
            await _context.SaveChangesAsync();

            var model = new LoginViewModel { NationalID = "12345678901234", Password = "Password123!" };
            _mockPasswordService.Setup(p => p.VerifyPassword(model.Password, user.PasswordHash)).Returns(true);

            // Act
            var result = await _controller.Login(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Admin", redirectResult.ControllerName);
        }
        
        [Fact]
        public async Task ForgotPassword_Post_ValidUser_SendsEmail()
        {
            // Arrange
            var user = new SystemUser
            {
                Email = "test@example.com",
                NationalID = "12345678901234",
                IsActive = true,
                IsDeleted = false
            };
            _context.SystemUsers.Add(user);
            await _context.SaveChangesAsync();

            var model = new ForgotPasswordViewModel { Identifier = "test@example.com" };

            // Act
            var result = await _controller.ForgotPassword(model);

            // Assert
            _mockEmailService.Verify(e => e.SendAsync(user.Email, It.IsAny<string>(), It.IsAny<string>(), EmailType.General), Times.Once);
            Assert.True(user.MustChangePassword);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
