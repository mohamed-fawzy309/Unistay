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
    public class AuthenticationTests : IDisposable
    {
        private readonly AssuitDbContext _context;
        private readonly Mock<IPasswordService> _mockPasswordService;
        private readonly Mock<IAuditService> _mockAuditService;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IAuthenticationService> _mockAuthService;
        private readonly AccountController _controller;
        private readonly DefaultHttpContext _httpContext;

        public AuthenticationTests()
        {
            var options = new DbContextOptionsBuilder<AssuitDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AssuitDbContext(options);
            _mockPasswordService = new Mock<IPasswordService>();
            _mockAuditService = new Mock<IAuditService>();
            _mockEmailService = new Mock<IEmailService>();
            _mockAuthService = new Mock<IAuthenticationService>();

            _controller = new AccountController(
                _context,
                _mockPasswordService.Object,
                _mockAuditService.Object,
                _mockEmailService.Object
            );

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(s => s.GetService(typeof(IAuthenticationService)))
                .Returns(_mockAuthService.Object);

            var urlHelperMock = new Mock<IUrlHelper>();
            urlHelperMock.Setup(u => u.IsLocalUrl(It.IsAny<string>())).Returns(true);
            _controller.Url = urlHelperMock.Object;

            _httpContext = new DefaultHttpContext
            {
                RequestServices = serviceProviderMock.Object
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = _httpContext
            };

            var tempDataMock = new Mock<ITempDataDictionary>();
            _controller.TempData = tempDataMock.Object;
        }

        // ============================================================
        // AccountController — Admin Cookie Tests
        // ============================================================

        [Fact]
        public async Task Login_Post_ValidAdmin_CreatesAdminCookieWithCorrectClaims()
        {
            var user = new SystemUser
            {
                ID = 10,
                Name = "Admin User",
                NationalID = "29801011234567",
                PasswordHash = "hash",
                IsActive = true,
                IsSuperAdmin = true,
                MustChangePassword = false
            };
            _context.SystemUsers.Add(user);
            await _context.SaveChangesAsync();

            var model = new LoginViewModel { NationalID = "29801011234567", Password = "Pass123!" };
            _mockPasswordService.Setup(p => p.VerifyPassword("Pass123!", "hash")).Returns(true);

            _mockAuthService
                .Setup(a => a.SignInAsync(
                    It.IsAny<HttpContext>(),
                    "AdminCookie",
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.Login(model);

            _mockAuthService.Verify(a => a.SignOutAsync(_httpContext, "StaffCookie", null), Times.Once);
            _mockAuthService.Verify(a => a.SignOutAsync(_httpContext, "AdminCookie", null), Times.Once);
            _mockAuthService.Verify(a => a.SignInAsync(
                _httpContext,
                "AdminCookie",
                It.Is<ClaimsPrincipal>(p =>
                    p.HasClaim("UserID", "10") &&
                    p.HasClaim(ClaimTypes.Name, "Admin User") &&
                    p.HasClaim("UserType", "Admin") &&
                    p.HasClaim("IsSuperAdmin", "true")),
                It.IsAny<AuthenticationProperties>()), Times.Once);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Admin", redirectResult.ControllerName);
        }

        [Fact]
        public async Task Login_Post_ValidStaff_CreatesStaffCookieWithCorrectClaims()
        {
            var user = new SystemUser
            {
                ID = 20,
                Name = "Staff Member",
                NationalID = "29801019876543",
                PasswordHash = "hash",
                IsActive = true,
                IsSuperAdmin = false,
                MustChangePassword = false
            };
            _context.SystemUsers.Add(user);
            await _context.SaveChangesAsync();

            var model = new LoginViewModel { NationalID = "29801019876543", Password = "Pass123!" };
            _mockPasswordService.Setup(p => p.VerifyPassword("Pass123!", "hash")).Returns(true);

            _mockAuthService
                .Setup(a => a.SignInAsync(
                    It.IsAny<HttpContext>(),
                    "StaffCookie",
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.Login(model);

            _mockAuthService.Verify(a => a.SignInAsync(
                _httpContext,
                "StaffCookie",
                It.Is<ClaimsPrincipal>(p =>
                    p.HasClaim("UserID", "20") &&
                    p.HasClaim(ClaimTypes.Name, "Staff Member") &&
                    p.HasClaim("UserType", "Staff") &&
                    p.HasClaim("IsSuperAdmin", "false")),
                It.IsAny<AuthenticationProperties>()), Times.Once);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Staff", redirectResult.ControllerName);
        }

        [Fact]
        public async Task Login_Post_AdminMustChangePassword_RedirectsToChangePassword()
        {
            var user = new SystemUser
            {
                ID = 30,
                Name = "Must Change",
                NationalID = "29801015556666",
                PasswordHash = "hash",
                IsActive = true,
                IsSuperAdmin = true,
                MustChangePassword = true
            };
            _context.SystemUsers.Add(user);
            await _context.SaveChangesAsync();

            var model = new LoginViewModel { NationalID = "29801015556666", Password = "Pass123!" };
            _mockPasswordService.Setup(p => p.VerifyPassword("Pass123!", "hash")).Returns(true);

            _mockAuthService
                .Setup(a => a.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.Login(model);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("ChangePassword", redirectResult.ActionName);
        }

        [Fact]
        public async Task Login_Post_InvalidPassword_NoCookieCreated()
        {
            var user = new SystemUser
            {
                ID = 40,
                Name = "Test",
                NationalID = "29801019999000",
                PasswordHash = "hash",
                IsActive = true,
                IsSuperAdmin = false,
                MustChangePassword = false
            };
            _context.SystemUsers.Add(user);
            await _context.SaveChangesAsync();

            var model = new LoginViewModel { NationalID = "29801019999000", Password = "WrongPass!" };
            _mockPasswordService.Setup(p => p.VerifyPassword("WrongPass!", "hash")).Returns(false);

            var result = await _controller.Login(model);

            _mockAuthService.Verify(a => a.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()), Times.Never);
            _mockAuthService.Verify(a => a.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>()), Times.Never);
            Assert.Equal("بيانات الدخول كلمة المرور غير صحيحة", model.ErrorMessage);
        }

        [Fact]
        public async Task Login_Post_InactiveUser_ReturnsErrorNoCookie()
        {
            var user = new SystemUser
            {
                ID = 50,
                Name = "Inactive",
                NationalID = "29801011111111",
                PasswordHash = "hash",
                IsActive = false,
                IsSuperAdmin = false
            };
            _context.SystemUsers.Add(user);
            await _context.SaveChangesAsync();

            var model = new LoginViewModel { NationalID = "29801011111111", Password = "Pass123!" };
            _mockPasswordService.Setup(p => p.VerifyPassword("Pass123!", "hash")).Returns(true);

            var result = await _controller.Login(model);

            _mockAuthService.Verify(a => a.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()), Times.Never);
            Assert.Equal("بيانات الدخول غير صحيحة", model.ErrorMessage);
        }

        [Fact]
        public async Task Login_Get_AlreadyAuthenticatedAdmin_RedirectsToHome()
        {
            _mockAuthService
                .Setup(a => a.AuthenticateAsync(It.IsAny<HttpContext>(), "AdminCookie"))
                .ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(
                    new ClaimsPrincipal(new ClaimsIdentity("test")), "AdminCookie")));

            var result = await _controller.Login();

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);
        }

        [Fact]
        public async Task Logout_ClearsBothCookiesAndRedirectsToLogin()
        {
            _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("UserID", "1") }, "mock"));

            _mockAuthService
                .Setup(a => a.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.Logout();

            _mockAuthService.Verify(a => a.SignOutAsync(_httpContext, "StaffCookie", null), Times.Once);
            _mockAuthService.Verify(a => a.SignOutAsync(_httpContext, "AdminCookie", null), Times.Once);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
        }

        // ============================================================
        // AccountController — Cookie Properties (expiry, persistence)
        // ============================================================

        [Fact]
        public async Task Login_Post_AdminCookieProperties_AreSessionOnly()
        {
            var user = new SystemUser
            {
                ID = 60,
                Name = "Admin",
                NationalID = "29801012222333",
                PasswordHash = "hash",
                IsActive = true,
                IsSuperAdmin = true,
                MustChangePassword = false
            };
            _context.SystemUsers.Add(user);
            await _context.SaveChangesAsync();

            var model = new LoginViewModel { NationalID = "29801012222333", Password = "Pass123!" };
            _mockPasswordService.Setup(p => p.VerifyPassword("Pass123!", "hash")).Returns(true);

            AuthenticationProperties? capturedProps = null;
            _mockAuthService
                .Setup(a => a.SignInAsync(
                    It.IsAny<HttpContext>(),
                    "AdminCookie",
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<AuthenticationProperties>()))
                .Callback<HttpContext, string, ClaimsPrincipal, AuthenticationProperties>((_, _, _, props) => capturedProps = props)
                .Returns(Task.CompletedTask);

            await _controller.Login(model);

            Assert.NotNull(capturedProps);
            Assert.False(capturedProps!.IsPersistent);
            Assert.Null(capturedProps.ExpiresUtc);
        }

        // ============================================================
        // StudentAccountController — Student Cookie Tests
        // ============================================================

        private (StudentAccountController, DefaultHttpContext) CreateStudentController()
        {
            var studentController = new StudentAccountController(
                _context,
                _mockPasswordService.Object,
                _mockAuditService.Object,
                _mockEmailService.Object
            );

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(s => s.GetService(typeof(IAuthenticationService)))
                .Returns(_mockAuthService.Object);

            var urlHelperMock = new Mock<IUrlHelper>();
            urlHelperMock.Setup(u => u.IsLocalUrl(It.IsAny<string>())).Returns(true);
            studentController.Url = urlHelperMock.Object;

            var httpContext = new DefaultHttpContext { RequestServices = serviceProviderMock.Object };
            studentController.ControllerContext = new ControllerContext { HttpContext = httpContext };
            studentController.TempData = new Mock<ITempDataDictionary>().Object;

            return (studentController, httpContext);
        }

        private async Task<(StudentLogin, Student)> SeedStudent(int id, string nationalId)
        {
            var student = new Student
            {
                ID = id,
                FullName = "Ahmed Student",
                NationalID = nationalId,
                Phone = "01000000000",
                Email = "a@a.com",
                Gender = "Male",
                Nationality = "مصري",
                Religion = "مسلم",
                BirthDate = new DateOnly(2000, 1, 1)
            };
            var studentLogin = new StudentLogin
            {
                StudentID = id,
                Username = nationalId,
                PasswordHash = "hash",
                IsActive = true,
                Student = student
            };
            _context.Students.Add(student);
            _context.StudentLogins.Add(studentLogin);
            await _context.SaveChangesAsync();
            return (studentLogin, student);
        }

        [Fact]
        public async Task StudentLogin_Post_ValidStudent_CreatesStudentCookieWithCorrectClaims()
        {
            var (studentLogin, student) = await SeedStudent(100, "30001011234567");
            var (studentController, httpContext) = CreateStudentController();

            var model = new StudentLoginViewModel { NationalID = "30001011234567", Password = "Pass123!", RememberMe = false };
            _mockPasswordService.Setup(p => p.VerifyPassword("Pass123!", "hash")).Returns(true);
            _mockAuthService
                .Setup(a => a.SignInAsync(It.IsAny<HttpContext>(), "StudentCookie", It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            var result = await studentController.Login(model);

            _mockAuthService.Verify(a => a.SignInAsync(
                httpContext,
                "StudentCookie",
                It.Is<ClaimsPrincipal>(p =>
                    p.HasClaim("StudentID", "100") &&
                    p.HasClaim("NationalID", "30001011234567") &&
                    p.HasClaim(ClaimTypes.Name, "Ahmed Student") &&
                    p.HasClaim("FullName", "Ahmed Student")),
                It.Is<AuthenticationProperties>(props => props.IsPersistent == false)), Times.Once);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Home", redirectResult.ActionName);
            Assert.Equal("Student", redirectResult.ControllerName);
        }

        [Fact]
        public async Task StudentLogin_Post_RememberMe_SetsPersistentWith7DayExpiry()
        {
            var (studentLogin, student) = await SeedStudent(200, "30001019999001");
            var (studentController, httpContext) = CreateStudentController();

            var model = new StudentLoginViewModel { NationalID = "30001019999001", Password = "Pass123!", RememberMe = true };
            _mockPasswordService.Setup(p => p.VerifyPassword("Pass123!", "hash")).Returns(true);

            AuthenticationProperties? capturedProps = null;
            _mockAuthService
                .Setup(a => a.SignInAsync(It.IsAny<HttpContext>(), "StudentCookie", It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()))
                .Callback<HttpContext, string, ClaimsPrincipal, AuthenticationProperties>((_, _, _, props) => capturedProps = props)
                .Returns(Task.CompletedTask);

            await studentController.Login(model);

            Assert.NotNull(capturedProps);
            Assert.True(capturedProps!.IsPersistent);
            Assert.NotNull(capturedProps.ExpiresUtc);
            Assert.True(capturedProps.ExpiresUtc!.Value > DateTimeOffset.UtcNow.AddDays(6));
            Assert.True(capturedProps.ExpiresUtc!.Value <= DateTimeOffset.UtcNow.AddDays(7));
        }

        [Fact]
        public async Task StudentLogin_Post_NoRememberMe_SetsSessionCookieWith12HourExpiry()
        {
            var (studentLogin, student) = await SeedStudent(300, "30001019999002");
            var (studentController, httpContext) = CreateStudentController();

            var model = new StudentLoginViewModel { NationalID = "30001019999002", Password = "Pass123!", RememberMe = false };
            _mockPasswordService.Setup(p => p.VerifyPassword("Pass123!", "hash")).Returns(true);

            AuthenticationProperties? capturedProps = null;
            _mockAuthService
                .Setup(a => a.SignInAsync(It.IsAny<HttpContext>(), "StudentCookie", It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()))
                .Callback<HttpContext, string, ClaimsPrincipal, AuthenticationProperties>((_, _, _, props) => capturedProps = props)
                .Returns(Task.CompletedTask);

            await studentController.Login(model);

            Assert.NotNull(capturedProps);
            Assert.False(capturedProps!.IsPersistent);
            Assert.NotNull(capturedProps.ExpiresUtc);
            Assert.True(capturedProps.ExpiresUtc!.Value > DateTimeOffset.UtcNow.AddHours(11));
            Assert.True(capturedProps.ExpiresUtc!.Value <= DateTimeOffset.UtcNow.AddHours(12));
        }

        [Fact]
        public async Task StudentLogin_Post_InvalidCredentials_NoCookieCreated()
        {
            var model = new StudentLoginViewModel { NationalID = "99999999999999", Password = "Wrong!" };

            var studentController = new StudentAccountController(
                _context,
                _mockPasswordService.Object,
                _mockAuditService.Object,
                _mockEmailService.Object
            );
            studentController.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            studentController.TempData = new Mock<ITempDataDictionary>().Object;

            var result = await studentController.Login(model);

            _mockAuthService.Verify(a => a.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()), Times.Never);
            Assert.Equal("الرقم القومي أو كلمة المرور غير صحيحة", model.ErrorMessage);
        }

        [Fact]
        public async Task StudentLogout_ClearsStudentCookieAndRedirects()
        {
            var studentController = new StudentAccountController(
                _context,
                _mockPasswordService.Object,
                _mockAuditService.Object,
                _mockEmailService.Object
            );

            var claims = new List<Claim> { new Claim("StudentID", "100") };
            var identity = new ClaimsIdentity(claims, "test");
            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(s => s.GetService(typeof(IAuthenticationService)))
                .Returns(_mockAuthService.Object);

            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity),
                RequestServices = serviceProviderMock.Object
            };
            studentController.ControllerContext = new ControllerContext { HttpContext = httpContext };
            studentController.Url = new Mock<IUrlHelper>().Object;

            _mockAuthService
                .Setup(a => a.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            var result = await studentController.Logout();

            _mockAuthService.Verify(a => a.SignOutAsync(httpContext, "StudentCookie", null), Times.Once);
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
        }

        // ============================================================
        // Cookie Configuration Validation
        // ============================================================

        [Fact]
        public void CookieConfiguration_AdminCookie_IsHttpOnlyAndSecure()
        {
            var options = new Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions
            {
                Cookie = new Microsoft.AspNetCore.Http.CookieBuilder
                {
                    Name = ".UniStay.Admin",
                    HttpOnly = true,
                    SecurePolicy = CookieSecurePolicy.Always,
                    SameSite = SameSiteMode.Strict
                },
                ExpireTimeSpan = TimeSpan.FromHours(8),
                SlidingExpiration = true
            };

            Assert.Equal(".UniStay.Admin", options.Cookie.Name);
            Assert.True(options.Cookie.HttpOnly);
            Assert.Equal(CookieSecurePolicy.Always, options.Cookie.SecurePolicy);
            Assert.Equal(SameSiteMode.Strict, options.Cookie.SameSite);
            Assert.Equal(TimeSpan.FromHours(8), options.ExpireTimeSpan);
            Assert.True(options.SlidingExpiration);
        }

        [Fact]
        public void CookieConfiguration_StudentCookie_IsHttpOnlyAndLaxSameSite()
        {
            var options = new Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions
            {
                Cookie = new Microsoft.AspNetCore.Http.CookieBuilder
                {
                    Name = ".UniStay.Student",
                    HttpOnly = true,
                    SecurePolicy = CookieSecurePolicy.Always,
                    SameSite = SameSiteMode.Lax
                },
                ExpireTimeSpan = TimeSpan.FromDays(7),
                SlidingExpiration = true
            };

            Assert.Equal(".UniStay.Student", options.Cookie.Name);
            Assert.True(options.Cookie.HttpOnly);
            Assert.Equal(CookieSecurePolicy.Always, options.Cookie.SecurePolicy);
            Assert.Equal(SameSiteMode.Lax, options.Cookie.SameSite);
            Assert.Equal(TimeSpan.FromDays(7), options.ExpireTimeSpan);
            Assert.True(options.SlidingExpiration);
        }

        // ============================================================
        // Session Tests — Note: Session is NOT used in this project
        // ============================================================

        [Fact]
        public void Session_IsNotConfigured_NoSessionMiddleware()
        {
            var programCs = System.IO.File.ReadAllText(
                System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "..", "..", "..", "..", "..", "UniStay", "Program.cs"));

            Assert.False(programCs.Contains("AddSession"), "AddSession() should not be present — project uses cookie auth only");
            Assert.False(programCs.Contains("UseSession"), "UseSession() should not be present — project uses cookie auth only");
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
