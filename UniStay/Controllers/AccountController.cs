using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using UniStay.Data;
using UniStay.Models;
using UniStay.Services.Interfaces;
using UniStay.ViewModels.Account;

namespace UniStay.Controllers
{
    [Route("Account")]
    public class AccountController : Controller
    {
        private readonly AssuitDbContext _context;
        private readonly IPasswordService _passwordService;
        private readonly IAuditService _auditService;
        private readonly IEmailService _emailService;

        public AccountController(
            AssuitDbContext context,
            IPasswordService passwordService,
            IAuditService auditService,
            IEmailService emailService)
        {
            _context = context;
            _passwordService = passwordService;
            _auditService = auditService;
            _emailService = emailService;
        }
        [HttpGet("create-Assuit-university")]
        public async Task<IActionResult> CreateUniversityTemp()
        {
            try
            {
                // تحقق هل الجامعة موجودة
                var existing = await _context.Universities
                    .FirstOrDefaultAsync(u => u.Name == "جامعة أسيوط");

                if (existing != null)
                    return Content("University already exists");

                var university = new University
                {
                    Name = "جامعة أسيوط",
                    NameEn = "Assiut University",
                    Address = "أسيوط - جمهورية مصر العربية",
                    Phone = "0881234567",
                    Email = "info@aun.edu.eg",
                    Website = "https://www.aun.edu.eg",
                    Logo = "/images/Assuit_university_logo.png",
                    APIBaseUrl = null,
                    APIKey = null
                };

                _context.Universities.Add(university);
                await _context.SaveChangesAsync();

                return Content("University created successfully");
            }
            catch (Exception ex)
            {
                return Content(ex.ToString());
            }
        }

        [HttpGet("create")]
        public async Task<IActionResult> CreateAdminTemp()
        {
            // 🔥 غير القيم دي
            string name = "admin";
            string email = "admin@unistay.com";
            string nationalId = "12345678912345";
            string password = "12345678";

            // تحقق لو موجود
            var existing = await _context.SystemUsers
                .FirstOrDefaultAsync(x => x.Name == name);

            if (existing != null)
                return Content("Admin already exists");

            var admin = new SystemUser
            {
                Name = name,
                Email = email,
                NationalID = nationalId,
                Phone = "123456",
                PasswordHash = _passwordService.HashPassword(password),
                IsSuperAdmin = true,
                IsActive = true,
                MustChangePassword = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.SystemUsers.Add(admin);
            await _context.SaveChangesAsync();

            return Content("Admin created successfully");
        }

        // ============================================================
        // LOGIN
        // ============================================================

        [AllowAnonymous]
        [HttpGet("Login")]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            var adminAuth = await HttpContext.AuthenticateAsync("AdminCookie");
            var staffAuth = await HttpContext.AuthenticateAsync("StaffCookie");
            if (adminAuth.Succeeded || staffAuth.Succeeded)
                return RedirectToAction("Index", "Home");

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [AllowAnonymous]
        [HttpPost("Login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.SystemUsers
                .FirstOrDefaultAsync(u => u.Name == model.Name && !u.IsDeleted);

            if (user == null || user.IsActive != true)
            {
                model.ErrorMessage = "بيانات الدخول غير صحيحة";
                return View(model);
            }

            if (!_passwordService.VerifyPassword(model.Password, user.PasswordHash))
            {
                model.ErrorMessage = "بيانات الدخول كلمة المرور غير صحيحة";
                return View(model);
            }

            bool isAdmin = user.IsSuperAdmin == true;
            string scheme = isAdmin ? "AdminCookie" : "StaffCookie";

            var claims = new List<Claim>
            {
                new Claim("UserID", user.ID.ToString()),
                new Claim(ClaimTypes.Name, user.Name ?? ""),
                new Claim("UserType", isAdmin ? "Admin" : "Staff"),
                new Claim("IsSuperAdmin", isAdmin.ToString().ToLower())
            };

            var identity = new ClaimsIdentity(claims, scheme);
            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = false,
            };

            await HttpContext.SignOutAsync("StaffCookie");
            await HttpContext.SignOutAsync("AdminCookie");
            await HttpContext.SignInAsync(scheme, principal, authProperties);

            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(user.ID, isAdmin ? "Admin" : "Staff",
                "Login", "SystemUser", user.ID, null, null,
                HttpContext.Connection.RemoteIpAddress?.ToString());

            if (user.MustChangePassword == true)
                return RedirectToAction("ChangePassword");

            if (isAdmin)
            {
                return RedirectToAction("Index", "Admin");
            }
            return RedirectToAction("Index", "Staff");
        }

        // ============================================================
        // LOGOUT
        // ============================================================

        [Authorize(AuthenticationSchemes = "StaffCookie,AdminCookie")]
        [HttpGet("Logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("StaffCookie");
            await HttpContext.SignOutAsync("AdminCookie");
            return RedirectToAction("Login");
        }

        [AllowAnonymous]
        [HttpGet("AccessDenied")]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // ============================================================
        // FORGOT PASSWORD
        // ============================================================

        [AllowAnonymous]
        [HttpGet("ForgotPassword")]
        public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

        [AllowAnonymous]
        [HttpPost("ForgotPassword")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _context.SystemUsers
                .FirstOrDefaultAsync(u => (u.NationalID == model.Identifier || u.Email == model.Identifier)
                                       && !u.IsDeleted && u.IsActive == true);

            if (user == null)
            {
                ViewBag.Message = "إذا كان الحساب موجوداً، سيتم إرسال التعليمات.";
                return View();
            }

            if (string.IsNullOrEmpty(user.Email))
            {
                ViewBag.Message = "لا يوجد بريد إلكتروني مرتبط بهذا الحساب. يرجى التواصل مع المسؤول.";
                return View();
            }

            string tempPassword = GenerateTemporaryPassword();

            user.PasswordHash = _passwordService.HashPassword(tempPassword);
            user.MustChangePassword = true;
            user.LastUpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(user.ID, "Staff",
                "Password.ForgotReset", "SystemUser", user.ID, null, null,
                HttpContext.Connection.RemoteIpAddress?.ToString());

            string emailBody = $@"
                <h3>إعادة تعيين كلمة المرور</h3>
                <p>كلمة المرور المؤقتة: <strong>{tempPassword}</strong></p>
                <p>يرجى تغييرها بعد تسجيل الدخول.</p>";

            await _emailService.SendAsync(
                user.Email,
                "إعادة تعيين كلمة المرور - UniStay",
                emailBody,
                EmailType.General
            );

            ViewBag.Message = "تم إرسال كلمة مرور مؤقتة.";
            return View();
        }

        // ============================================================
        // CHANGE PASSWORD
        // ============================================================

        [Authorize(AuthenticationSchemes = "StaffCookie,AdminCookie")]
        [HttpGet("ChangePassword")]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        [Authorize(AuthenticationSchemes = "StaffCookie,AdminCookie")]
        [HttpPost("ChangePassword")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userIdClaim = User.FindFirst("UserID")?.Value;

            if (!int.TryParse(userIdClaim, out int userId))
                return RedirectToAction("Login");

            var user = await _context.SystemUsers.FindAsync(userId);
            if (user == null)
                return RedirectToAction("Login");

            if (!_passwordService.VerifyPassword(model.CurrentPassword, user.PasswordHash))
            {
                ModelState.AddModelError("CurrentPassword", "كلمة المرور الحالية غير صحيحة");
                return View(model);
            }

            user.PasswordHash = _passwordService.HashPassword(model.NewPassword);
            user.MustChangePassword = false;
            user.LastUpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(userId, "Staff",
                "Password.Change", "SystemUser", userId, null, null,
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Success"] = "تم تغيير كلمة المرور بنجاح";
            return RedirectToAction("Index", "Home");
        }

        // ============================================================
        // HELPERS
        // ============================================================

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        private static string GenerateTemporaryPassword(int length = 10)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$";
            var bytes = new byte[length];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
        }
    }
}