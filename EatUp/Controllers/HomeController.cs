using EatUp.Data;
using EatUp.Helpers;
using EatUp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;
using EatUp.Models;

namespace EatUp.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;

    public HomeController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(
        string? q,
        string? category,
        string? city,
        string sort = "fastest",
        bool openNow = false,
        bool freeDelivery = false,
        bool minRating = false)
    {
        // city resolution priority: explicit filter param > customer's saved city > null (show all)
        string? effectiveCity = city;
        int? currentUserId = null;
        if (User.Identity?.IsAuthenticated == true && User.FindFirstValue("Role") == "Customer")
        {
            currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (effectiveCity == null)
            {
                var user = await _db.Users.FindAsync(currentUserId);
                effectiveCity = string.IsNullOrWhiteSpace(user?.City) ? null : user.City;
            }
        }
        if (effectiveCity == "Toate") effectiveCity = null;

        var baseQuery = _db.Restaurants
            .Where(r => r.IsApproved && !r.IsBlocked)
            .Where(r => _db.MenuItems.Any(m => m.RestaurantId == r.Id && m.IsApproved));

        if (!string.IsNullOrWhiteSpace(effectiveCity))
            baseQuery = baseQuery.Where(r => r.City == effectiveCity);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var matchIds = await _db.MenuItems
                .Where(mi => mi.IsApproved && mi.IsAvailable && EF.Functions.Like(mi.Name, $"%{q}%"))
                .Select(mi => mi.RestaurantId)
                .Distinct()
                .ToListAsync();

            baseQuery = baseQuery.Where(r => EF.Functions.Like(r.Name, $"%{q}%") || matchIds.Contains(r.Id));
        }

        if (freeDelivery)
            baseQuery = baseQuery.Where(r => r.DeliveryFee == 0);

        if (minRating)
            baseQuery = baseQuery.Where(r => r.Rating >= 4);

        // cheapest and recomandate are sorted in-memory; others are SQL-ordered
        var orderedQuery = sort switch
        {
            "popular"     => baseQuery.OrderByDescending(r => r.TotalReviews),
            "recomandate" => baseQuery.OrderByDescending(r => r.Rating),
            "cheapest"    => baseQuery.OrderByDescending(r => r.Rating),
            _             => baseQuery.OrderBy(r => r.EstimatedDeliveryTime)
        };

        var restaurants = await orderedQuery.ToListAsync();
        var restaurantIds = restaurants.Select(r => r.Id).ToList();

        var allMenuItems = await _db.MenuItems
            .Include(mi => mi.Category)
            .Where(mi => restaurantIds.Contains(mi.RestaurantId) && mi.IsApproved && mi.IsAvailable)
            .ToListAsync();

        var itemsByRestaurant = allMenuItems
            .GroupBy(mi => mi.RestaurantId)
            .ToDictionary(g => g.Key, g => g.ToList());

        Dictionary<int, int> recommendationScore = new();
        if (sort == "recomandate" && currentUserId.HasValue)
        {
            var pastOrders = await _db.Orders
                .Where(o => o.CustomerId == currentUserId.Value && o.Status == OrderStatus.Delivered)
                .ToListAsync();

            var freqByRestaurant = pastOrders
                .GroupBy(o => o.RestaurantId)
                .ToDictionary(g => g.Key, g => g.Count());

            var pastOrderIds = pastOrders.Select(o => o.Id).ToList();
            var pastItems = await _db.OrderItems
                .Include(oi => oi.MenuItem).ThenInclude(mi => mi.Category)
                .Where(oi => pastOrderIds.Contains(oi.OrderId))
                .ToListAsync();

            var preferredCats = pastItems
                .Select(oi => oi.MenuItem?.Category?.Name ?? "")
                .Where(n => !string.IsNullOrEmpty(n))
                .GroupBy(n => n)
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var r in restaurants)
            {
                int score = 0;
                if (freqByRestaurant.TryGetValue(r.Id, out var freq))
                    score += freq * 3;

                var rCats = (string.IsNullOrEmpty(r.Categories) ? r.Category : r.Categories)
                    .Split(',').Select(c => c.Trim());
                foreach (var rc in rCats)
                {
                    if (preferredCats.Keys.Any(pc =>
                        pc.Contains(rc, StringComparison.OrdinalIgnoreCase) ||
                        rc.Contains(pc, StringComparison.OrdinalIgnoreCase)))
                        score += 1;
                }
                recommendationScore[r.Id] = score;
            }
        }

        // open-now and category filters run in memory because IsOpenNow uses local time and category matching requires string splitting
        var cards = restaurants
            .Select(r => new RestaurantCardViewModel
            {
                Id = r.Id,
                Name = r.Name,
                CoverImage = r.CoverImage,
                Logo = r.Logo,
                Rating = r.Rating,
                TotalReviews = r.TotalReviews,
                EstimatedDeliveryTime = r.EstimatedDeliveryTime,
                DeliveryFee = r.DeliveryFee,
                Category = r.Category,
                Categories = r.Categories,
                City = r.City,
                IsOpen = OpeningHoursHelper.IsOpenNow(r.OpeningHoursJson),
                Lat = r.Lat,
                Lng = r.Lng,
                Address = r.Address,
                Description = r.Description ?? string.Empty,
                AvgMainPrice = AvgMain(r.Id, itemsByRestaurant)
            })
            .Where(r => !openNow || r.IsOpen)
            .Where(r => string.IsNullOrEmpty(category) || category == "All" ||
                        (string.IsNullOrEmpty(r.Categories) ? r.Category : r.Categories)
                            .Split(',').Select(c => c.Trim())
                            .Contains(category, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (sort == "cheapest")
            cards = cards.OrderBy(r => r.AvgMainPrice == 0 ? decimal.MaxValue : r.AvgMainPrice).ToList();
        else if (sort == "recomandate" && recommendationScore.Count > 0)
            cards = cards.OrderByDescending(r => recommendationScore.GetValueOrDefault(r.Id, 0))
                         .ThenByDescending(r => r.Rating)
                         .ToList();

        return View(new HomeViewModel
        {
            Restaurants = cards,
            Query = q,
            Category = category,
            City = city ?? effectiveCity,
            Sort = sort,
            OpenNow = openNow,
            FreeDelivery = freeDelivery,
            MinRating = minRating
        });
    }

    private static readonly string[] DrinkKeywords   = { "băutură", "bautura", "băuturi", "bauturi", "suc", "sucuri", "cafea", "apă", "apa", "ceai", "bere", "vin", "cocktail", "limonadă", "limonada" };
    private static readonly string[] DessertKeywords = { "desert", "deserturi", "dulciuri", "dulce", "prăjituri", "prajituri", "tort", "inghetata", "înghețată" };
    private static readonly string[] SideKeywords    = { "garnituri", "garnitură", "garnitura", "salate", "salată", "salata", "supe", "supă", "supa", "gustări", "aperitiv" };

    private static bool HasKeyword(string name, string[] keywords)
        => keywords.Any(k => name.Contains(k, StringComparison.OrdinalIgnoreCase));

    private static decimal AvgMain(int restaurantId,
        Dictionary<int, List<Models.MenuItem>> itemsByRestaurant)
    {
        if (!itemsByRestaurant.TryGetValue(restaurantId, out var items) || items.Count == 0)
            return 0;

        var mains = items.Where(i =>
            !HasKeyword(i.Category?.Name ?? "", DrinkKeywords) &&
            !HasKeyword(i.Category?.Name ?? "", DessertKeywords) &&
            !HasKeyword(i.Category?.Name ?? "", SideKeywords)).ToList();

        if (mains.Count == 0) return 0;
        return mains.Average(i => i.Price);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
        => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult HttpError(int code) => View(code);
}
