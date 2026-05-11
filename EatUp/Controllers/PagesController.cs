using EatUp.Data;
using EatUp.Helpers;
using EatUp.Models;
using EatUp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EatUp.Controllers;

public class PagesController : Controller
{
    private readonly ApplicationDbContext _db;

    public PagesController(ApplicationDbContext db)
    {
        _db = db;
    }

    [Route("about")]
    public IActionResult About() => View();

    [Route("how-it-works")]
    public IActionResult HowItWorks() => View();

    [Route("become-partner")]
    public IActionResult BecomePartner() => View();

    [Route("faq")]
    public IActionResult Faq() => View();

    [Route("contact")]
    [HttpGet]
    public IActionResult Contact() => View();

    [Route("contact")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ContactPost([FromBody] ContactFormRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.Email)
            || string.IsNullOrWhiteSpace(req.Subject) || string.IsNullOrWhiteSpace(req.Message))
            return BadRequest(new { error = "Toate câmpurile sunt obligatorii." });

        _db.ContactMessages.Add(new ContactMessage
        {
            Name    = req.Name.Trim(),
            Email   = req.Email.Trim(),
            Subject = req.Subject.Trim(),
            Message = req.Message.Trim(),
            SentAt  = DateTime.UtcNow,
            IsRead  = false
        });
        await _db.SaveChangesAsync();

        return Json(new { ok = true });
    }

    [Route("terms")]
    public IActionResult Terms() => View();

    [Route("privacy")]
    public IActionResult Privacy() => View();

    [Route("partner-restaurants")]
    public async Task<IActionResult> PartnerRestaurants()
    {
        var restaurants = await _db.Restaurants
            .Where(r => r.IsApproved && !r.IsBlocked)
            .Where(r => _db.MenuItems.Any(m => m.RestaurantId == r.Id && m.IsApproved))
            .OrderByDescending(r => r.Rating)
            .ToListAsync();

        var cards = restaurants.Select(r => new RestaurantCardViewModel
        {
            Id                    = r.Id,
            Name                  = r.Name,
            CoverImage            = r.CoverImage,
            Logo                  = r.Logo,
            Rating                = r.Rating,
            TotalReviews          = r.TotalReviews,
            EstimatedDeliveryTime = r.EstimatedDeliveryTime,
            DeliveryFee           = r.DeliveryFee,
            Category              = r.Category,
            Categories            = r.Categories,
            City                  = r.City,
            IsOpen                = OpeningHoursHelper.IsOpenNow(r.OpeningHoursJson),
            Lat                   = r.Lat,
            Lng                   = r.Lng,
            Address               = r.Address,
            Description           = r.Description ?? string.Empty
        }).ToList();

        return View(cards);
    }
}

public record ContactFormRequest(string Name, string Email, string Subject, string Message);
