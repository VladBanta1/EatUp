using EatUp.Data;
using EatUp.Hubs;
using EatUp.Models;
using EatUp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EatUp.Controllers;

[Route("orders")]
[Authorize(Policy = "CustomerOnly")]
public class OrdersController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IHubContext<OrderHub> _hub;

    public OrdersController(ApplicationDbContext db, IHubContext<OrderHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var orders = await _db.Orders
            .Where(o => o.CustomerId == userId)
            .Include(o => o.Restaurant)
            .Include(o => o.OrderItems)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
        return View(orders);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var order = await _db.Orders
            .Where(o => o.Id == id && o.CustomerId == userId)
            .Include(o => o.Restaurant)
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync();

        if (order == null) return NotFound();

        var existingReview = await _db.Reviews
            .FirstOrDefaultAsync(r => r.CustomerId == userId && r.RestaurantId == order.RestaurantId);

        var vm = new OrderTrackingViewModel
        {
            Order = order,
            HasReview = existingReview != null,
            ExistingReview = existingReview
        };
        return View(vm);
    }

    [HttpPost("{id:int}/cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelOrder(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var order = await _db.Orders
            .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == userId);

        if (order == null) return NotFound();
        if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Accepted)
        {
            TempData["Error"] = "Comanda nu mai poate fi anulată.";
            return RedirectToAction(nameof(Details), new { id });
        }

        order.Status = OrderStatus.Cancelled;
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var restaurantGroup = $"restaurant-{order.RestaurantId}";
        await _hub.Clients.Group(restaurantGroup)
            .SendAsync("order_cancelled", order.Id, order.RestaurantOrderNumber);
        await _hub.Clients.Group(restaurantGroup)
            .SendAsync("order_updated", order.Id, "Cancelled");

        TempData["Success"] = "Comanda a fost anulată.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/review")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitReview(int id, int rating, string? comment)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var order = await _db.Orders
            .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == userId);

        if (order == null) return NotFound();
        if (order.Status != OrderStatus.Delivered) return BadRequest();

        var alreadyReviewed = await _db.Reviews
            .AnyAsync(r => r.CustomerId == userId && r.RestaurantId == order.RestaurantId);
        if (alreadyReviewed)
        {
            TempData["Error"] = "Ai lăsat deja o recenzie pentru acest restaurant.";
            return RedirectToAction(nameof(Details), new { id });
        }

        rating = Math.Clamp(rating, 1, 5);

        var review = new Review
        {
            CustomerId = userId,
            RestaurantId = order.RestaurantId,
            OrderId = id,
            Rating = rating,
            Comment = comment,
            CreatedAt = DateTime.UtcNow
        };
        _db.Reviews.Add(review);
        await _db.SaveChangesAsync();

        var restaurant = await _db.Restaurants.FindAsync(order.RestaurantId);
        if (restaurant != null)
        {
            var stats = await _db.Reviews
                .Where(r => r.RestaurantId == order.RestaurantId)
                .GroupBy(r => r.RestaurantId)
                .Select(g => new { Count = g.Count(), Avg = g.Average(r => (double)r.Rating) })
                .FirstOrDefaultAsync();

            if (stats != null)
            {
                restaurant.TotalReviews = stats.Count;
                restaurant.Rating = (decimal)Math.Round(stats.Avg, 2);
                await _db.SaveChangesAsync();
            }
        }

        TempData["Success"] = "Recenzia a fost salvată. Mulțumim!";
        return RedirectToAction(nameof(Details), new { id });
    }
}
