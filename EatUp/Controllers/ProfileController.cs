using EatUp.Data;
using EatUp.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EatUp.Controllers;

[Authorize(Policy = "CustomerOnly")]
public class ProfileController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;

    public ProfileController(ApplicationDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();
        return View(user);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveProfile(string name, string? phone, string? address, string? city, IFormFile? avatar)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Numele este obligatoriu.";
            return RedirectToAction(nameof(Index));
        }

        user.Name = name.Trim();
        user.Phone = phone?.Trim();
        user.Address = address?.Trim();
        user.City = string.IsNullOrWhiteSpace(city) ? null : city.Trim();

        if (avatar != null && avatar.Length > 0)
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(avatar.FileName).ToLowerInvariant();
            if (allowed.Contains(ext))
            {
                var dir = Path.Combine(_env.WebRootPath, "uploads", "avatars");
                Directory.CreateDirectory(dir);
                var fileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(dir, fileName);
                await using var stream = new FileStream(filePath, FileMode.Create);
                await avatar.CopyToAsync(stream);
                user.Avatar = $"/uploads/avatars/{fileName}";
            }
        }

        await _db.SaveChangesAsync();
        await RefreshSignIn(user);

        TempData["Success"] = "Profilul a fost actualizat.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
        {
            TempData["Error"] = "Parola curentă este incorectă.";
            return RedirectToAction(nameof(Index));
        }

        if (newPassword != confirmPassword)
        {
            TempData["Error"] = "Parolele noi nu coincid.";
            return RedirectToAction(nameof(Index));
        }

        if (newPassword.Length < 6)
        {
            TempData["Error"] = "Parola nouă trebuie să aibă cel puțin 6 caractere.";
            return RedirectToAction(nameof(Index));
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Parola a fost schimbată cu succes.";
        return RedirectToAction(nameof(Index));
    }

    private async Task RefreshSignIn(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new("Role", user.Role.ToString()),
            new("Avatar", user.Avatar ?? "")
        };
        var identity = new ClaimsIdentity(claims, "Cookies");
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync("Cookies", principal,
            new AuthenticationProperties { IsPersistent = true });
    }
}
