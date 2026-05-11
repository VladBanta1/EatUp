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
        string sort = "rating",
        bool openNow = false,
        bool freeDelivery = false,
        bool minRating = false)
    {
        // city resolution priority: explicit filter param > customer's saved city > null (show all)
        string? effectiveCity = city;
        if (effectiveCity == null && User.Identity?.IsAuthenticated == true
            && User.FindFirstValue("Role") == "Customer")
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _db.Users.FindAsync(userId);
            effectiveCity = string.IsNullOrWhiteSpace(user?.City) ? null : user.City;
        }
        if (effectiveCity == "Toate") effectiveCity = null;

        var query = _db.Restaurants
            .Where(r => r.IsApproved && !r.IsBlocked)
            .Where(r => _db.MenuItems.Any(m => m.RestaurantId == r.Id && m.IsApproved));

        if (!string.IsNullOrWhiteSpace(effectiveCity))
            query = query.Where(r => r.City == effectiveCity);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var matchIds = await _db.MenuItems
                .Where(mi => mi.IsApproved && mi.IsAvailable && EF.Functions.Like(mi.Name, $"%{q}%"))
                .Select(mi => mi.RestaurantId)
                .Distinct()
                .ToListAsync();

            query = query.Where(r => EF.Functions.Like(r.Name, $"%{q}%") || matchIds.Contains(r.Id));
        }

        if (freeDelivery)
            query = query.Where(r => r.DeliveryFee == 0);

        if (minRating)
            query = query.Where(r => r.Rating >= 4);

        query = sort switch
        {
            "fastest" => query.OrderBy(r => r.EstimatedDeliveryTime),
            "popular" => query.OrderByDescending(r => r.TotalReviews),
            _ => query.OrderByDescending(r => r.Rating)
        };

        var restaurants = await query.ToListAsync();

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
                Description = r.Description ?? string.Empty
            })
            .Where(r => !openNow || r.IsOpen)
            .Where(r => string.IsNullOrEmpty(category) || category == "All" ||
                        (string.IsNullOrEmpty(r.Categories) ? r.Category : r.Categories)
                            .Split(',').Select(c => c.Trim())
                            .Contains(category, StringComparer.OrdinalIgnoreCase))
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

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
        => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult HttpError(int code) => View(code);
}
