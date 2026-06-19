using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UniStay.Data;

namespace UniStay.Helpers
{
    public class StudentAuthFilter : IAsyncActionFilter
    {
        private readonly AssuitDbContext _context;

        public StudentAuthFilter(AssuitDbContext context)
        {
            _context = context;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var httpContext = context.HttpContext;
            var authResult = await httpContext.AuthenticateAsync("StudentCookie");
            if (!authResult.Succeeded)
            {
                context.Result = new RedirectResult("/StudentAccount/Login");
                return;
            }

            var principal = authResult.Principal;
            httpContext.User = principal;

            var studentIdClaim = principal.FindFirstValue("StudentID");
            if (string.IsNullOrEmpty(studentIdClaim) || !int.TryParse(studentIdClaim, out var studentId))
            {
                context.Result = new RedirectResult("/StudentAccount/Login");
                return;
            }

            var studentLogin = await _context.StudentLogins
                .Include(s => s.Student)
                .FirstOrDefaultAsync(s => s.StudentID == studentId);

            if (studentLogin == null || studentLogin.IsActive != true)
            {
                context.Result = new RedirectResult("/StudentAccount/Login?expired=true");
                return;
            }

            httpContext.Items["CurrentStudent"] = studentLogin;
            httpContext.Items["StudentName"] = studentLogin.Student?.FullName;

            var resultContext = await next();

            if (resultContext.Controller is Controller controller)
            {
                controller.ViewBag.StudentName ??= studentLogin.Student?.FullName ?? "طالب";

                var hasAllocation = await _context.Allocations
                    .AnyAsync(a => a.StudentID == studentId && a.Status == "Active");
                controller.ViewBag.IsAllocated = hasAllocation;

                var hasAcceptedApp = await _context.Applications
                    .AnyAsync(a => a.StudentID == studentId && a.Status == "Accepted");
                controller.ViewBag.IsAccepted = hasAcceptedApp;

                var hasReservation = await _context.Allocations
                    .AnyAsync(a => a.StudentID == studentId && a.Status == "Reserved");
                controller.ViewBag.HasReservation = hasReservation;
            }
        }
    }

    public record StudentLoginCookie(int StudentID, string NationalID, string FullName);
}
