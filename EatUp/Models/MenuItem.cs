using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EatUp.Models;

public class MenuItem
{
    public int Id { get; set; }

    public int RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = null!;

    public int CategoryId { get; set; }
    public MenuCategory Category { get; set; } = null!;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    public string? Image { get; set; }

    public bool IsAvailable { get; set; } = true;
    public bool IsApproved { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<MenuItemChangeRequest> ChangeRequests { get; set; } = new List<MenuItemChangeRequest>();
}
