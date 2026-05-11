using EatUp.Data;
using EatUp.Helpers;
using EatUp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EatUp.Controllers;

[Authorize(Policy = "CustomerOnly")]
public class FavoritesController : Controller
{
    private readonly ApplicationDbContext _db;

    public FavoritesController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var favorites = await _db.Favorites
            .Where(f => f.CustomerId == userId)
            .Include(f => f.Restaurant)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

        var cards = favorites.Select(f =>
        {
            var r = f.Restaurant;
            return new RestaurantCardViewModel
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
                IsOpen = OpeningHoursHelper.IsOpenNow(r.OpeningHoursJson),
                Lat = r.Lat,
                Lng = r.Lng,
                Address = r.Address,
                Description = r.Description ?? string.Empty
            };
        }).ToList();

        return View(cards);
    }
}
