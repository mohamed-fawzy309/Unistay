using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UniStay.Data;
using UniStay.Helpers;
using UniStay.Models;
using UniStay.Services.Interfaces;
using UniStay.ViewModels.Account;

namespace UniStay.Controllers
{
    [Route("StudentAccount")]
    public class StudentAccountController : Controller
    {
        private readonly AssuitDbContext _context;
        private readonly IPasswordService _passwordService;
        private readonly IAuditService _auditService;
        private readonly IEmailService _emailService;

        public StudentAccountController(
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

        [HttpGet("Login")]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            var authResult = await HttpContext.AuthenticateAsync("StudentCookie");
            if (authResult.Succeeded)
                return RedirectToAction("Home", "Student");

            ViewData["ReturnUrl"] = returnUrl;
            return View(new StudentLoginViewModel());
        }

        [HttpPost("Login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(StudentLoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            var studentLogin = await _context.StudentLogins
                .Include(s => s.Student)
                .FirstOrDefaultAsync(s => s.Username == model.NationalID && s.IsActive == true);

            if (studentLogin?.Student == null)
            {
                model.ErrorMessage = "الرقم القومي أو كلمة المرور غير صحيحة";
                await _auditService.LogAsync(0, "Student", "Login.Failed", "StudentLogin");
                return View(model);
            }

            if (!_passwordService.VerifyPassword(model.Password, studentLogin.PasswordHash))
            {
                model.ErrorMessage = "الرقم القومي أو كلمة المرور غير صحيحة";
                await _auditService.LogAsync(studentLogin.StudentID, "Student", "Login.Failed", "StudentLogin", studentLogin.StudentID);
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim("StudentID", studentLogin.StudentID.ToString()),
                new Claim("NationalID", studentLogin.Student.NationalID ?? ""),
                new Claim(ClaimTypes.Name, studentLogin.Student.FullName ?? "طالب"),
                new Claim("FullName", studentLogin.Student.FullName ?? "طالب")
            };

            var identity = new ClaimsIdentity(claims, "StudentCookie");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("StudentCookie", principal, new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(12)
            });

            studentLogin.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(studentLogin.StudentID, "Student", "Login.Success", "StudentLogin", studentLogin.StudentID);

            return RedirectToLocal(returnUrl);
        }

        [HttpGet("Register")]
        public async Task<IActionResult> Register()
        {
            var authResult = await HttpContext.AuthenticateAsync("StudentCookie");
            if (authResult.Succeeded)
                return RedirectToAction("Home", "Student");

            return View(new StudentRegisterViewModel());
        }

        [HttpPost("Register")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(StudentRegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var existingNationalId = await _context.Students.AnyAsync(s => s.NationalID == model.NationalID);
            if (existingNationalId)
            {
                model.ErrorMessage = "هذا الرقم القومي مسجل بالفعل. يمكنك تسجيل الدخول مباشرة.";
                return View(model);
            }

            var existingLogin = await _context.StudentLogins.AnyAsync(s => s.Username == model.NationalID);
            if (existingLogin)
            {
                model.ErrorMessage = "يوجد حساب مسبق بهذا الرقم القومي. يمكنك تسجيل الدخول.";
                return View(model);
            }

            var student = new Student
            {
                NationalID = model.NationalID,
                FullName = model.FullName,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow
            };
            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            var studentLogin = new StudentLogin
            {
                StudentID = student.ID,
                Username = model.NationalID,
                PasswordHash = _passwordService.HashPassword(model.Password),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.StudentLogins.Add(studentLogin);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(student.ID, "Student", "Register", "StudentLogin", student.ID);

            TempData["Success"] = "تم إنشاء الحساب بنجاح. يمكنك تسجيل الدخول الآن.";
            return RedirectToAction("Login");
        }

        [HttpGet("Logout")]
        public async Task<IActionResult> Logout()
        {
            var studentIdClaim = User.FindFirstValue("StudentID");
            if (int.TryParse(studentIdClaim, out var sid))
                await _auditService.LogAsync(sid, "Student", "Logout", "StudentLogin", sid);

            await HttpContext.SignOutAsync("StudentCookie");
            return RedirectToAction("Login");
        }

        [HttpGet("ChangePassword")]
        [TypeFilter(typeof(StudentAuthFilter))]
        public IActionResult ChangePassword() => View(new StudentChangePasswordViewModel());

        [HttpPost("ChangePassword")]
        [ValidateAntiForgeryToken]
        [TypeFilter(typeof(StudentAuthFilter))]
        public async Task<IActionResult> ChangePassword(StudentChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var studentIdClaim = User.FindFirstValue("StudentID");
            if (!int.TryParse(studentIdClaim, out var studentId))
                return RedirectToAction("Login");

            var studentLogin = await _context.StudentLogins.FindAsync(studentId);
            if (studentLogin == null) return RedirectToAction("Login");

            if (!_passwordService.VerifyPassword(model.CurrentPassword, studentLogin.PasswordHash))
            {
                ModelState.AddModelError("CurrentPassword", "كلمة المرور الحالية غير صحيحة");
                return View(model);
            }

            studentLogin.PasswordHash = _passwordService.HashPassword(model.NewPassword);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(studentId, "Student", "Password.Changed", "StudentLogin", studentId);

            TempData["Success"] = "تم تغيير كلمة المرور بنجاح";
            return RedirectToAction("Home", "Student");
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Home", "Student");
        }
    }
}
