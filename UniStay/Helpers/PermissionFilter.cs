using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using UniStay.Services.Interfaces;

namespace UniStay.Helpers
{
    public class PermissionFilter : IAsyncActionFilter
    {
        private readonly IPermissionService _permissionService;
        private readonly string _permissionKey;
        private readonly string _requiredAction;

        public PermissionFilter(IPermissionService permissionService, string permissionKey, string requiredAction = "CanView")
        {
            _permissionService = permissionService;
            _permissionKey = permissionKey;
            _requiredAction = requiredAction;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var userIdClaim = context.HttpContext.User.FindFirst("UserID")?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
            {
                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl = context.HttpContext.Request.Path });
                return;
            }

            bool hasPermission = _permissionService.HasPermission(userId, _permissionKey, _requiredAction);

            if (!hasPermission)
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
                return;
            }

            await next();
        }
    }
}