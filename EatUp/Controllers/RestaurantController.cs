using EatUp.Data;
using EatUp.Models;
using EatUp.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;

namespace EatUp.Controllers;

[Authorize(Policy = "RestaurantOnly")]
public class RestaurantController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;

    public RestaurantController(ApplicationDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    private async Task<Restaurant?> GetOwnRestaurant()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return await _db.Restaurants.FirstOrDefaultAsync(r => r.UserId == userId);
    }

    private async Task<IActionResult?> BlockedRedirectAsync(Restaurant r)
    {
        if (!r.IsBlocked) return null;
        await HttpContext.SignOutAsync("Cookies");
        HttpContext.Session.Clear();
        return RedirectToAction("RestaurantSuspended", "Account");
    }

    public async Task<IActionResult> Dashboard()
    {
        var r = await GetOwnRestaurant();
        if (r == null) return NotFound();
        var blocked = await BlockedRedirectAsync(r); if (blocked != null) return blocked;
        if (!r.IsApproved) return View("Pending", r);
        return RedirectToAction(nameof(Stats));
    }

    public async Task<IActionResult> Orders()
    {
        var r = await GetOwnRestaurant();
        if (r == null) return NotFound();
        var blocked = await BlockedRedirectAsync(r); if (blocked != null) return blocked;
        if (!r.IsApproved) return View("Pending", r);

        var orders = await _db.Orders
            .Where(o => o.RestaurantId == r.Id)
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        ViewBag.HasApprovedItems = await _db.MenuItems.AnyAsync(m => m.RestaurantId == r.Id && m.IsApproved);

        var vm = new RestaurantOrdersViewModel
        {
            Restaurant = r,
            ActiveOrders = orders.Where(o => RestaurantOrdersViewModel.IsActive(o.Status)).ToList(),
            CompletedOrders = orders.Where(o => !RestaurantOrdersViewModel.IsActive(o.Status)).ToList(),
        };
        return View(vm);
    }

    public async Task<IActionResult> Menu()
    {
        var r = await GetOwnRestaurant();
        if (r == null) return NotFound();
        var blocked = await BlockedRedirectAsync(r); if (blocked != null) return blocked;
        if (!r.IsApproved) return View("Pending", r);

        var categories = await _db.MenuCategories
            .Where(c => c.RestaurantId == r.Id)
            .Include(c => c.MenuItems)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();

        var pendingItemIds = await _db.MenuItemChangeRequests
            .Where(cr => cr.RestaurantId == r.Id && cr.Status == ChangeRequestStatus.Pending)
            .Select(cr => new { cr.MenuItemId, cr.Type })
            .ToListAsync();

        var pendingCount = await _db.MenuItemChangeRequests
            .CountAsync(cr => cr.RestaurantId == r.Id && cr.Status == ChangeRequestStatus.Pending);

        ViewBag.HasApprovedItems = await _db.MenuItems.AnyAsync(m => m.RestaurantId == r.Id && m.IsApproved);

        var vm = new MenuManagementViewModel
        {
            Restaurant = r,
            PendingRequestCount = pendingCount,
            Categories = categories.Select(c => new MenuCategoryWithItems
            {
                Category = c,
                Items = c.MenuItems.OrderBy(m => m.Name).Select(m =>
                {
                    var p = pendingItemIds.FirstOrDefault(x => x.MenuItemId == m.Id);
                    return new MenuItemRow
                    {
                        Item = m,
                        HasPending = p != null,
                        PendingType = p?.Type
                    };
                }).ToList()
            }).ToList()
        };
        return View(vm);
    }

    public async Task<IActionResult> ChangeRequests()
    {
        var r = await GetOwnRestaurant();
        if (r == null) return NotFound();
        var blocked = await BlockedRedirectAsync(r); if (blocked != null) return blocked;
        if (!r.IsApproved) return View("Pending", r);

        var requests = await _db.MenuItemChangeRequests
            .Where(cr => cr.RestaurantId == r.Id)
            .Include(cr => cr.MenuItem)
            .OrderByDescending(cr => cr.CreatedAt)
            .ToListAsync();

        ViewBag.Restaurant = r;
        return View(requests);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategory([FromBody] CategoryNameRequest req)
    {
        var r = await GetOwnRestaurant();
        if (r == null) return Forbid();
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new { error = "Numele categoriei este obligatoriu." });

        var maxOrder = await _db.MenuCategories
            .Where(c => c.RestaurantId == r.Id)
            .MaxAsync(c => (int?)c.DisplayOrder) ?? 0;

        var cat = new MenuCategory
        {
            RestaurantId = r.Id,
            Name = req.Name.Trim(),
            DisplayOrder = maxOrder + 1
        };
        _db.MenuCategories.Add(cat);
        await _db.SaveChangesAsync();

        return Json(new { ok = true, id = cat.Id, name = cat.Name });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCategory([FromBody] UpdateCategoryRequest req)
    {
        var r = await GetOwnRestaurant();
        if (r == null) return Forbid();

        var cat = await _db.MenuCategories.FirstOrDefaultAsync(
            c => c.Id == req.Id && c.RestaurantId == r.Id);
        if (cat == null) return NotFound();

        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new { error = "Numele nu poate fi gol." });

        cat.Name = req.Name.Trim();
        await _db.SaveChangesAsync();
        return Json(new { ok = true });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory([FromBody] IdRequest req)
    {
        var r = await GetOwnRestaurant();
        if (r == null) return Forbid();

        var cat = await _db.MenuCategories
            .Include(c => c.MenuItems)
            .FirstOrDefaultAsync(c => c.Id == req.Id && c.RestaurantId == r.Id);
        if (cat == null) return NotFound();

        if (cat.MenuItems.Count > 0)
            return BadRequest(new { error = "Categoria are produse. Șterge produsele mai întâi." });

        _db.MenuCategories.Remove(cat);
        await _db.SaveChangesAsync();
        return Json(new { ok = true });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ReorderCategories([FromBody] ReorderRequest req)
    {
        var r = await GetOwnRestaurant();
        if (r == null) return Forbid();

        var cats = await _db.MenuCategories
            .Where(c => c.RestaurantId == r.Id && req.Ids.Contains(c.Id))
            .ToListAsync();

        for (int i = 0; i < req.Ids.Length; i++)
        {
            var cat = cats.FirstOrDefault(c => c.Id == req.Ids[i]);
            if (cat != null) cat.DisplayOrder = i + 1;
        }
        await _db.SaveChangesAsync();
        return Json(new { ok = true });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAvailability([FromBody] IdRequest req)
    {
        var r = await GetOwnRestaurant();
        if (r == null) return Forbid();

        var item = await _db.MenuItems.FirstOrDefaultAsync(
            m => m.Id == req.Id && m.RestaurantId == r.Id);
        if (item == null) return NotFound();

        item.IsAvailable = !item.IsAvailable;
        await _db.SaveChangesAsync();
        return Json(new { ok = true, isAvailable = item.IsAvailable });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitChangeRequest([FromForm] ChangeRequestFormModel form)
    {
        var r = await GetOwnRestaurant();
        if (r == null) return Forbid();

        if (string.IsNullOrWhiteSpace(form.Name))
            return BadRequest(new { error = "Numele produsului este obligatoriu." });

        if (form.Price <= 0)
            return BadRequest(new { error = "Prețul trebuie să fie pozitiv." });

        var cat = await _db.MenuCategories.FirstOrDefaultAsync(
            c => c.Id == form.CategoryId && c.RestaurantId == r.Id);
        if (cat == null)
            return BadRequest(new { error = "Categoria nu a fost găsită." });

        string? imagePath = null;
        if (form.Image != null && form.Image.Length > 0)
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(form.Image.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
                return BadRequest(new { error = "Format imagine invalid. Folosiți JPG, PNG sau WebP." });

            var fileName = $"{Guid.NewGuid()}{ext}";
            var folder = Path.Combine(_env.WebRootPath, "img", "menu");
            Directory.CreateDirectory(folder);
            var filePath = Path.Combine(folder, fileName);
            await using var stream = new FileStream(filePath, FileMode.Create);
            await form.Image.CopyToAsync(stream);
            imagePath = $"/img/menu/{fileName}";
        }

        // for updates, fall back to the existing image when no new file is uploaded
        string? existingImage = null;
        if (form.Type == ChangeRequestType.Update && form.MenuItemId.HasValue)
        {
            var existing = await _db.MenuItems.FindAsync(form.MenuItemId.Value);
            if (existing == null || existing.RestaurantId != r.Id)
                return BadRequest(new { error = "Produsul nu a fost găsit." });
            existingImage = existing.Image;

            var hasPending = await _db.MenuItemChangeRequests.AnyAsync(
                cr => cr.MenuItemId == form.MenuItemId && cr.Status == ChangeRequestStatus.Pending);
            if (hasPending)
                return BadRequest(new { error = "Există deja o cerere în așteptare pentru acest produs." });
        }

        var proposedData = JsonSerializer.Serialize(new
        {
            name = form.Name.Trim(),
            description = form.Description?.Trim(),
            price = form.Price,
            categoryId = form.CategoryId,
            image = imagePath ?? existingImage
        });

        var cr = new MenuItemChangeRequest
        {
            RestaurantId = r.Id,
            MenuItemId = form.MenuItemId,
            Type = form.Type,
            ProposedDataJson = proposedData,
            Status = ChangeRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        _db.MenuItemChangeRequests.Add(cr);
        await _db.SaveChangesAsync();

        return Json(new { ok = true });
    }

    public async Task<IActionResult> Stats()
    {
        var r = await GetOwnRestaurant();
        if (r == null) return NotFound();
        var blocked = await BlockedRedirectAsync(r); if (blocked != null) return blocked;
        if (!r.IsApproved) return View("Pending", r);

        var ic = CultureInfo.InvariantCulture;
        var today = DateTime.UtcNow.Date;
        var cutoff30 = today.AddDays(-29);
        var cutoff7 = today.AddDays(-6);

        var orders = await _db.Orders
            .Where(o => o.RestaurantId == r.Id
                     && o.Status == OrderStatus.Delivered
                     && o.CreatedAt >= cutoff30)
            .ToListAsync();

        var byDay = orders
            .GroupBy(o => o.CreatedAt.Date)
            .ToDictionary(g => g.Key, g => (count: g.Count(), revenue: g.Sum(o => o.Total)));

        var days30 = Enumerable.Range(0, 30).Select(i => cutoff30.AddDays(i)).ToList();
        var days7 = Enumerable.Range(0, 7).Select(i => cutoff7.AddDays(i)).ToList();

        string[] L(IEnumerable<DateTime> ds) => ds.Select(d => d.ToString("dd MMM", new CultureInfo("ro-RO"))).ToArray();
        int[] C(IEnumerable<DateTime> ds) => ds.Select(d => byDay.TryGetValue(d, out var s) ? s.count : 0).ToArray();
        double[] Rv(IEnumerable<DateTime> ds) => ds.Select(d => byDay.TryGetValue(d, out var s) ? (double)s.revenue : 0.0).ToArray();

        var orderIds30 = orders.Select(o => o.Id).ToList();
        var topItems = orderIds30.Count == 0
            ? new List<TopItemStat>()
            : await _db.OrderItems
                .Where(oi => orderIds30.Contains(oi.OrderId))
                .GroupBy(oi => oi.NameSnapshot)
                .Select(g => new TopItemStat
                {
                    Name = g.Key,
                    Quantity = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.PriceSnapshot * oi.Quantity)
                })
                .OrderByDescending(x => x.Quantity)
                .Take(5)
                .ToListAsync();

        var orders7 = orders.Where(o => o.CreatedAt.Date >= cutoff7).ToList();

        ViewBag.HasApprovedItems = await _db.MenuItems.AnyAsync(m => m.RestaurantId == r.Id && m.IsApproved);

        var vm = new RestaurantStatsViewModel
        {
            Restaurant = r,
            TotalOrders30d = orders.Count,
            TotalRevenue30d = orders.Sum(o => o.Total),
            TotalOrders7d = orders7.Count,
            TotalRevenue7d = orders7.Sum(o => o.Total),
            ChartLabels30Json = JsonSerializer.Serialize(L(days30)),
            ChartOrders30Json = JsonSerializer.Serialize(C(days30)),
            ChartRevenue30Json = JsonSerializer.Serialize(Rv(days30)),
            ChartLabels7Json = JsonSerializer.Serialize(L(days7)),
            ChartOrders7Json = JsonSerializer.Serialize(C(days7)),
            ChartRevenue7Json = JsonSerializer.Serialize(Rv(days7)),
            TopItems = topItems,
        };
        return View(vm);
    }

    public async Task<IActionResult> Profile()
    {
        var r = await GetOwnRestaurant();
        if (r == null) return NotFound();
        var blocked = await BlockedRedirectAsync(r); if (blocked != null) return blocked;

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _db.Users.FindAsync(userId);

        var days = new[] { "Monday","Tuesday","Wednesday","Thursday","Friday","Saturday","Sunday" };
        var hours = new List<DaySchedule>();
        if (r.OpeningHoursJson != null)
        {
            try
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, DaySchedule>>(
                    r.OpeningHoursJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (dict != null)
                    hours = days.Select(d => dict.TryGetValue(d, out var h)
                        ? new DaySchedule { Day = d, IsClosed = h.IsClosed, Open = h.Open, Close = h.Close }
                        : new DaySchedule { Day = d, Open = "08:00", Close = "23:59" }).ToList();
            }
            catch { }
        }
        if (hours.Count == 0)
            hours = days.Select(d => new DaySchedule { Day = d, Open = "08:00", Close = "23:59" }).ToList();

        var existingCats = (string.IsNullOrWhiteSpace(r.Categories) ? r.Category : r.Categories)
            .Split(',').Select(c => c.Trim()).Where(c => !string.IsNullOrEmpty(c)).ToList();

        var form = new RestaurantProfileForm
        {
            Name = r.Name,
            Description = r.Description,
            Category = r.Category,
            SelectedCategories = existingCats,
            DeliveryFee = r.DeliveryFee,
            MinOrderAmount = r.MinOrderAmount,
            EstimatedDeliveryTime = r.EstimatedDeliveryTime,
            Phone = user?.Phone,
            OpeningHours = hours
        };
        ViewBag.Restaurant = r;
        return View(form);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveProfile(RestaurantProfileForm form)
    {
        var r = await GetOwnRestaurant();
        if (r == null) return NotFound();
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _db.Users.FindAsync(userId);

        if (string.IsNullOrWhiteSpace(form.Name))
        {
            TempData["Error"] = "Numele restaurantului este obligatoriu.";
            return RedirectToAction(nameof(Profile));
        }

        var selectedCats = form.SelectedCategories.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();

        r.Name = form.Name.Trim();
        r.Description = form.Description?.Trim();
        if (selectedCats.Count > 0)
        {
            r.Categories = string.Join(",", selectedCats);
            r.Category = selectedCats[0];
        }
        r.DeliveryFee = form.DeliveryFee;
        r.MinOrderAmount = form.MinOrderAmount;
        r.EstimatedDeliveryTime = form.EstimatedDeliveryTime;
        if (user != null) user.Phone = form.Phone?.Trim();

        if (form.Logo != null && form.Logo.Length > 0)
        {
            var ext = Path.GetExtension(form.Logo.FileName).ToLowerInvariant();
            if (new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(ext))
            {
                var dir = Path.Combine(_env.WebRootPath, "uploads", "logos");
                Directory.CreateDirectory(dir);
                var fn = $"{Guid.NewGuid()}{ext}";
                await using var s = new FileStream(Path.Combine(dir, fn), FileMode.Create);
                await form.Logo.CopyToAsync(s);
                r.Logo = $"/uploads/logos/{fn}";
            }
        }

        if (form.CoverImage != null && form.CoverImage.Length > 0)
        {
            var ext = Path.GetExtension(form.CoverImage.FileName).ToLowerInvariant();
            if (new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(ext))
            {
                var dir = Path.Combine(_env.WebRootPath, "uploads", "covers");
                Directory.CreateDirectory(dir);
                var fn = $"{Guid.NewGuid()}{ext}";
                await using var s = new FileStream(Path.Combine(dir, fn), FileMode.Create);
                await form.CoverImage.CopyToAsync(s);
                r.CoverImage = $"/uploads/covers/{fn}";
            }
        }

        if (form.OpeningHours?.Count > 0)
        {
            var dict = form.OpeningHours.ToDictionary(
                h => h.Day,
                h => new { h.Open, h.Close, h.IsClosed });
            r.OpeningHoursJson = JsonSerializer.Serialize(dict);
        }

        await _db.SaveChangesAsync();

        // re-sign in to refresh the auth cookie so the navbar avatar reflects the new logo without requiring a logout
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, user!.Name),
            new(ClaimTypes.Email, user.Email),
            new("Role", user.Role.ToString()),
            new("Avatar", r.Logo ?? "")
        };
        await HttpContext.SignInAsync("Cookies",
            new ClaimsPrincipal(new ClaimsIdentity(claims, "Cookies")));

        TempData["Success"] = "Profilul restaurantului a fost actualizat.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestDelete([FromBody] IdRequest req)
    {
        var r = await GetOwnRestaurant();
        if (r == null) return Forbid();

        var item = await _db.MenuItems.FirstOrDefaultAsync(
            m => m.Id == req.Id && m.RestaurantId == r.Id);
        if (item == null) return NotFound();

        var hasPending = await _db.MenuItemChangeRequests.AnyAsync(
            cr => cr.MenuItemId == item.Id && cr.Status == ChangeRequestStatus.Pending);
        if (hasPending)
            return BadRequest(new { error = "Există deja o cerere în așteptare pentru acest produs." });

        var proposedData = JsonSerializer.Serialize(new
        {
            name = item.Name,
            description = item.Description,
            price = item.Price,
            categoryId = item.CategoryId,
            image = item.Image
        });

        var cr = new MenuItemChangeRequest
        {
            RestaurantId = r.Id,
            MenuItemId = item.Id,
            Type = ChangeRequestType.Delete,
            ProposedDataJson = proposedData,
            Status = ChangeRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        _db.MenuItemChangeRequests.Add(cr);
        await _db.SaveChangesAsync();

        return Json(new { ok = true });
    }
}

public record CategoryNameRequest(string Name);
public record UpdateCategoryRequest(int Id, string Name);
public record IdRequest(int Id);
public record ReorderRequest(int[] Ids);

public class RestaurantProfileForm
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public List<string> SelectedCategories { get; set; } = new();
    public decimal DeliveryFee { get; set; }
    public decimal MinOrderAmount { get; set; }
    public int EstimatedDeliveryTime { get; set; }
    public string? Phone { get; set; }
    public IFormFile? Logo { get; set; }
    public IFormFile? CoverImage { get; set; }
    public List<EatUp.ViewModels.DaySchedule> OpeningHours { get; set; } = new();
}

public class ChangeRequestFormModel
{
    public ChangeRequestType Type { get; set; }
    public int? MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int CategoryId { get; set; }
    public IFormFile? Image { get; set; }
}
