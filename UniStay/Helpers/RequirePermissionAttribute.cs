// Helpers/RequirePermissionAttribute.cs
using Microsoft.AspNetCore.Mvc;

namespace UniStay.Helpers
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class RequirePermissionAttribute : TypeFilterAttribute
    {
        public RequirePermissionAttribute(string permissionKey, string action = "CanView")
            : base(typeof(PermissionFilter))
        {
            Arguments = new object[] { permissionKey, action };
        }
    }
}