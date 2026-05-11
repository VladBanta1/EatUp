namespace EatUp.Models;

public class Favorite
{
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public User Customer { get; set; } = null!;

    public int RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
