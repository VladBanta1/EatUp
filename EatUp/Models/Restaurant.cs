using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EatUp.Models;

public class Restaurant
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public string Address { get; set; } = string.Empty;

    public double Lat { get; set; }
    public double Lng { get; set; }

    public string? Logo { get; set; }
    public string? CoverImage { get; set; }

    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    public string Categories { get; set; } = string.Empty;

    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    public decimal DeliveryFee { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal MinOrderAmount { get; set; }

    public int EstimatedDeliveryTime { get; set; }

    [Column(TypeName = "decimal(3,2)")]
    public decimal Rating { get; set; } = 0;

    public int TotalReviews { get; set; } = 0;

    public bool IsApproved { get; set; } = false;
    public bool IsBlocked { get; set; } = false;

    public string? RejectionReason { get; set; }

    public string? OpeningHoursJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<MenuCategory> MenuCategories { get; set; } = new List<MenuCategory>();
    public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public ICollection<MenuItemChangeRequest> ChangeRequests { get; set; } = new List<MenuItemChangeRequest>();
}
