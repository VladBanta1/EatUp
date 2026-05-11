namespace EatUp.ViewModels;

using EatUp.Models;

public class MenuManagementViewModel
{
    public Restaurant Restaurant { get; set; } = null!;
    public List<MenuCategoryWithItems> Categories { get; set; } = new();
    public int PendingRequestCount { get; set; }
}

public class MenuCategoryWithItems
{
    public MenuCategory Category { get; set; } = null!;
    public List<MenuItemRow> Items { get; set; } = new();
}

public class MenuItemRow
{
    public MenuItem Item { get; set; } = null!;
    public bool HasPending { get; set; }
    public ChangeRequestType? PendingType { get; set; }
}
