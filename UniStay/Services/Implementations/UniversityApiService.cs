using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using RestSharp;
using UniStay.Data;
using UniStay.Models;
using UniStay.Services.Interfaces;

namespace UniStay.Services.Implementations
{
    public class UniversityApiService : IUniversityApiService
    {
        private readonly AssuitDbContext db;
        private readonly IAuditService audit;

        public UniversityApiService(
            AssuitDbContext db,
            IAuditService audit)
        {
            this.db = db;
            this.audit = audit;
        }

        public async Task<StudentApiResult> SearchByNationalIDAsync(string nationalId)
        {
            var uni = await db.Universities.FirstAsync();

            var client = new RestClient(uni.APIBaseUrl);

            var req = new RestRequest("students/search", Method.Post)
                .AddHeader("x-api-key", uni.APIKey)
                .AddJsonBody(new { nationalId });

            var res = await client.ExecuteAsync(req);

            var apiData = res.Content ?? "{}";

            var local = await db.Students
                .FirstOrDefaultAsync(s => s.NationalID == nationalId);

            var result =
                Newtonsoft.Json.JsonConvert
                .DeserializeObject<StudentApiResult>(apiData) ?? new();

            result.Found = res.IsSuccessful && result.FullName != null;

            // مقارنة البيانات
            if (local != null && result.Found)
            {
                if (local.FullName != result.FullName)
                    result.Differences["FullName"] =
                        (local.FullName, result.FullName);

                if (local.Faculty != result.Faculty)
                    result.Differences["Faculty"] =
                        (local.Faculty, result.Faculty);

                result.IsMatch = !result.Differences.Any();
            }

            // حفظ السجل
            db.UniversityAPISyncs.Add(new UniversityAPISync
            {
                NationalID = nationalId,
                SyncType = "Student",
                APIData = apiData,
                LocalData = Newtonsoft.Json.JsonConvert.SerializeObject(local),
                IsMatch = result.IsMatch,
                DifferenceDetails =
                    Newtonsoft.Json.JsonConvert.SerializeObject(result.Differences),
                SyncedAt = DateTime.Now
            });

            if (local != null)
            {
                // Only update IsEnrolled if the API explicitly returned the field
                var apiJson = JObject.Parse(apiData);
                if (apiJson["isEnrolled"] != null || apiJson["IsEnrolled"] != null)
                {
                    local.IsEnrolled = result.IsEnrolled;
                }
            }

            await db.SaveChangesAsync();

            await audit.LogAsync(
                0,
                "System",
                "UniversityApi.Verify",
                "Student",
                local?.ID ?? 0,
                null,
                result,
                null);

            return result;
        }

        public async Task<StaffApiResult> SearchStaffByNationalIDAsync(string nationalId)
        {
            var uni = await db.Universities.FirstAsync();

            var client = new RestClient(uni.APIBaseUrl);

            var req = new RestRequest("staff/search", Method.Post)
                .AddHeader("x-api-key", uni.APIKey)
                .AddJsonBody(new { nationalId });

            var res = await client.ExecuteAsync(req);

            return Newtonsoft.Json.JsonConvert
                .DeserializeObject<StaffApiResult>(res.Content ?? "{}") ?? new();
        }

        public async Task<BulkValidationResult> BulkValidateAsync(List<string> ids)
        {
            var result = new BulkValidationResult();

            foreach (var id in ids)
            {
                var r = await SearchByNationalIDAsync(id);

                if (r.Found)
                    result.Success++;
                else
                    result.Failed++;
            }

            return result;
        }
    }
}