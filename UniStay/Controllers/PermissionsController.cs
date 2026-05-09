using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Helpers;
using UniStay.Models;
using UniStay.Services.Interfaces;
using UniStay.ViewModels.Permissions;

namespace UniStay.Controllers
{
    [Route("Permissions")]
    [AdminAuthorize]
    public class PermissionsController : Controller
    {
        private readonly AssuitDbContext _db;
        private readonly IPasswordService _passwordService;
        private readonly IEmailService _emailService;
        private readonly IAuditService _auditService;

        public PermissionsController(
            AssuitDbContext db,
            IPasswordService passwordService,
            IEmailService emailService,
            IAuditService auditService)
        {
            _db = db;
            _passwordService = passwordService;
            _emailService = emailService;
            _auditService = auditService;
        }

        // ──────────────────────────────────────────
        // Guard: SuperAdmin فقط
        // ──────────────────────────────────────────
        private IActionResult? CheckSuperAdmin()
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login", "Account");

            if (User.FindFirst("UserType")?.Value != "Admin")
                return RedirectToAction("Login", "Account");

            if (User.FindFirst("IsSuperAdmin")?.Value != "true")
                return RedirectToAction("AccessDenied", "Account");

            return null;
        }

        private int CurrentUserID()
        {
            var c = User.FindFirst("UserID")?.Value;
            return int.TryParse(c, out var id) ? id : 0;
        }

        // ══════════════════════════════════════════════════════════════
        // 1. Users
        // ══════════════════════════════════════════════════════════════

        [HttpGet("Users")]
        public async Task<IActionResult> Users()
        {
            var guard = CheckSuperAdmin();
            if (guard != null) return guard;

            var rawUsers = await _db.SystemUsers
                .Where(u => !u.IsDeleted)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            var ids = rawUsers.Select(u => u.ID).ToList();

            // أدوار المدن
            var cityRoles = await _db.CityStaffs
                .Where(cs => ids.Contains(cs.SystemUserID))
                .Join(_db.DormitoryCities,
                      cs => cs.DormitoryCityID,
                      dc => dc.ID,
                      (cs, dc) => new { cs.SystemUserID, dc.Name, cs.RoleInCity })
                .ToListAsync();

            var cityRoleDict = cityRoles
                .GroupBy(x => x.SystemUserID)
                .ToDictionary(g => g.Key, g => g.ToList());

            // عدد الصلاحيات
            var permCounts = await _db.UserPermissions
                .Where(up => ids.Contains(up.SystemUserID))
                .GroupBy(up => up.SystemUserID)
                .Select(g => new { UserID = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserID, x => x.Count);

            var users = rawUsers.Select(u => new UserRowViewModel
            {
                ID = u.ID,
                Name = u.Name,
                Email = u.Email,
                Phone = u.Phone,
                NationalID = u.NationalID,
                IsSuperAdmin = u.IsSuperAdmin,
                IsActive = u.IsActive,
                MustChangePassword = u.MustChangePassword,
                LastLoginAt = u.LastLoginAt,
                CreatedAt = u.CreatedAt ?? DateTime.Now,
                PermissionsCount = permCounts.GetValueOrDefault(u.ID, 0),
                CityRoles = cityRoleDict.TryGetValue(u.ID, out var roles)
                    ? roles.Select(r => $"{r.Name} — {MapRole(r.RoleInCity)}").ToList()
                    : new()
            }).ToList();

            var vm = new UserListViewModel
            {
                Users = users,
                TotalCount = users.Count,
                ActiveCount = users.Count(u => u.IsActive),
                SuperAdminCount = users.Count(u => u.IsSuperAdmin)
            };

            ViewBag.CurrentUserID = CurrentUserID();
            return View(vm);
        }

        [HttpPost("Users")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Users(CreateUserViewModel model)
        {
            var guard = CheckSuperAdmin();
            if (guard != null) return guard;

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "يرجى تصحيح الأخطاء";
                return RedirectToAction(nameof(Users));
            }

            if (await _db.SystemUsers.AnyAsync(u => u.Email == model.Email && !u.IsDeleted))
            {
                TempData["Error"] = "البريد الإلكتروني مستخدم من قبل";
                return RedirectToAction(nameof(Users));
            }

            var tempPw = GenerateTempPassword();

            var user = new SystemUser
            {
                Name = model.Name,
                Email = model.Email,
                Phone = model.Phone,
                NationalID = model.NationalID,
                IsSuperAdmin = model.IsSuperAdmin,
                IsActive = true,
                IsDeleted = false,
                MustChangePassword = true,
                PasswordHash = _passwordService.HashPassword(tempPw),
                CreatedAt = DateTime.Now,
                CreatedBy = CurrentUserID()
            };

            _db.SystemUsers.Add(user);
            await _db.SaveChangesAsync();

            await _emailService.SendAsync(
                model.Email,
                "بيانات دخولك على نظام UniStay",
                BuildWelcomeEmail(model.Name, user.NationalID ?? model.Email, tempPw),
                EmailType.General
            );

            await _auditService.LogAsync(
                CurrentUserID(), "Staff", "User.Create",
                "SystemUser", user.ID,
                null, new { user.Name, user.Email, model.IsSuperAdmin });

            TempData["Success"] = $"تم إنشاء حساب {model.Name} — تم إرسال بيانات الدخول على {model.Email}";
            return RedirectToAction(nameof(Users));
        }

        // AJAX — تفعيل/تعطيل
        [HttpPost("ToggleActive")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var guard = CheckSuperAdmin();
            if (guard != null) return Json(new { success = false, message = "غير مصرح" });

            var user = await _db.SystemUsers.FindAsync(id);
            if (user == null || user.IsDeleted)
                return Json(new { success = false, message = "المستخدم غير موجود" });

            if (user.IsSuperAdmin && user.ID == CurrentUserID())
                return Json(new { success = false, message = "لا يمكنك تعطيل حسابك" });

            user.IsActive = !user.IsActive;
            user.LastUpdatedAt = DateTime.Now;
            user.LastUpdatedBy = CurrentUserID();
            await _db.SaveChangesAsync();

            await _auditService.LogAsync(
                CurrentUserID(), "Staff",
                user.IsActive ? "User.Activate" : "User.Deactivate",
                "SystemUser", id);

            return Json(new
            {
                success = true,
                isActive = user.IsActive,
                message = user.IsActive ? "تم تفعيل الحساب" : "تم تعطيل الحساب"
            });
        }

        // AJAX — إعادة تعيين كلمة المرور
        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword(int id)
        {
            var guard = CheckSuperAdmin();
            if (guard != null) return Json(new { success = false, message = "غير مصرح" });

            var user = await _db.SystemUsers.FindAsync(id);
            if (user == null || user.IsDeleted || string.IsNullOrEmpty(user.Email))
                return Json(new { success = false, message = "المستخدم غير موجود أو لا يوجد إيميل" });

            var newPw = GenerateTempPassword();
            user.PasswordHash = _passwordService.HashPassword(newPw);
            user.MustChangePassword = true;
            user.LastUpdatedAt = DateTime.Now;
            user.LastUpdatedBy = CurrentUserID();
            await _db.SaveChangesAsync();

            await _emailService.SendAsync(
                user.Email,
                "إعادة تعيين كلمة المرور — UniStay",
                BuildResetEmail(user.Name, newPw),
                EmailType.General
            );

            await _auditService.LogAsync(
                CurrentUserID(), "Staff", "User.ResetPassword",
                "SystemUser", id);

            return Json(new { success = true, message = "تم إعادة تعيين كلمة المرور وإرسالها" });
        }

        // AJAX — حذف ناعم
        [HttpPost("DeleteUser")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var guard = CheckSuperAdmin();
            if (guard != null) return Json(new { success = false, message = "غير مصرح" });

            var user = await _db.SystemUsers.FindAsync(id);
            if (user == null)
                return Json(new { success = false, message = "المستخدم غير موجود" });

            if (user.ID == CurrentUserID())
                return Json(new { success = false, message = "لا يمكنك حذف حسابك الخاص" });

            user.IsDeleted = true;
            user.IsActive = false;
            user.LastUpdatedAt = DateTime.Now;
            user.LastUpdatedBy = CurrentUserID();
            await _db.SaveChangesAsync();

            await _auditService.LogAsync(
                CurrentUserID(), "Staff", "User.Delete",
                "SystemUser", id,
                new { user.Name, user.Email }, null);

            return Json(new { success = true, message = "تم حذف المستخدم" });
        }

        // ══════════════════════════════════════════════════════════════
        // 2. Assign Permissions
        // ══════════════════════════════════════════════════════════════

        [HttpGet("Assign")]
        public async Task<IActionResult> Assign(int userId)
        {
            var guard = CheckSuperAdmin();
            if (guard != null) return guard;

            var user = await _db.SystemUsers
                .FirstOrDefaultAsync(u => u.ID == userId && !u.IsDeleted);

            if (user == null)
            {
                TempData["Error"] = "المستخدم غير موجود";
                return RedirectToAction(nameof(Users));
            }

            // كل مجموعات الصلاحيات
            var allGroups = await _db.PermissionGroups
                .Include(g => g.Permissions)
                .OrderBy(g => g.GroupName)
                .ToListAsync();

            // الصلاحيات الممنوحة حالياً
            var currentPerms = await _db.UserPermissions
                .Where(up => up.SystemUserID == userId)
                .Include(up => up.Permission)
                .ToListAsync();

            var permDict = currentPerms.ToDictionary(p => p.PermissionID);

            var groups = allGroups.Select(g => new PermissionGroupViewModel
            {
                GroupID = g.ID,
                GroupName = g.GroupName,
                Description = g.Description,
                Permissions = g.Permissions.Select(p => new PermissionItemViewModel
                {
                    PermissionID = p.ID,
                    PermissionKey = p.PermissionKey,
                    DisplayName = p.DisplayName,
                    Category = p.Category,
                    CanView = permDict.TryGetValue(p.ID, out var up) && up.CanView == true,
                    CanCreate = permDict.TryGetValue(p.ID, out up) && up.CanCreate == true,
                    CanEdit = permDict.TryGetValue(p.ID, out up) && up.CanEdit == true,
                    CanDelete = permDict.TryGetValue(p.ID, out up) && up.CanDelete == true,
                }).ToList()
            }).ToList();

            // أدوار المدن الحالية
            var cityRoles = await _db.CityStaffs
                .Where(cs => cs.SystemUserID == userId)
                .Join(_db.DormitoryCities,
                      cs => cs.DormitoryCityID,
                      dc => dc.ID,
                      (cs, dc) => new CityRoleRowViewModel
                      {
                          CityStaffID = cs.ID,
                          CityName = dc.Name,
                          RoleInCity = cs.RoleInCity,
                          RoleDisplayName = MapRole(cs.RoleInCity),
                          IsPrimary = cs.IsPrimary,
                          AssignedAt = cs.AssignedAt ?? DateTime.Now,
                      })
                .ToListAsync();

            // قائمة المدن
            var cities = await _db.DormitoryCities
                .Where(c => c.IsActive == true && c.IsDeleted != true)
                .Select(c => new SelectItem(c.ID.ToString(), c.Name))
                .ToListAsync();

            var vm = new AssignPermissionsViewModel
            {
                UserID = userId,
                UserName = user.Name,
                UserEmail = user.Email,
                IsSuperAdmin = user.IsSuperAdmin,
                Groups = groups,
                CurrentPermissions = currentPerms.Select(up => new UserPermissionDto
                {
                    PermissionID = up.PermissionID,
                    PermissionKey = up.Permission?.PermissionKey ?? "",
                    CanView = up.CanView == true,
                    CanCreate = up.CanCreate == true,
                    CanEdit = up.CanEdit == true,
                    CanDelete = up.CanDelete == true,
                    GrantedAt = up.GrantedAt ?? DateTime.Now,
                }).ToList()
            };

            ViewBag.CityRoles = cityRoles;
            ViewBag.AvailableCities = cities;
            return View(vm);
        }

        // حفظ الصلاحيات — AJAX/JSON body
        [HttpPost("Assign")]
        public async Task<IActionResult> Assign([FromBody] SavePermissionsRequest request)
        {
            var guard = CheckSuperAdmin();
            if (guard != null) return Json(new { success = false });

            var existing = await _db.UserPermissions
                .Where(up => up.SystemUserID == request.UserID)
                .ToListAsync();

            var existingDict = existing.ToDictionary(up => up.PermissionID);
            var now = DateTime.Now;
            var grantedBy = CurrentUserID();

            var oldSnap = existing.Select(p => new {
                p.PermissionID,
                p.CanView,
                p.CanCreate,
                p.CanEdit,
                p.CanDelete
            }).ToList();

            foreach (var item in request.Permissions)
            {
                if (existingDict.TryGetValue(item.PermissionID, out var ex))
                {
                    ex.CanView = item.CanView;
                    ex.CanCreate = item.CanCreate;
                    ex.CanEdit = item.CanEdit;
                    ex.CanDelete = item.CanDelete;
                    ex.GrantedBy = grantedBy;
                    ex.GrantedAt = now;
                }
                else if (item.CanView || item.CanCreate || item.CanEdit || item.CanDelete)
                {
                    _db.UserPermissions.Add(new UserPermission
                    {
                        SystemUserID = request.UserID,
                        PermissionID = item.PermissionID,
                        CanView = item.CanView,
                        CanCreate = item.CanCreate,
                        CanEdit = item.CanEdit,
                        CanDelete = item.CanDelete,
                        GrantedBy = grantedBy,
                        GrantedAt = now
                    });
                }
            }

            // احذف اللي بقى كله false
            var zeroIds = request.Permissions
                .Where(r => !r.CanView && !r.CanCreate && !r.CanEdit && !r.CanDelete)
                .Select(r => r.PermissionID).ToHashSet();

            var toRemove = existing.Where(ep => zeroIds.Contains(ep.PermissionID)).ToList();
            if (toRemove.Any()) _db.UserPermissions.RemoveRange(toRemove);

            await _db.SaveChangesAsync();

            await _auditService.LogAsync(
                grantedBy, "Staff", "Permission.Assign",
                "UserPermission", request.UserID,
                oldSnap, request.Permissions);

            return Json(new { success = true });
        }

        // تعيين دور في مدينة
        [HttpPost("AssignCityRole")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignCityRole(AssignCityRoleViewModel model)
        {
            var guard = CheckSuperAdmin();
            if (guard != null) return guard;

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "بيانات غير صحيحة";
                return RedirectToAction(nameof(Assign), new { userId = model.UserID });
            }

            var exists = await _db.CityStaffs
                .AnyAsync(cs => cs.SystemUserID == model.UserID
                             && cs.DormitoryCityID == model.DormitoryCityID);

            if (exists)
            {
                TempData["Error"] = "الموظف مُعيَّن بالفعل في هذه المدينة";
                return RedirectToAction(nameof(Assign), new { userId = model.UserID });
            }

            _db.CityStaffs.Add(new CityStaff
            {
                SystemUserID = model.UserID,
                DormitoryCityID = model.DormitoryCityID,
                RoleInCity = model.RoleInCity,
                IsPrimary = model.IsPrimary,
                AssignedAt = DateTime.Now,
                AssignedBy = CurrentUserID()
            });

            await _db.SaveChangesAsync();

            await _auditService.LogAsync(
                CurrentUserID(), "Staff", "CityStaff.Assign",
                "CityStaff", model.UserID,
                null, new { model.DormitoryCityID, model.RoleInCity });

            TempData["Success"] = "تم تعيين الدور في المدينة";
            return RedirectToAction(nameof(Assign), new { userId = model.UserID });
        }

        // إزالة دور من مدينة — AJAX
        [HttpPost("RemoveCityRole")]
        public async Task<IActionResult> RemoveCityRole(int cityStaffId, int userId)
        {
            var guard = CheckSuperAdmin();
            if (guard != null) return Json(new { success = false });

            var cs = await _db.CityStaffs.FindAsync(cityStaffId);
            if (cs == null) return Json(new { success = false, message = "غير موجود" });

            _db.CityStaffs.Remove(cs);
            await _db.SaveChangesAsync();

            await _auditService.LogAsync(
                CurrentUserID(), "Staff", "CityStaff.Remove",
                "CityStaff", cityStaffId,
                new { cs.DormitoryCityID, cs.RoleInCity }, null);

            return Json(new { success = true });
        }

        // ══════════════════════════════════════════════════════════════
        // 3. DataScopes
        // ══════════════════════════════════════════════════════════════

        [HttpGet("DataScopes")]
        public async Task<IActionResult> DataScopes(int userId)
        {
            var guard = CheckSuperAdmin();
            if (guard != null) return guard;

            var user = await _db.SystemUsers
                .FirstOrDefaultAsync(u => u.ID == userId && !u.IsDeleted);

            if (user == null)
            {
                TempData["Error"] = "المستخدم غير موجود";
                return RedirectToAction(nameof(Users));
            }

            // ✅ نجلب النطاقات عبر العلاقة Many-to-Many
            var currentScopes = await _db.DataScopes
                .Where(ds => ds.SystemUsers.Any(u => u.ID == userId))
                .Select(ds => new UserDataScopeRowViewModel
                {
                    DataScopeID = ds.ID,
                    ScopeType = ds.ScopeType,
                    ScopeTypeDisplay = MapScopeType(ds.ScopeType),
                    ScopeValue = ds.ScopeValue,
                    ScopeValueDisplay = ds.ScopeValue ?? "—"
                })
                .ToListAsync();

            var cities = await _db.DormitoryCities
                .Where(c => c.IsDeleted != true && c.IsActive == true)
                .Select(c => new SelectItem("CityID:" + c.ID, c.Name))
                .ToListAsync();

            var buildings = await _db.CityBuildings
                .Where(b => b.IsDeleted != true && b.IsActive == true)
                .Join(_db.DormitoryCities,
                      b => b.DormitoryCityID,
                      dc => dc.ID,
                      (b, dc) => new SelectItem("BuildingID:" + b.ID, dc.Name + " — " + b.BuildingName))
                .ToListAsync();

            var vm = new DataScopeIndexViewModel
            {
                UserID = userId,
                UserName = user.Name,
                CurrentScopes = currentScopes,
                AvailableCities = cities,
                AvailableBuildings = buildings,
                AddScope = new AddDataScopeViewModel { UserID = userId }
            };

            return View(vm);
        }

        [HttpPost("DataScopes")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DataScopes(AddDataScopeViewModel model)
        {
            var guard = CheckSuperAdmin();
            if (guard != null) return guard;

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "بيانات غير صحيحة";
                return RedirectToAction(nameof(DataScopes), new { userId = model.UserID });
            }

            // ابحث عن DataScope موجود أو أنشئ جديد
            var scope = await _db.DataScopes
                .FirstOrDefaultAsync(ds => ds.ScopeType == model.ScopeType
                                        && ds.ScopeValue == model.ScopeValue);

            if (scope == null)
            {
                scope = new DataScope
                {
                    ScopeType = model.ScopeType,
                    ScopeValue = model.ScopeValue
                };
                _db.DataScopes.Add(scope);
                await _db.SaveChangesAsync();
            }

            // ✅ نجلب الـ user مع الـ DataScopes عبر العلاقة
            var user = await _db.SystemUsers
                .Include(u => u.DataScopes)
                .FirstOrDefaultAsync(u => u.ID == model.UserID);

            if (user == null)
            {
                TempData["Error"] = "المستخدم غير موجود";
                return RedirectToAction(nameof(DataScopes), new { userId = model.UserID });
            }

            if (user.DataScopes.Any(d => d.ID == scope.ID))
            {
                TempData["Error"] = "هذا النطاق مُعيَّن بالفعل";
                return RedirectToAction(nameof(DataScopes), new { userId = model.UserID });
            }

            user.DataScopes.Add(scope);
            await _db.SaveChangesAsync();

            await _auditService.LogAsync(
                CurrentUserID(), "Staff", "DataScope.Add",
                "UserDataScope", model.UserID,
                null, new { model.ScopeType, model.ScopeValue });

            TempData["Success"] = "تم إضافة نطاق البيانات";
            return RedirectToAction(nameof(DataScopes), new { userId = model.UserID });
        }

        // إزالة نطاق — AJAX
        [HttpPost("RemoveDataScope")]
        public async Task<IActionResult> RemoveDataScope(int dataScopeId, int userId)
        {
            var guard = CheckSuperAdmin();
            if (guard != null) return Json(new { success = false });

            // ✅ نجلب الـ user مع الـ DataScopes عبر العلاقة
            var user = await _db.SystemUsers
                .Include(u => u.DataScopes)
                .FirstOrDefaultAsync(u => u.ID == userId);

            var scope = user?.DataScopes.FirstOrDefault(d => d.ID == dataScopeId);
            if (scope == null) return Json(new { success = false, message = "غير موجود" });

            user!.DataScopes.Remove(scope);
            await _db.SaveChangesAsync();

            return Json(new { success = true, message = "تم إزالة النطاق" });
        }

        // ══════════════════════════════════════════════════════════════
        // 4. AuditLog
        // ══════════════════════════════════════════════════════════════

        [HttpGet("AuditLog")]
        public async Task<IActionResult> AuditLog(AuditLogFilterViewModel filter)
        {
            var guard = CheckSuperAdmin();
            if (guard != null) return guard;

            var query = _db.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(filter.UserType))
                query = query.Where(l => l.UserType == filter.UserType);

            if (!string.IsNullOrEmpty(filter.Action))
                query = query.Where(l => l.Action.Contains(filter.Action));

            if (!string.IsNullOrEmpty(filter.TableName))
                query = query.Where(l => l.TableName == filter.TableName);

            if (filter.UserID.HasValue)
                query = query.Where(l => l.UserID == filter.UserID.Value);

            if (filter.From.HasValue)
                query = query.Where(l => l.CreatedAt >= filter.From.Value);

            if (filter.To.HasValue)
                query = query.Where(l => l.CreatedAt <= filter.To.Value.AddDays(1));

            var total = await query.CountAsync();
            var today = DateTime.Today;
            var todayCount = await _db.AuditLogs.CountAsync(l => l.CreatedAt >= today);
            var weekCount = await _db.AuditLogs.CountAsync(l => l.CreatedAt >= today.AddDays(-7));

            var rawLogs = await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            // أسماء الموظفين دفعة واحدة
            var staffIds = rawLogs.Where(l => l.UserType == "Staff")
                .Select(l => l.UserID).Distinct().ToList();
            var staffNames = await _db.SystemUsers
                .Where(u => staffIds.Contains(u.ID))
                .ToDictionaryAsync(u => u.ID, u => u.Name);

            // أسماء الطلاب دفعة واحدة
            var studentIds = rawLogs.Where(l => l.UserType == "Student")
                .Select(l => l.UserID).Distinct().ToList();
            var studentNames = await _db.Students
                .Where(s => studentIds.Contains(s.ID))
                .ToDictionaryAsync(s => s.ID, s => s.FullName);

            // أسماء المدن دفعة واحدة
            var cityIds = rawLogs.Where(l => l.DormitoryCityID.HasValue)
                .Select(l => l.DormitoryCityID!.Value).Distinct().ToList();
            var cityNames = await _db.DormitoryCities
                .Where(c => cityIds.Contains(c.ID))
                .ToDictionaryAsync(c => c.ID, c => c.Name);

            var logs = rawLogs.Select(l => new AuditLogRowViewModel
            {
                ID = l.ID,
                UserID = l.UserID,
                UserType = l.UserType,
                UserDisplayName = l.UserType == "Staff"
                    ? staffNames.GetValueOrDefault(l.UserID, "موظف #" + l.UserID)
                    : l.UserType == "Student"
                        ? studentNames.GetValueOrDefault(l.UserID, "طالب #" + l.UserID)
                        : "النظام",
                Action = l.Action,
                ActionDisplay = MapAction(l.Action),
                TableName = l.TableName,
                RecordID = l.RecordID,
                OldValues = l.OldValues,
                NewValues = l.NewValues,
                IPAddress = l.IPAddress,
                CityName = l.DormitoryCityID.HasValue
                    ? cityNames.GetValueOrDefault(l.DormitoryCityID.Value)
                    : null,
                CreatedAt = l.CreatedAt
            }).ToList();

            var vm = new AuditLogViewModel
            {
                Logs = logs,
                Filter = filter,
                TotalCount = total,
                TodayCount = todayCount,
                WeekCount = weekCount
            };

            return View(vm);
        }

        // ══════════════════════════════════════════════════════════════
        // Helpers
        // ══════════════════════════════════════════════════════════════

        private static string GenerateTempPassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789@#";
            var rng = new Random();
            return new string(Enumerable.Range(0, 10)
                .Select(_ => chars[rng.Next(chars.Length)]).ToArray());
        }

        private static string MapRole(string role) => role switch
        {
            "Manager" => "مدير المدينة",
            "Accountant" => "محاسب",
            "MealStaff" => "موظف تغذية",
            "HousingStaff" => "موظف إسكان",
            "ApplicationReviewer" => "مراجع طلبات",
            "MaintenanceStaff" => "موظف صيانة",
            "SecurityStaff" => "موظف أمن",
            _ => role
        };

        private static string MapScopeType(string type) => type switch
        {
            "All" => "كل البيانات",
            "MaleOnly" => "ذكور فقط",
            "FemaleOnly" => "إناث فقط",
            "DormitoryCity" => "مدينة جامعية محددة",
            "Building" => "مبنى محدد",
            "Faculty" => "كلية محددة",
            _ => type
        };

        private static string MapAction(string action) => action switch
        {
            "User.Create" => "إنشاء مستخدم",
            "User.Delete" => "حذف مستخدم",
            "User.Activate" => "تفعيل حساب",
            "User.Deactivate" => "تعطيل حساب",
            "User.ResetPassword" => "إعادة تعيين كلمة المرور",
            "Permission.Assign" => "تعيين صلاحيات",
            "CityStaff.Assign" => "تعيين دور في مدينة",
            "CityStaff.Remove" => "إلغاء دور في مدينة",
            "DataScope.Add" => "إضافة نطاق بيانات",
            "Application.Approve" => "قبول الطلب",
            "Application.Reject" => "رفض الطلب",
            "Student.Edit" => "تعديل بيانات الطالب",
            _ => action
        };

        private static string BuildWelcomeEmail(string name, string username, string password) => $"""
            <div dir="rtl" style="font-family:Arial;line-height:2">
                <h2>مرحباً {name}</h2>
                <p>تم إنشاء حسابك على نظام <strong>UniStay</strong>.</p>
                <ul>
                    <li><strong>اسم المستخدم:</strong> {username}</li>
                    <li><strong>كلمة المرور المؤقتة:</strong> {password}</li>
                </ul>
                <p style="color:red">⚠️ يجب تغيير كلمة المرور عند أول دخول</p>
            </div>
            """;

        private static string BuildResetEmail(string name, string password) => $"""
            <div dir="rtl" style="font-family:Arial;line-height:2">
                <h2>إعادة تعيين كلمة المرور</h2>
                <p>عزيزي {name}،</p>
                <p><strong>كلمة المرور الجديدة:</strong> {password}</p>
                <p style="color:red">⚠️ يجب تغيير كلمة المرور فور تسجيل الدخول</p>
            </div>
            """;
    }
}