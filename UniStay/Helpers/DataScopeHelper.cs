// Helpers/DataScopeHelper.cs
using Microsoft.EntityFrameworkCore;
using UniStay.Models;
using UniStay.Services.Interfaces;

namespace UniStay.Helpers
{
    public static class DataScopeHelper
    {
        public static IQueryable<T> ApplyDataScope<T>(this IQueryable<T> query, int userId, IPermissionService permissionService)
            where T : class, IHasDormitoryCity
        {
            if (permissionService.IsInDataScope(userId, null)) // All scope
                return query;

            // يمكن توسيع هذا لاحقاً حسب نوع الكيان
            return query; // placeholder — سيتم توسيعه حسب الحاجة في كل Controller
        }

        public static bool CanAccessCity(this IPermissionService permissionService, int userId, int cityId)
        {
            return permissionService.IsInDataScope(userId, cityId: cityId);
        }

        public static bool CanAccessBuilding(this IPermissionService permissionService, int userId, int buildingId)
        {
            return permissionService.IsInDataScope(userId, buildingId: buildingId);
        }
    }

    // Interface يجب إضافته للكيانات التي لها DormitoryCityID
    public interface IHasDormitoryCity
    {
        int DormitoryCityID { get; }
    }
}