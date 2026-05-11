using EatUp.Models;

namespace EatUp.ViewModels;

public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int TotalRestaurants { get; set; }
    public int OrdersToday { get; set; }
    public decimal RevenueToday { get; set; }
    public int PendingRestaurants { get; set; }
    public int PendingChangeRequests { get; set; }
    public List<Order> RecentOrders { get; set; } = new();
    public string ChartLabelsJson { get; set; } = "[]";
    public string ChartOrdersJson { get; set; } = "[]";
    public string ChartRevenueJson { get; set; } = "[]";
}
