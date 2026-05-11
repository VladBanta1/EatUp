using EatUp.Data;
using EatUp.Models;
using EatUp.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace EatUp.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;

    public AccountController(ApplicationDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        if (await _db.Users.AnyAsync(u => u.Email == model.Email))
        {
            ModelState.AddModelError("Email", "Există deja un cont cu acest email.");
            return View(model);
        }

        var user = new User
        {
            Name = model.Name,
            Email = model.Email,
            Phone = model.Phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            Role = UserRole.Customer,
            City = string.IsNullOrWhiteSpace(model.City) ? null : model.City.Trim()
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        await SignInAsync(user);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult RegisterRestaurant()
        => View(new RegisterRestaurantViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterRestaurant(RegisterRestaurantViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        if (await _db.Users.AnyAsync(u => u.Email == model.Email))
        {
            ModelState.AddModelError("Email", "Există deja un cont cu acest email.");
            return View(model);
        }

        var user = new User
        {
            Name = model.OwnerName,
            Email = model.Email,
            Phone = model.Phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            Role = UserRole.Restaurant
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var openingHoursJson = JsonSerializer.Serialize(
            model.OpeningHours.ToDictionary(
                d => d.Day,
                d => d.IsClosed
                    ? (object)new { IsClosed = true }
                    : new { Open = d.Open, Close = d.Close }));

        var selectedCats = model.SelectedCategories.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
        if (selectedCats.Count == 0)
        {
            ModelState.AddModelError("SelectedCategories", "Selectează cel puțin o categorie.");
            return View(model);
        }

        var restaurant = new Restaurant
        {
            UserId = user.Id,
            Name = model.RestaurantName,
            Description = model.Description,
            Address = model.Address,
            Lat = model.Lat,
            Lng = model.Lng,
            Category = selectedCats[0],
            Categories = string.Join(",", selectedCats),
            DeliveryFee = model.DeliveryFee,
            MinOrderAmount = model.MinOrderAmount,
            EstimatedDeliveryTime = model.EstimatedDeliveryTime,
            IsApproved = false,
            OpeningHoursJson = openingHoursJson,
            Logo = await SaveFileAsync(model.Logo, "restaurants"),
            CoverImage = await SaveFileAsync(model.CoverImage, "restaurants")
        };
        _db.Restaurants.Add(restaurant);
        await _db.SaveChangesAsync();

        await SignInAsync(user);
        return RedirectToAction("Pending");
    }

    [HttpGet]
    public IActionResult Pending() => View();

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Email sau parolă incorectă.");
            return View(model);
        }

        if (user.IsBlocked)
            return RedirectToAction("Suspended");

        if (user.Role == UserRole.Restaurant)
        {
            var restaurant = await _db.Restaurants.FirstOrDefaultAsync(r => r.UserId == user.Id);
            if (restaurant?.IsBlocked == true)
                return RedirectToAction("RestaurantSuspended");
        }

        await SignInAsync(user, model.RememberMe);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return user.Role switch
        {
            UserRole.Admin => RedirectToAction("Index", "Admin"),
            UserRole.Restaurant => RedirectToAction("Dashboard", "Restaurant"),
            _ => RedirectToAction("Index", "Home")
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("Cookies");
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Suspended() => View();

    [HttpGet]
    public IActionResult RestaurantSuspended() => View();

    [HttpGet]
    public IActionResult AccessDenied() => View();

    private async Task SignInAsync(User user, bool persistent = false)
    {
        string avatarValue = user.Avatar ?? "";
        if (user.Role == UserRole.Restaurant)
        {
            var rest = await _db.Restaurants.FirstOrDefaultAsync(r => r.UserId == user.Id);
            avatarValue = rest?.Logo ?? "";
        }
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new("Role", user.Role.ToString()),
            new("Avatar", avatarValue)
        };
        var identity = new ClaimsIdentity(claims, "Cookies");
        var principal = new ClaimsPrincipal(identity);
        var props = new AuthenticationProperties { IsPersistent = persistent };
        await HttpContext.SignInAsync("Cookies", principal, props);
    }

    private async Task<string?> SaveFileAsync(IFormFile? file, string folder)
    {
        if (file == null || file.Length == 0) return null;

        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext)) return null;

        var dir = Path.Combine(_env.WebRootPath, "uploads", folder);
        Directory.CreateDirectory(dir);

        var fileName = $"{Guid.NewGuid()}{ext}";
        var path = Path.Combine(dir, fileName);
        await using var stream = new FileStream(path, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/uploads/{folder}/{fileName}";
    }
}
