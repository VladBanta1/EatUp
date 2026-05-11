using EatUp.Models;

namespace EatUp.ViewModels;

public class CategoryWithItems
{
    public MenuCategory Category { get; set; } = null!;
    public List<MenuItem> Items { get; set; } = new();
}

public class RestaurantDetailViewModel
{
    public Restaurant Restaurant { get; set; } = null!;
    public List<CategoryWithItems> MenuSections { get; set; } = new();
    public bool IsOpen { get; set; }
    public bool IsFavorited { get; set; }
    public List<Review> Reviews { get; set; } = new();
    public Cart? CurrentCart { get; set; }
    public bool IsCityMismatch { get; set; }
}
