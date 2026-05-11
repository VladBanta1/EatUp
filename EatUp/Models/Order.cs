using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EatUp.Models;

public class Order
{
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public User Customer { get; set; } = null!;

    public int RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = null!;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public int RestaurantOrderNumber { get; set; }

    public string ItemsJson { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    public decimal Subtotal { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal DeliveryFee { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Discount { get; set; } = 0;

    [Column(TypeName = "decimal(10,2)")]
    public decimal Total { get; set; }

    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    public string? StripePaymentIntentId { get; set; }

    [Required]
    public string DeliveryAddress { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? DeliveryBlock { get; set; }

    [MaxLength(50)]
    public string? DeliveryStaircase { get; set; }

    [MaxLength(100)]
    public string? DeliveryApartment { get; set; }

    public double? DeliveryLat { get; set; }
    public double? DeliveryLng { get; set; }

    public int? PromoCodeId { get; set; }
    public PromoCode? PromoCode { get; set; }

    public double? CourierLat { get; set; }
    public double? CourierLng { get; set; }

    public string? PhoneNumber { get; set; }

    public string? DeliveryComment { get; set; }

    public string? RejectionReason { get; set; }

    public DateTime? AcceptedAt { get; set; }
    public DateTime? PreparingAt { get; set; }
    public DateTime? ReadyAt { get; set; }
    public DateTime? OutForDeliveryAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? RejectedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
