using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using UniStay.Data;

namespace UniStay.Helpers;

public class StaffAuthFilter : IAsyncActionFilter
{
    private readonly AssuitDbContext _db;
    public StaffAuthFilter(AssuitDbContext db) => _db = db;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var path = httpContext.Request.Path.Value?.ToLower() ?? "";

        if (path.Contains("/account/login")) { await next(); return; }

        var authResult = await httpContext.AuthenticateAsync("StaffCookie");
        if (!authResult.Succeeded)
        {
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl = httpContext.Request.Path });
            return;
        }

        var user = authResult.Principal;
        if (!user.HasClaim("UserType", "Staff"))
        {
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl = httpContext.Request.Path });
            return;
        }

        httpContext.User = user;

        var idClaim = user.FindFirst("UserID")?.Value;
        if (!int.TryParse(idClaim, out int userId))
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }

        var sysUser = await _db.SystemUsers.AsNoTracking().FirstOrDefaultAsync(u => u.ID == userId);

        if (sysUser == null || sysUser.IsActive != true || sysUser.IsDeleted == true)
        {
            await context.HttpContext.SignOutAsync("StaffCookie");
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }

        if (sysUser.MustChangePassword == true && !path.Contains("changepassword"))
        {
            context.Result = new RedirectToActionResult("ChangePassword", "Account", null);
            return;
        }

        await next();
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class StaffAuthorizeAttribute : TypeFilterAttribute
{
    public StaffAuthorizeAttribute() : base(typeof(StaffAuthFilter)) { }
}