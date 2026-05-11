using EatUp.Data;
using EatUp.Models;
using EatUp.Services;
using EatUp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;

namespace EatUp.Controllers;

[Authorize(Policy = "AdminOnly")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IEmailService _email;

    public AdminController(ApplicationDbContext db, IEmailService email)
    {
        _db = db;
        _email = email;
    }

    public async Task<IActionResult> Index()
    {
        var ic = CultureInfo.InvariantCulture;
        var todayStart = DateTime.UtcNow.Date;
        var tomorrowStart = todayStart.AddDays(1);
        var cutoff30 = todayStart.AddDays(-29);

        var totalUsers = await _db.Users.CountAsync(u => u.Role != UserRole.Admin);
        var totalRestaurants = await _db.Restaurants.CountAsync(r => r.IsApproved);
        var ordersToday = await _db.Orders.CountAsync(o => o.CreatedAt >= todayStart && o.CreatedAt < tomorrowStart);
        var revenueToday = (await _db.Orders
            .Where(o => o.CreatedAt >= todayStart && o.CreatedAt < tomorrowStart && o.Status == OrderStatus.Delivered)
            .SumAsync(o => (decimal?)o.Subtotal) ?? 0) * 0.2m;
        var pendingRestaurants = await _db.Restaurants.CountAsync(r => !r.IsApproved && r.RejectionReason == null);
        var pendingCRs = await _db.MenuItemChangeRequests.CountAsync(cr => cr.Status == ChangeRequestStatus.Pending);

        var recentOrders = await _db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Restaurant)
            .OrderByDescending(o => o.CreatedAt)
            .Take(10)
            .ToListAsync();

        var orders30 = await _db.Orders
            .Where(o => o.CreatedAt >= cutoff30)
            .ToListAsync();

        var byDay = orders30
            .GroupBy(o => o.CreatedAt.Date)
            .ToDictionary(g => g.Key, g => (
                count: g.Count(),
                revenue: g.Where(o => o.Status == OrderStatus.Delivered).Sum(o => o.Subtotal) * 0.2m
            ));

        var days = Enumerable.Range(0, 30).Select(i => cutoff30.AddDays(i)).ToList();
        var labels = days.Select(d => d.ToString("dd MMM", new CultureInfo("ro-RO"))).ToArray();
        var countsArr = days.Select(d => byDay.TryGetValue(d, out var s) ? s.count : 0).ToArray();
        var revsArr = days.Select(d => byDay.TryGetValue(d, out var s) ? (double)s.revenue : 0.0).ToArray();

        var vm = new AdminDashboardViewModel
        {
            TotalUsers = totalUsers,
            TotalRestaurants = totalRestaurants,
            OrdersToday = ordersToday,
            RevenueToday = revenueToday,
            PendingRestaurants = pendingRestaurants,
            PendingChangeRequests = pendingCRs,
            RecentOrders = recentOrders,
            ChartLabelsJson = JsonSerializer.Serialize(labels),
            ChartOrdersJson = JsonSerializer.Serialize(countsArr),
            ChartRevenueJson = JsonSerializer.Serialize(revsArr),
        };
        return View(vm);
    }

    public async Task<IActionResult> Restaurants(string tab = "pending")
    {
        var pending = await _db.Restaurants
            .Include(r => r.User)
            .Where(r => !r.IsApproved && r.RejectionReason == null)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();

        var all = await _db.Restaurants
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        ViewBag.Tab = tab;
        ViewBag.PendingCount = pending.Count;
        ViewBag.PendingRestaurants = pending;
        ViewBag.AllRestaurants = all;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveRestaurant(int id)
    {
        var r = await _db.Restaurants.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return NotFound();
        r.IsApproved = true;
        r.RejectionReason = null;
        await _db.SaveChangesAsync();
        _email.SendRestaurantApproved(r.User.Email, r.User.Name, r.Name);
        TempData["Success"] = $"Restaurantul \"{r.Name}\" a fost aprobat.";
        return RedirectToAction(nameof(Restaurants));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectRestaurant(int id, string reason)
    {
        var r = await _db.Restaurants.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return NotFound();
        r.IsApproved = false;
        r.RejectionReason = reason?.Trim() ?? "Respins de admin.";
        await _db.SaveChangesAsync();
        _email.SendRestaurantRejected(r.User.Email, r.User.Name, r.Name, r.RejectionReason);
        TempData["Success"] = $"Restaurantul \"{r.Name}\" a fost respins.";
        return RedirectToAction(nameof(Restaurants));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BlockRestaurant(int id)
    {
        var r = await _db.Restaurants.FindAsync(id);
        if (r == null) return NotFound();
        r.IsBlocked = !r.IsBlocked;
        await _db.SaveChangesAsync();
        TempData["Success"] = r.IsBlocked
            ? $"Restaurantul \"{r.Name}\" a fost blocat."
            : $"Restaurantul \"{r.Name}\" a fost deblocat.";
        return RedirectToAction(nameof(Restaurants), new { tab = "all" });
    }

    public async Task<IActionResult> Users(string? q)
    {
        var query = _db.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(u => u.Name.Contains(q) || u.Email.Contains(q));

        var users = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();
        ViewBag.Q = q;
        return View(users);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BlockUser(int id)
    {
        var u = await _db.Users.FindAsync(id);
        if (u == null) return NotFound();
        u.IsBlocked = !u.IsBlocked;
        await _db.SaveChangesAsync();
        TempData["Success"] = u.IsBlocked
            ? $"Utilizatorul \"{u.Name}\" a fost blocat."
            : $"Utilizatorul \"{u.Name}\" a fost deblocat.";
        return RedirectToAction(nameof(Users));
    }

    public async Task<IActionResult> Orders(string? status, int? restaurantId, string? dateFrom, string? dateTo)
    {
        var query = _db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Restaurant)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, out var parsedStatus))
            query = query.Where(o => o.Status == parsedStatus);

        if (restaurantId.HasValue)
            query = query.Where(o => o.RestaurantId == restaurantId.Value);

        if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out var from))
            query = query.Where(o => o.CreatedAt >= from.Date);

        if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out var to))
            query = query.Where(o => o.CreatedAt < to.Date.AddDays(1));

        var orders = await query.OrderByDescending(o => o.CreatedAt).Take(200).ToListAsync();
        var restaurants = await _db.Restaurants.OrderBy(r => r.Name).ToListAsync();

        ViewBag.Orders = orders;
        ViewBag.Restaurants = restaurants;
        ViewBag.Status = status;
        ViewBag.RestaurantId = restaurantId;
        ViewBag.DateFrom = dateFrom;
        ViewBag.DateTo = dateTo;
        return View();
    }

    public async Task<IActionResult> OrderDetail(int id)
    {
        var order = await _db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Restaurant)
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound();
        return PartialView("_OrderDetail", order);
    }

    public async Task<IActionResult> ChangeRequests(string filter = "pending")
    {
        var query = _db.MenuItemChangeRequests
            .Include(cr => cr.MenuItem)
            .Include(cr => cr.Restaurant)
            .AsQueryable();

        if (filter == "pending")
            query = query.Where(cr => cr.Status == ChangeRequestStatus.Pending);

        var requests = await query.OrderByDescending(cr => cr.CreatedAt).ToListAsync();

        ViewBag.Filter = filter;
        ViewBag.PendingCount = await _db.MenuItemChangeRequests
            .CountAsync(cr => cr.Status == ChangeRequestStatus.Pending);
        return View(requests);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveChangeRequest(int id)
    {
        var cr = await _db.MenuItemChangeRequests
            .Include(x => x.MenuItem)
            .Include(x => x.Restaurant).ThenInclude(r => r.User)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (cr == null || cr.Status != ChangeRequestStatus.Pending)
            return BadRequest(new { error = "Cererea nu a fost găsită sau a fost deja procesată." });

        ProposedData? data = null;
        try { data = JsonSerializer.Deserialize<ProposedData>(cr.ProposedDataJson); }
        catch { return BadRequest(new { error = "Date propuse invalide." }); }

        if (data == null) return BadRequest(new { error = "Date propuse lipsă." });

        if (cr.Type == ChangeRequestType.Create)
        {
            var item = new MenuItem
            {
                RestaurantId = cr.RestaurantId,
                CategoryId = data.categoryId,
                Name = data.name ?? string.Empty,
                Description = data.description,
                Price = data.price,
                Image = data.image,
                IsAvailable = true,
                IsApproved = true,
                CreatedAt = DateTime.UtcNow
            };
            _db.MenuItems.Add(item);
        }
        else if (cr.Type == ChangeRequestType.Update && cr.MenuItem != null)
        {
            cr.MenuItem.CategoryId = data.categoryId;
            cr.MenuItem.Name = data.name ?? cr.MenuItem.Name;
            cr.MenuItem.Description = data.description;
            cr.MenuItem.Price = data.price;
            if (data.image != null) cr.MenuItem.Image = data.image;
        }
        else if (cr.Type == ChangeRequestType.Delete && cr.MenuItem != null)
        {
            _db.MenuItems.Remove(cr.MenuItem);
            cr.MenuItemId = null;
        }

        cr.Status = ChangeRequestStatus.Approved;
        cr.ReviewedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        if (cr.Restaurant?.User != null)
        {
            var itemName = cr.Type == ChangeRequestType.Create ? (data.name ?? "produs nou") : cr.MenuItem?.Name ?? "produs";
            var changeType = cr.Type == ChangeRequestType.Create ? "adăugare"
                           : cr.Type == ChangeRequestType.Update ? "actualizare"
                           : "ștergere";
            _email.SendChangeApproved(cr.Restaurant.User.Email, cr.Restaurant.User.Name,
                cr.Restaurant.Name, itemName, changeType);
        }

        TempData["Success"] = "Cererea a fost aprobată.";
        return RedirectToAction(nameof(ChangeRequests));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectChangeRequest(int id, string note)
    {
        var cr = await _db.MenuItemChangeRequests
            .Include(x => x.MenuItem)
            .Include(x => x.Restaurant).ThenInclude(r => r.User)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (cr == null || cr.Status != ChangeRequestStatus.Pending)
            return BadRequest(new { error = "Cererea nu a fost găsită sau a fost deja procesată." });

        cr.Status = ChangeRequestStatus.Rejected;
        cr.AdminNote = note?.Trim();
        cr.ReviewedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        if (cr.Restaurant?.User != null)
        {
            string itemName;
            try { itemName = cr.MenuItem?.Name ?? JsonSerializer.Deserialize<ProposedData>(cr.ProposedDataJson)?.name ?? "produs"; }
            catch { itemName = "produs"; }
            _email.SendChangeRejected(cr.Restaurant.User.Email, cr.Restaurant.User.Name,
                cr.Restaurant.Name, itemName, cr.AdminNote);
        }

        TempData["Success"] = "Cererea a fost respinsă.";
        return RedirectToAction(nameof(ChangeRequests));
    }

    public async Task<IActionResult> PromoCodes()
    {
        var codes = await _db.PromoCodes.OrderByDescending(p => p.CreatedAt).ToListAsync();
        return View(codes);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePromoCode(CreatePromoCodeForm form)
    {
        var code = form.Code.Trim().ToUpper();
        if (await _db.PromoCodes.AnyAsync(p => p.Code == code))
        {
            TempData["Error"] = "Există deja un cod cu acest nume.";
            return RedirectToAction(nameof(PromoCodes));
        }

        _db.PromoCodes.Add(new PromoCode
        {
            Code = code,
            Description = form.Description?.Trim(),
            DiscountType = form.DiscountType,
            DiscountValue = form.DiscountValue,
            MinOrderAmount = form.MinOrderAmount,
            MaxUses = form.MaxUses,
            ExpiresAt = form.ExpiresAt,
            IsActive = form.IsActive,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Codul \"{code}\" a fost creat.";
        return RedirectToAction(nameof(PromoCodes));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePromoCode(int id)
    {
        var code = await _db.PromoCodes.FindAsync(id);
        if (code == null) return NotFound();
        code.IsActive = !code.IsActive;
        await _db.SaveChangesAsync();
        TempData["Success"] = code.IsActive ? "Cod activat." : "Cod dezactivat.";
        return RedirectToAction(nameof(PromoCodes));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePromoCode(int id)
    {
        var code = await _db.PromoCodes.FindAsync(id);
        if (code == null) return NotFound();
        _db.PromoCodes.Remove(code);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Codul \"{code.Code}\" a fost șters.";
        return RedirectToAction(nameof(PromoCodes));
    }

    public async Task<IActionResult> Messages()
    {
        var messages = await _db.ContactMessages
            .OrderByDescending(m => m.SentAt)
            .ToListAsync();
        return View(messages);
    }

    [HttpPost]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var msg = await _db.ContactMessages.FindAsync(id);
        if (msg == null) return NotFound();
        msg.IsRead = true;
        await _db.SaveChangesAsync();
        return Json(new { ok = true });
    }

    [HttpPost]
    public async Task<IActionResult> MarkAsReplied(int id)
    {
        var msg = await _db.ContactMessages.FindAsync(id);
        if (msg == null) return NotFound();
        msg.IsRead = true;
        msg.IsReplied = true;
        await _db.SaveChangesAsync();
        return Json(new { ok = true });
    }
}

public class CreatePromoCodeForm
{
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal MinOrderAmount { get; set; }
    public int MaxUses { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
}

internal record ProposedData(
    string? name,
    string? description,
    decimal price,
    int categoryId,
    string? image);
