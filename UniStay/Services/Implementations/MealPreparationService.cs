using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Services.Interfaces;
using UniStay.ViewModels.Meal;

namespace UniStay.Services.Implementations;

public class MealPreparationService(AssuitDbContext db, IReportExportService export) : IMealPreparationService
{
    public async Task<MealPreparationIndexViewModel> GetPreparationSummaryAsync(DateOnly? date, int? cityId)
    {
        var today = date ?? DateOnly.FromDateTime(DateTime.Today);
        var cities = await db.DormitoryCities.Where(c => c.IsActive)
            .Select(c => new CityLookup { ID = c.ID, Name = c.Name }).ToListAsync();

        var mealsQuery = db.Meals.Where(m => m.MealDate == today && m.IsBooked == true && m.IsActive == true);

        if (cityId.HasValue)
            mealsQuery = mealsQuery.Where(m => m.DormitoryCityID == cityId.Value);

        var totalCount = await mealsQuery.CountAsync();

        var cityBreakdowns = await (from m in mealsQuery
                                    join dc in db.DormitoryCities on m.DormitoryCityID equals dc.ID
                                    group m by new { dc.ID, dc.Name } into g
                                    select new CityBreakdownViewModel
                                    {
                                        CityId = g.Key.ID,
                                        CityName = g.Key.Name,
                                        BreakfastCount = g.Count(x => x.MealType == "Breakfast"),
                                        LunchCount = g.Count(x => x.MealType == "Lunch"),
                                        DinnerCount = g.Count(x => x.MealType == "Dinner"),
                                        TotalCount = g.Count()
                                    }).ToListAsync();

        if (cityId.HasValue)
        {
            var breakdownData = await (from m in mealsQuery
                                       join a in db.Allocations on m.StudentID equals a.StudentID
                                       join cr in db.CityRooms on a.CityRoomID equals cr.ID
                                       join cbld in db.CityBuildings on cr.CityBuildingID equals cbld.ID
                                       where a.Status == "Active"
                                       group m by new { cbld.ID, BuildingName = cbld.BuildingName, cr.RoomNumber } into g
                                       select new
                                       {
                                           g.Key.ID,
                                           g.Key.BuildingName,
                                           g.Key.RoomNumber,
                                           BreakfastCount = g.Count(x => x.MealType == "Breakfast"),
                                           LunchCount = g.Count(x => x.MealType == "Lunch"),
                                           DinnerCount = g.Count(x => x.MealType == "Dinner"),
                                           TotalCount = g.Count()
                                       }).ToListAsync();

            var buildingGroups = breakdownData.GroupBy(x => new { x.ID, x.BuildingName });

            foreach (var bg in buildingGroups)
            {
                var rooms = bg.Select(r => new RoomBreakdownViewModel
                {
                    RoomNumber = r.RoomNumber,
                    BreakfastCount = r.BreakfastCount,
                    LunchCount = r.LunchCount,
                    DinnerCount = r.DinnerCount,
                    TotalCount = r.TotalCount
                }).ToList();

                var bld = new BuildingBreakdownViewModel
                {
                    BuildingId = bg.Key.ID,
                    BuildingName = bg.Key.BuildingName,
                    BreakfastCount = rooms.Sum(r => r.BreakfastCount),
                    LunchCount = rooms.Sum(r => r.LunchCount),
                    DinnerCount = rooms.Sum(r => r.DinnerCount),
                    TotalCount = rooms.Sum(r => r.TotalCount),
                    Rooms = rooms
                };

                var cb = cityBreakdowns.FirstOrDefault(c => c.CityId == cityId.Value);
                if (cb != null)
                    cb.Buildings.Add(bld);
            }
        }

        return new MealPreparationIndexViewModel
        {
            SelectedDate = today,
            CityId = cityId,
            TotalCount = totalCount,
            Cities = cities,
            CityBreakdowns = cityBreakdowns
        };
    }

    public async Task<DailyPreparationSheetViewModel> GetDailySheetAsync(DateOnly date, int? cityId)
    {
        var summary = await GetPreparationSummaryAsync(date, cityId);
        var cityName = cityId.HasValue
            ? await db.DormitoryCities.Where(c => c.ID == cityId.Value).Select(c => c.Name).FirstOrDefaultAsync()
            : null;

        return new DailyPreparationSheetViewModel
        {
            PrepDate = date,
            CityName = cityName ?? "جميع المدن",
            BreakfastCount = summary.BreakfastCount,
            LunchCount = summary.LunchCount,
            DinnerCount = summary.DinnerCount,
            TotalCount = summary.TotalCount,
            CityBreakdowns = summary.CityBreakdowns
        };
    }

    public async Task<KitchenReportViewModel> GetKitchenReportAsync(DateOnly date, int? cityId)
    {
        var mealsQuery = db.Meals.Where(m => m.MealDate == date);
        if (cityId.HasValue)
            mealsQuery = mealsQuery.Where(m => m.DormitoryCityID == cityId.Value);

        var totalPrepared = await mealsQuery.CountAsync(m => m.IsBooked == true && m.IsActive == true);
        var totalConsumed = await mealsQuery.CountAsync(m => m.IsConsumed == true);
        var totalRemaining = totalPrepared - totalConsumed;
        var totalCost = totalPrepared * 12m;

        var mealTypeSummaries = await mealsQuery
            .Where(m => m.IsBooked == true && m.IsActive == true)
            .GroupBy(m => m.MealType)
            .Select(g => new KitchenMealTypeSummaryViewModel
            {
                MealType = g.Key,
                PreparedCount = g.Count(),
                ConsumedCount = g.Count(m => m.IsConsumed == true),
                RemainingCount = g.Count() - g.Count(m => m.IsConsumed == true),
                Cost = g.Count() * 12m
            }).ToListAsync();

        return new KitchenReportViewModel
        {
            ReportDate = date,
            TotalMealsPrepared = totalPrepared,
            TotalConsumed = totalConsumed,
            TotalRemaining = totalRemaining,
            TotalCost = totalCost,
            MealTypeSummaries = mealTypeSummaries
        };
    }

    public async Task<DistributionReportViewModel> GetDistributionReportAsync(DateOnly date, int? cityId)
    {
        var mealsQuery = db.Meals.Where(m => m.MealDate == date && m.IsBooked == true && m.IsActive == true);
        if (cityId.HasValue)
            mealsQuery = mealsQuery.Where(m => m.DormitoryCityID == cityId.Value);

        var totalPrepared = await mealsQuery.CountAsync();
        var totalDistributed = await mealsQuery.CountAsync(m => m.IsConsumed == true);
        var totalPending = totalPrepared - totalDistributed;

        var citySummaries = await (from m in mealsQuery
                                   join dc in db.DormitoryCities on m.DormitoryCityID equals dc.ID
                                   group m by dc.Name into g
                                   select new DistributionCitySummaryViewModel
                                   {
                                       CityName = g.Key,
                                       PreparedCount = g.Count(),
                                       DistributedCount = g.Count(x => x.IsConsumed == true),
                                       PendingCount = g.Count() - g.Count(x => x.IsConsumed == true),
                                       BuildingCount = 0
                                   }).ToListAsync();

        return new DistributionReportViewModel
        {
            ReportDate = date,
            TotalPrepared = totalPrepared,
            TotalDistributed = totalDistributed,
            TotalPending = totalPending,
            CitySummaries = citySummaries
        };
    }

    public async Task<byte[]> ExportDailySheetExcelAsync(DateOnly date, int? cityId)
    {
        var sheet = await GetDailySheetAsync(date, cityId);
        var cityName = cityId.HasValue
            ? await db.DormitoryCities.Where(c => c.ID == cityId.Value).Select(c => c.Name).FirstOrDefaultAsync() ?? ""
            : "جميع المدن";

        var columns = new[] { "المدينة", "المبنى", "الغرفة", "فطور", "غداء", "عشاء", "الإجمالي" };
        var rows = new List<string[]>();

        foreach (var city in sheet.CityBreakdowns)
        {
            if (city.Buildings.Any())
            {
                foreach (var bld in city.Buildings)
                {
                    if (bld.Rooms.Any())
                    {
                        foreach (var rm in bld.Rooms)
                            rows.Add(new[] { city.CityName, bld.BuildingName, rm.RoomNumber, rm.BreakfastCount.ToString(), rm.LunchCount.ToString(), rm.DinnerCount.ToString(), rm.TotalCount.ToString() });
                    }
                    else
                    {
                        rows.Add(new[] { city.CityName, bld.BuildingName, "-", bld.BreakfastCount.ToString(), bld.LunchCount.ToString(), bld.DinnerCount.ToString(), bld.TotalCount.ToString() });
                    }
                }
            }
            else
            {
                rows.Add(new[] { city.CityName, "-", "-", city.BreakfastCount.ToString(), city.LunchCount.ToString(), city.DinnerCount.ToString(), city.TotalCount.ToString() });
            }
        }

        return export.ExportToExcel($"ورقة تجهيز {date:yyyy-MM-dd} - {cityName}", columns, rows, r => r);
    }

    public async Task<byte[]> ExportDailySheetPdfAsync(DateOnly date, int? cityId)
    {
        var sheet = await GetDailySheetAsync(date, cityId);
        var cityName = cityId.HasValue
            ? await db.DormitoryCities.Where(c => c.ID == cityId.Value).Select(c => c.Name).FirstOrDefaultAsync() ?? ""
            : "جميع المدن";

        var columns = new[] { "المدينة", "الفطور", "الغداء", "العشاء", "الإجمالي" };
        var pdfRows = sheet.CityBreakdowns.Select(c => new[] { c.CityName, c.BreakfastCount.ToString(), c.LunchCount.ToString(), c.DinnerCount.ToString(), c.TotalCount.ToString() }).ToArray();

        return export.ExportToPdf($"ورقة تجهيز الوجبات - {date:yyyy-MM-dd} - {cityName}", columns, pdfRows);
    }
}
