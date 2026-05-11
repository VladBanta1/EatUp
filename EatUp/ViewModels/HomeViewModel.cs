namespace EatUp.ViewModels;

public class RestaurantCardViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? CoverImage { get; set; }
    public string? Logo { get; set; }
    public decimal Rating { get; set; }
    public int TotalReviews { get; set; }
    public int EstimatedDeliveryTime { get; set; }
    public decimal DeliveryFee { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Categories { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public bool IsOpen { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class HomeViewModel
{
    public List<RestaurantCardViewModel> Restaurants { get; set; } = new();
    public string? Query { get; set; }
    public string? Category { get; set; }
    public string? City { get; set; }
    public string Sort { get; set; } = "rating";
    public bool OpenNow { get; set; }
    public bool FreeDelivery { get; set; }
    public bool MinRating { get; set; }
}
