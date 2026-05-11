using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EatUp.Models;

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public int MenuItemId { get; set; }
    public MenuItem MenuItem { get; set; } = null!;

    [Required, MaxLength(200)]
    public string NameSnapshot { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    public decimal PriceSnapshot { get; set; }

    public int Quantity { get; set; }
}
