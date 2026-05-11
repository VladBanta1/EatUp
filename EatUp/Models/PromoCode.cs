using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EatUp.Models;

public class PromoCode
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DiscountType DiscountType { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal DiscountValue { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal MinOrderAmount { get; set; } = 0;

    public int MaxUses { get; set; }
    public int UsedCount { get; set; } = 0;

    public DateTime? ExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
