using EatUp.Models;

namespace EatUp.ViewModels;

public class CartViewModel
{
    public Cart Cart { get; set; } = new();
    public decimal Discount { get; set; }
    public int? PromoCodeId { get; set; }
    public string? PromoDescription { get; set; }
    public string StripePublishableKey { get; set; } = string.Empty;
    public decimal MinOrderAmount { get; set; }
    public string DefaultAddress { get; set; } = string.Empty;
    public string DefaultPhone { get; set; } = string.Empty;
    public decimal Total => Cart.Subtotal + Cart.DeliveryFee - Discount;

    public double MapCenterLat { get; set; } = 44.4268;
    public double MapCenterLng { get; set; } = 26.1025;
}
