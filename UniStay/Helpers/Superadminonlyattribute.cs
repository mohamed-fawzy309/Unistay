using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace UniStay.Helpers
{
    /// <summary>
    /// فلتر: يتحقق من أن المستخدم الحالي هو SuperAdmin
    /// يُطبَّق على PermissionsController بالكامل
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class SuperAdminOnlyAttribute : Attribute, IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            // تحقق 1: هل المستخدم مسجَّل دخول؟
            if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            // تحقق 2: هل هو موظف (Staff)?
            var userType = context.HttpContext.User.FindFirst("UserType")?.Value;
            if (userType != "Staff")
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            // تحقق 3: هل IsSuperAdmin = true في Claims?
            var isSuperAdmin = context.HttpContext.User.FindFirst("IsSuperAdmin")?.Value;
            if (isSuperAdmin != "true")
            {
                // يُعيد لصفحة "غير مصرّح" بدلاً من Login
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
                return;
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}