# UniStay Audit Report — Complete Repair Roadmap
**Date:** 2026-06-19
**Mode:** Audit Only — No changes made

---

## CRITICAL

### C1. StudentAuthFilter queries wrong column — login loop
- **Module:** Student Portal / Authentication
- **Problem:** After student logs in, every navigation to `StudentController` redirects back to login with `?expired=true`. Student portal is unusable.
- **Root cause:** `StudentAuthFilter.cs:41` queries `s.ID == studentId` (StudentLogin PK), but `StudentID` claim stores `StudentLogin.StudentID` (FK to Student). These are different values. Lookup always fails → redirect loop.
- **Files:** `Helpers/StudentAuthFilter.cs:41`
- **Fix:** Change `s.ID == studentId` → `s.StudentID == studentId`

### C2. EmailService not configured — 100% silent failure
- **Module:** Email
- **Problem:** Every email send silently fails or throws. EmailSettings properties don't match appsettings.json, and no `Configure<EmailSettings>()` binding exists.
- **Root cause:** 
  1. `Program.cs` missing `builder.Services.Configure<EmailSettings>(...)` 
  2. `EmailSettings` class uses `Smtp`/`User`/`Pass` but appsettings.json uses `Host`/`Username`/`Password`
- **Files:** `Program.cs`, `Services/Implementations/EmailService.cs`, `appsettings.json`
- **Fix:** Add `Configure<EmailSettings>()` binding OR rename `EmailSettings` properties to match JSON keys

### C3. AuditLog.UserID has no FK relationship
- **Module:** Data / Audit
- **Problem:** `AuditLog.UserID` is non-nullable `int` with no navigation property, no FK relationship configured. Database has no referential integrity on this column.
- **Root cause:** Missing `HasOne<SystemUser>()` or `HasOne<Student>()` configuration in `AssuitDbContext`
- **Files:** `Models/AuditLog.cs`, `Data/AssuitDbContext.cs`
- **Fix:** Add FK relationship to `SystemUser` or make `UserID` nullable if it can reference either staff or student

---

## HIGH

### H1. Missing DB indexes on frequently queried columns
- **Module:** Database / Performance
- **Problem:** Several critical foreign key columns lack indexes, causing table scans on every query.
- **Details:**

| Entity | Column | Impact |
|--------|--------|--------|
| Student | Email | Used for auth/lookup |
| Guardian | StudentID | Always queried by StudentID |
| Document | StudentID | Always queried by StudentID |
| Violation | StudentID | Always queried by StudentID |
| SocialCase | StudentID | Always queried by StudentID |
| SpecialCase | StudentID | Always queried by StudentID |
| StudentValidationLog | StudentID | Always queried by StudentID |
| IDCard | StudentID | Always queried by StudentID |
| CoordinationResult | ApplicationID | Always queried by ApplicationID |
| SpecialCase | ApplicationID | Always queried by ApplicationID |
| Document | ApplicationID | Always queried by ApplicationID |

- **Fix:** Add non-clustered indexes on each listed column in `AssuitDbContext.OnModelCreating`

### H2. Hangfire packages installed but never configured
- **Module:** Build / Dependencies
- **Problem:** `Hangfire.AspNetCore` and `Hangfire.SqlServer` (~4MB) are in `.csproj` but never registered or used anywhere.
- **Files:** `UniStay.csproj:21-22`
- **Fix:** Remove packages OR add `AddHangfire()` / `UseHangfireServer()` / `UseHangfireDashboard()` in `Program.cs`

### H3. Duplicate ViewModels — 6x CityLookup, 2x ApplicationRowViewModel, 2x ReviewDecisionViewModel
- **Module:** Code Quality
- **Problem:** Identical ViewModel classes duplicated across files. CityLookup (int ID + string Name) defined in 6 separate files.
- **Files involved:**
  - `CityLookup` × 6: `ViolationViewModels.cs:57`, `ReportsViewModels.cs:261`, `PhotosViewModels.cs:139`, `MealViewModels.cs:31`, `AttendanceViewModels.cs:78`, `ApplicationsViewModels.cs:163`
  - `ApplicationRowViewModel` × 2: `AdminViewModels.cs:11`, `ApplicationsViewModels.cs:32`
  - `ReviewDecisionViewModel` × 2: `AdminViewModels.cs:27`, `ApplicationsViewModels.cs:140`
  - `CoordinationResultRowViewModel` × 2: `CoordinationViewModels.cs:74`, `OnlineReviewViewModels.cs:84`
  - `BuildingLookup` × 3: `ReportsViewModels.cs:267`, `PhotosViewModels.cs:145`, `AllocationViewModels.cs:148`
- **Fix:** Extract shared ViewModels to a common namespace

### H4. Global namespace classes in ViewModels/University/
- **Module:** Code Quality / Namespacing
- **Problem:** 3 ViewModels with no namespace (global scope):
  - `VerifyStudentRequest` in `VerifyStudentViewModel.cs`
  - `ValidationDetailViewModel` in `ValidationLogViewModel.cs`
  - `SearchStaffRequest`, `BulkValidateViewModel` in `SearchStaffViewModel.cs`
- **Files:** `ViewModels/University/*.cs`
- **Fix:** Add `namespace UniStay.ViewModels.University;`

### H5. PhotosController uses service locator anti-pattern
- **Module:** Code Quality / DI
- **Problem:** `PhotosController.cs:192` resolves `IReportExportService` via `HttpContext.RequestServices.GetRequiredService<>()` instead of constructor injection
- **Files:** `Controllers/PhotosController.cs:192`
- **Fix:** Inject via constructor parameter

### H6. SuperAdminOnlyAttribute is redundant with AdminAuthFilter
- **Module:** Authorization
- **Problem:** `PermissionsController` has `[AdminAuthorize]` (which checks `IsSuperAdmin == true`), then individual actions also have `[SuperAdminOnly]` which re-checks the same thing but redirects to `AccessDenied` instead of `Login`. Inconsistent UX.
- **Files:** `Helpers/Superadminonlyattribute.cs`, `Controllers/PermissionsController.cs`
- **Fix:** Remove `SuperAdminOnlyAttribute`, rely solely on `AdminAuthFilter`

### H7. Missing _ViewImports namespace imports
- **Module:** Views / Maintainability
- **Problem:** `_ViewImports.cshtml` missing:
  - `UniStay.ViewModels.Reports`
  - `UniStay.ViewModels.Staff`
  - `UniStay.ViewModels.Applications`
  - `UniStay.ViewModels.University`
  
  Views work around this with fully qualified names or local `@using` directives.
- **Files:** `Views/_ViewImports.cshtml`
- **Fix:** Add the missing imports

---

## MEDIUM

### M1. 14 controller methods exceeding 50 lines
- **Module:** Controllers — Various
- **Problem:** Overly long action methods violate SRP and hurt testability.
- **Top offenders:**
  - `StudentProfilesController.Status()` — 88 lines
  - `StudentProfilesController.Print()` — 78 lines
  - `StudentProfilesController.Details()` — 76 lines
  - `PermissionsController.AuditLog()` — 90 lines
  - `ApplicationsCenterController.Details()` — 63 lines
  - `PermissionsController.Users()` — 60 lines
  - `StudentController.ReserveRoom()` — 57 lines
  - `AdminController.Index()` — 55 lines
- **Fix:** Extract business logic into service methods

### M2. DeleteBehavior.ClientSetNull on non-nullable FKs
- **Module:** Database
- **Problem:** Most FK relationships use `DeleteBehavior.ClientSetNull` even when the FK property is **non-nullable** (`int`, not `int?`). At the database level, SQL Server cannot `SET NULL` on a non-nullable column. This configuration is misleading.
- **Files:** `Data/AssuitDbContext.cs` — applies to ~50+ relationships
- **Fix:** Change to `DeleteBehavior.Restrict` for non-nullable FKs, keep `ClientSetNull` only for nullable FKs

### M3. Orphan FK properties with no relationship configured
- **Module:** Database / Data Integrity
- **Problem:** These FK properties exist in models but have no navigation property and no FK relationship:
  - `FeeType.CreatedBy` (int?)
  - `Role.CreatedBy` (int?)
  - `ApplicationConfiguration.CreatedBy` (int?)
  - `ApplicationConfiguration.LastUpdatedBy` (int?)
- **Files:** `Models/FeeType.cs`, `Models/Role.cs`, `Models/ApplicationConfiguration.cs`
- **Fix:** Add navigation properties and FK configuration, or remove unused FK columns

### M4. UniversityAPISync has no FK to University or Student
- **Module:** Data / Integrity
- **Problem:** `UniversityAPISync` tracks sync operations by `NationalID` and `StudentCode` but has no FK relationship to either `University` or `Student`. Records become orphans if students are deleted.
- **Files:** `Models/UniversityAPISync.cs`, `Data/AssuitDbContext.cs`
- **Fix:** Add `StudentID` FK or keep as-is (intentional if data must survive student deletion)

### M5. Inline CSS exceeds 730 lines across 3 layout files
- **Module:** Views / Maintainability
- **Problem:** `_AdminLayout.cshtml` (~370 lines), `_LoginLayout.cshtml` (~185 lines), `_StudentLayout.cshtml` (~175 lines) all contain massive inline `<style>` blocks. No CSP headers set.
- **Files:** `Views/Shared/_AdminLayout.cshtml`, `_LoginLayout.cshtml`, `_StudentLayout.cshtml`
- **Fix:** Extract to separate `.css` files in `wwwroot/css/`

### M6. MealBookingService and MealReceivingService duplicate scanning logic
- **Module:** Services / DRY
- **Problem:** Both services have nearly identical `ScanStudentAsync()` methods (lookup by NationalID, StudentCode, or ID) and `ImportFromExcelAsync()` methods.
- **Files:** `Services/Implementations/MealBookingService.cs`, `Services/Implementations/MealReceivingService.cs`
- **Fix:** Extract shared student lookup into a common helper or base service

---

## LOW

### L1. Namespace confusion: `Application` vs `Applications`
- **Module:** Code Quality / Naming
- **Problem:** `UniStay.ViewModels.Application` (singular, 1 class) and `UniStay.ViewModels.Applications` (plural, 14 classes) differ by only one letter. Easy to import the wrong one.
- **Fix:** Rename one to be more distinct (e.g., `ApplicationsCenter`)

### L2. Stale comment in Login.cshtml
- **File:** `Views/Account/Login.cshtml:4`
- **Problem:** Comment says "سننشئه لاحقاً" ("we'll create it later") about `_LoginLayout`, which was already created
- **Fix:** Remove comment

### L3. File name doesn't match class name
- **Files:**
  - `Services/Implementations/AppSettings.cs` contains `class UniversityApiSettings`
  - `ViewModels/University/VerifyStudentViewModel.cs` contains `class VerifyStudentRequest`
- **Fix:** Rename files or classes for consistency

### L4. Classes defined in interface file
- **File:** `Services/Interfaces/IUniversityApiService.cs`
- **Problem:** Defines `StudentApiResult`, `StaffApiResult`, `BulkValidationResult` alongside the interface
- **Fix:** Move DTOs to a separate file or a DTOs folder

### L5. Near-identical view files
- **Files:** `Views/Meal/RamadanSchedule.cshtml`, `Views/Meal/ChristianSchedule.cshtml`
- **Problem:** ~95% identical (140 lines each, differ only in icon and title)
- **Fix:** Consolidate into a single parameterized view

### L6. Inconsistent ViewModel naming suffixes
- **Problem:** Mix of `VM`, `ViewModel`, `Model` suffixes:
  - `SocialCaseListVM` (VM suffix)
  - `LoginViewModel` (ViewModel suffix)
  - `ErrorViewModel` (Model suffix — actually in Models/)
- **Fix:** Standardize on `ViewModel` suffix

### L7. Hard-coded year in footer
- **File:** `Views/Shared/_LoginLayout.cshtml:201`
- **Problem:** `&copy; 2025` — hard-coded year
- **Fix:** Use `@DateTime.Now.Year`

### L8. Bootstrap version mismatch
- **File:** `Views/Shared/_AdminLayout.cshtml`
- **Problem:** CSS loaded from local `lib/bootstrap/` but JS loaded from CDN `5.3.3`. If offline, admin JS breaks. If local version differs from CDN, behavior may be inconsistent.
- **Fix:** Use consistent source (prefer local for both)

### L9. DataScope and UserDataScope not `partial class`
- **Files:** `Models/DataScope.cs`, `Models/UserDataScope.cs`
- **Problem:** All other entity models are `partial class`; these two are not. Also use inconsistent initialization (`= string.Empty` instead of `= null!;`) and non-file-scoped namespace.
- **Fix:** Make `partial class` and standardize initialization

### L10. Home controller has unused Privacy action
- **File:** `Controllers/HomeController.cs` (as reported by first exploration agent)
- **Problem:** `Privacy` action exists but may not have a matching view
- **Fix:** Verify and add view or remove action

---

## SUMMARY TABLE

| Severity | Count | Key Issues |
|----------|-------|------------|
| **CRITICAL** | 3 | StudentAuthFilter bug, EmailService config, AuditLog FK |
| **HIGH** | 7 | Missing indexes, Hangfire dead weight, duplicate ViewModels, global namespaces, service locator, redundant auth filter, missing imports |
| **MEDIUM** | 6 | Long methods, misleading cascade delete, orphan FKs, APISync FK, inline CSS, duplicated meal services |
| **LOW** | 10 | Naming, stale comments, file names, DTOs in interface, duplicate views, naming suffixes, hard-coded year, Bootstrap mismatch, non-partial classes, unused action |

**Total issues found: 26**

---

## ROUTING VERIFICATION (Phase 1-2)

Since the app cannot be run (shell environment unresponsive), routing was verified by code analysis:

| Route | Controller/Action | Expected | Status |
|-------|-------------------|----------|--------|
| `/` | HomeController.Index | 200 | ✅ Code path correct |
| `/Account/Login` | AccountController.Login GET | 200 | ✅ AllowAnonymous |
| `/Account/Login` (POST) | AccountController.Login POST | 302→Admin or Staff | ✅ Validates, signs in, redirects |
| `/Account/Logout` | AccountController.Logout | 302 → Login | ✅ Authorize StaffCookie,AdminCookie |
| `/Account/AccessDenied` | AccountController.AccessDenied | 200 | ✅ AllowAnonymous |
| `/Admin` | AdminController.Index | 200 (after auth) | ✅ Authorize AdminCookie |
| `/Staff` | StaffController.Index | 200 (after auth) | ✅ Authorize StaffCookie |
| `/StudentAccount/Login` | StudentAccountController.Login | 200 | ✅ AllowAnonymous |
| `/Home/Error` | HomeController.Error | 200 | ✅ AllowAnonymous (added in Phase 4) |
| `/Student/Home` | StudentController.Home | **BROKEN** | 🔴 StudentAuthFilter bug (C1) |

**Login flow audit (code review):**
- `Login` (POST) → finds `SystemUser` by `NationalID` → verifies password → creates claims → signs in as AdminCookie/StaffCookie → redirects to `/Admin` or `/Staff`
- Filter chain for `/Admin`: `app.UseAuthentication()` → `app.UseAuthorization()` → `[Authorize(AdminCookie)]` → `AdminController.Index` → `[RequirePermission("Dashboard.View","CanView")]` → `PermissionFilter.HasPermission` → dashboard renders
- **Everything in the login flow is intact. No Phase 3-10 changes affect it.**

---

## REPAIR ROADMAP

### Must-fix before anything else:
1. **C1** — `StudentAuthFilter.cs:41` (1-line fix)
2. **C2** — `EmailSettings` configuration (config binding or rename properties)
3. **C3** — `AuditLog.UserID` FK (navigation property + fluent config)

### Recommended next:
4. **H1** — Add missing DB indexes
5. **H5** — Fix service locator in PhotosController
6. **H2** — Remove or configure Hangfire
7. **H3** — Deduplicate ViewModels
8. **H4** — Fix global namespace classes
9. **H6** — Remove redundant SuperAdminOnlyAttribute

### Refactoring/cleanup:
10. **H7** — Add missing _ViewImports
11. **M1-M6** — Refactor long methods, deduplicate services, fix cascade delete config
12. **L1-L10** — Low-priority cleanup
