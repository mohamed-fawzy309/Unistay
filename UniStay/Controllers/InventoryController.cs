using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniStay.Data;
using UniStay.Helpers;
using UniStay.Models;
using UniStay.Services.Interfaces;
using UniStay.ViewModels.Inventory;

namespace UniStay.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminCookie")]
    public class InventoryController : Controller
    {
        private readonly AssuitDbContext _db;
        private readonly IAuditService _audit;

        public InventoryController(AssuitDbContext db, IAuditService audit)
        {
            _db = db;
            _audit = audit;
        }

        private int CurrentUserId => int.Parse(User.FindFirst("UserID")!.Value);

        [HttpGet]
        public async Task<IActionResult> Items(int page = 1)
        {
            var query = _db.InventoryItems.AsQueryable();

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / 20.0);

            var items = await query
                .OrderBy(i => i.ItemName)
                .Skip((page - 1) * 20)
                .Take(20)
                .Select(i => new InventoryItemRowViewModel
                {
                    ID = i.ID,
                    ItemName = i.ItemName,
                    ItemCode = i.ItemCode,
                    ItemValue = i.ItemValue,
                    TotalStock = i.TotalStock,
                    AvailableStock = i.AvailableStock,
                    IsActive = i.IsActive ?? false
                })
                .ToListAsync();

            return View(new InventoryItemsViewModel
            {
                Items = items,
                Page = page,
                TotalPages = totalPages
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateItem(CreateInventoryItemViewModel model)
        {
            if (!ModelState.IsValid) return Json(new { success = false, message = "بيانات غير صالحة" });

            var exists = await _db.InventoryItems.AnyAsync(i => i.ItemCode == model.ItemCode);
            if (exists) return Json(new { success = false, message = "كود الصنف موجود مسبقاً" });

            var item = new InventoryItem
            {
                ItemName = model.ItemName,
                ItemCode = model.ItemCode,
                ItemValue = model.ItemValue,
                TotalStock = model.TotalStock,
                AvailableStock = model.TotalStock,
                IsActive = true
            };

            _db.InventoryItems.Add(item);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "InventoryItem.Create", "InventoryItem",
                item.ID, null, new { item.ItemName, item.ItemCode, item.TotalStock });

            return Json(new { success = true, message = "تم إضافة الصنف" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(AssignItemViewModel model)
        {
            if (!ModelState.IsValid) return Json(new { success = false, message = "بيانات غير صالحة" });

            var item = await _db.InventoryItems.FindAsync(model.InventoryItemID);
            if (item == null) return Json(new { success = false, message = "الصنف غير موجود" });

            if (item.AvailableStock < model.Quantity)
                return Json(new { success = false, message = $"الكمية المتاحة غير كافية (المتبقي: {item.AvailableStock})" });

            item.AvailableStock -= model.Quantity;

            var si = new StudentInventory
            {
                StudentID = model.StudentID,
                InventoryItemID = model.InventoryItemID,
                AllocationID = model.AllocationID,
                Quantity = model.Quantity,
                Condition = model.Condition ?? "Good",
                IsReturned = false,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = CurrentUserId
            };

            _db.StudentInventories.Add(si);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Inventory.Assign", "StudentInventory",
                si.ID, null, new { model.StudentID, model.InventoryItemID, model.Quantity });

            return Json(new { success = true, message = "تم التوزيع" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Return(ReturnItemViewModel model)
        {
            var si = await _db.StudentInventories
                .Include(s => s.InventoryItem)
                .FirstOrDefaultAsync(s => s.ID == model.StudentInventoryID);

            if (si == null) return Json(new { success = false, message = "السجل غير موجود" });
            if (si.IsReturned == true) return Json(new { success = false, message = "تم إرجاع هذا الصنف مسبقاً" });

            si.IsReturned = true;
            si.ReturnedAt = DateTime.UtcNow;
            si.ReturnedBy = CurrentUserId;
            si.Condition = model.Condition ?? si.Condition;
            si.DeductionAmount = model.DeductionAmount;

            if (si.InventoryItem != null)
                si.InventoryItem.AvailableStock += si.Quantity;

            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Inventory.Return", "StudentInventory",
                si.ID, null, new { si.StudentID, si.InventoryItemID, si.Quantity, si.DeductionAmount });

            return Json(new { success = true, message = "تم الإرجاع" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordDeduction(int studentInventoryId, decimal amount)
        {
            var si = await _db.StudentInventories.FindAsync(studentInventoryId);
            if (si == null) return Json(new { success = false, message = "السجل غير موجود" });

            si.DeductionAmount = (si.DeductionAmount ?? 0) + amount;

            await _db.SaveChangesAsync();

            await _audit.LogAsync(CurrentUserId, "Staff", "Inventory.Deduction", "StudentInventory",
                si.ID, null, new { si.StudentID, si.InventoryItemID, DeductionAmount = amount });

            return Json(new { success = true, message = "تم تسجيل الخصم" });
        }

        [HttpGet]
        public async Task<IActionResult> Report(int? cityId, int page = 1)
        {
            var query = _db.StudentInventories
                .Include(s => s.Student)
                .Include(s => s.InventoryItem)
                .AsQueryable();

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / 20.0);

            var assignments = await query
                .OrderByDescending(s => s.AssignedAt)
                .Skip((page - 1) * 20)
                .Take(20)
                .Select(s => new InventoryAssignmentRowViewModel
                {
                    ID = s.ID,
                    StudentName = s.Student!.FullName,
                    NationalID = s.Student.NationalID,
                    ItemName = s.InventoryItem!.ItemName,
                    Quantity = s.Quantity,
                    Condition = s.Condition,
                    IsReturned = s.IsReturned ?? false,
                    DeductionAmount = s.DeductionAmount,
                    AssignedAt = s.AssignedAt
                })
                .ToListAsync();

            var totalItems = await _db.InventoryItems.CountAsync();
            var totalAvailable = await _db.InventoryItems.SumAsync(i => i.AvailableStock);
            var totalAssigned = await _db.StudentInventories
                .Where(s => s.IsReturned != true)
                .SumAsync(s => (int)s.Quantity);
            var totalValue = await _db.InventoryItems.SumAsync(i => i.ItemValue * i.TotalStock);
            var totalDeductions = await _db.StudentInventories
                .SumAsync(s => (decimal?)s.DeductionAmount) ?? 0;

            return View(new InventoryReportViewModel
            {
                TotalItems = totalItems,
                TotalAssigned = totalAssigned,
                TotalAvailable = totalAvailable,
                TotalValue = totalValue,
                TotalDeductions = totalDeductions,
                Assignments = assignments,
                Page = page,
                TotalPages = totalPages
            });
        }
    }
}
