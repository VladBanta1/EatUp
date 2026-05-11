namespace EatUp.Models;

public class CartItem
{
    public int MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

public class Cart
{
    public int RestaurantId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public decimal DeliveryFee { get; set; }
    public List<CartItem> Items { get; set; } = new();

    [System.Text.Json.Serialization.JsonIgnore]
    public decimal Subtotal => Items.Sum(i => i.Price * i.Quantity);

    [System.Text.Json.Serialization.JsonIgnore]
    public int TotalItems => Items.Sum(i => i.Quantity);
}
