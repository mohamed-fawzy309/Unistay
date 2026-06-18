using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using UniStay.Data;
using UniStay.Helpers;
using UniStay.Services.Implementations;
using UniStay.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// ===== Database =====
builder.Services.AddDbContext<AssuitDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null
        )
    ));

// ===== MVC =====
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddHttpContextAccessor();

// ===== AntiForgery =====
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = ".UniStay.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
});

// ===== HttpClient =====
builder.Services.AddScoped<IUniversityApiService, UniversityApiService>();

// ===== QuestPDF License =====
QuestPDF.Settings.License = LicenseType.Community;

// ===== Application Services =====
builder.Services.AddScoped<ICoordinationService, CoordinationService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IMealService, MealService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IReportExportService, ReportExportService>();
builder.Services.AddScoped<IPhotoService, PhotoService>();
builder.Services.AddScoped<ICardPrintService, CardPrintService>();
builder.Services.AddScoped<IMealRestrictionService, MealRestrictionService>();
builder.Services.AddScoped<IMealReceivingService, MealReceivingService>();
builder.Services.AddScoped<IMealBookingService, MealBookingService>();
builder.Services.AddScoped<IMealPreparationService, MealPreparationService>();

// ===== Settings =====
builder.Services.Configure<UniversityApiSettings>(
    builder.Configuration.GetSection("UniversityApi"));

// ===== Filters =====
builder.Services.AddScoped<StaffAuthFilter>();
builder.Services.AddScoped<AdminAuthFilter>();
builder.Services.AddScoped<StudentAuthFilter>();

// ===== Cookie Authentication =====
builder.Services.AddAuthentication()
.AddCookie("AdminCookie", options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/Login";
    options.Cookie.Name = ".UniStay.Admin";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;

    options.Cookie.MaxAge = null;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
})
.AddCookie("StaffCookie", options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/Login";
    options.Cookie.Name = ".UniStay.Staff";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;

    options.Cookie.MaxAge = null;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
})
.AddCookie("StudentCookie", options =>
{
    options.LoginPath = "/StudentAccount/Login";
    options.LogoutPath = "/StudentAccount/Logout";
    options.AccessDeniedPath = "/StudentAccount/Login";
    options.Cookie.Name = ".UniStay.Student";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;

    options.Cookie.MaxAge = null;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
});
builder.Services.AddAuthorization();

// ===== Build =====
var app = builder.Build();

// ===== Pipeline =====
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();