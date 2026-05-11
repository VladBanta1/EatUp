using System.ComponentModel.DataAnnotations;

namespace EatUp.Models;

public class User
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    public UserRole Role { get; set; } = UserRole.Customer;

    public string? Avatar { get; set; }

    public string? Address { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    public double? Lat { get; set; }

    public double? Lng { get; set; }

    public bool IsBlocked { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Restaurant? Restaurant { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
}
