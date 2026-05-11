using EatUp.Data;
using EatUp.Helpers;
using EatUp.Models;
using EatUp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace EatUp.Controllers;

[Route("restaurants")]
public class RestaurantsController : Controller
{
    private readonly ApplicationDbContext _db;

    public RestaurantsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(int id)
    {
        var restaurant = await _db.Restaurants
            .Include(r => r.MenuCategories.OrderBy(c => c.DisplayOrder))
            .Include(r => r.Reviews.OrderByDescending(rv => rv.CreatedAt))
                .ThenInclude(rv => rv.Customer)
            .FirstOrDefaultAsync(r => r.Id == id && r.IsApproved && !r.IsBlocked);

        if (restaurant == null) return NotFound();

        var menuItems = await _db.MenuItems
            .Where(mi => mi.RestaurantId == id && mi.IsApproved && mi.IsAvailable)
            .ToListAsync();

        bool isFavorited = false;
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            isFavorited = await _db.Favorites
                .AnyAsync(f => f.CustomerId == userId && f.RestaurantId == id);
        }

        Cart? currentCart = null;
        var cartJson = HttpContext.Session.GetString("Cart");
        if (!string.IsNullOrEmpty(cartJson))
            currentCart = JsonSerializer.Deserialize<Cart>(cartJson);

        bool isCityMismatch = false;
        if (User.Identity?.IsAuthenticated == true && User.FindFirstValue("Role") == "Customer"
            && !string.IsNullOrWhiteSpace(restaurant.City))
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var customer = await _db.Users.FindAsync(userId);
            if (!string.IsNullOrWhiteSpace(customer?.City) && customer.City != restaurant.City)
                isCityMismatch = true;
        }

        var vm = new RestaurantDetailViewModel
        {
            Restaurant = restaurant,
            IsOpen = OpeningHoursHelper.IsOpenNow(restaurant.OpeningHoursJson),
            IsFavorited = isFavorited,
            IsCityMismatch = isCityMismatch,
            MenuSections = restaurant.MenuCategories
                .Select(c => new CategoryWithItems
                {
                    Category = c,
                    Items = menuItems.Where(mi => mi.CategoryId == c.Id).ToList()
                })
                .Where(s => s.Items.Count > 0)
                .ToList(),
            Reviews = restaurant.Reviews.ToList(),
            CurrentCart = currentCart
        };

        return View(vm);
    }

    [HttpPost("favorite/{id:int}")]
    [Authorize(Policy = "CustomerOnly")]
    public async Task<IActionResult> ToggleFavorite(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var existing = await _db.Favorites
            .FirstOrDefaultAsync(f => f.CustomerId == userId && f.RestaurantId == id);

        if (existing != null)
        {
            _db.Favorites.Remove(existing);
            await _db.SaveChangesAsync();
            return Json(new { favorited = false });
        }

        _db.Favorites.Add(new Favorite { CustomerId = userId, RestaurantId = id });
        await _db.SaveChangesAsync();
        return Json(new { favorited = true });
    }
}
