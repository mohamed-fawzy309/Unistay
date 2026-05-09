using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using UniStay.Data;

namespace UniStay.Helpers;

public class AdminAuthFilter : IAsyncActionFilter
{
    private readonly AssuitDbContext _db;
    public AdminAuthFilter(AssuitDbContext db) => _db = db;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var path = httpContext.Request.Path.Value?.ToLower() ?? "";

        if (path.Contains("/account/login")) { await next(); return; }

        var authResult = await httpContext.AuthenticateAsync("AdminCookie");
        if (!authResult.Succeeded)
        {
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl = httpContext.Request.Path });
            return;
        }

        var user = authResult.Principal;
        if (!user.HasClaim("UserType", "Admin"))
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

        var sysUser = await _db.SystemUsers.AsNoTracking()
            .FirstOrDefaultAsync(u => u.ID == userId);

        if (sysUser == null || sysUser.IsActive != true || sysUser.IsDeleted == true || sysUser.IsSuperAdmin != true)
        {
            await context.HttpContext.SignOutAsync("AdminCookie");
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
public class AdminAuthorizeAttribute : TypeFilterAttribute
{
    public AdminAuthorizeAttribute() : base(typeof(AdminAuthFilter)) { }
}
