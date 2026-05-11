using EatUp.Models;

namespace EatUp.ViewModels;

public class RestaurantStatsViewModel
{
    public Restaurant Restaurant { get; set; } = null!;
    public int TotalOrders30d { get; set; }
    public decimal TotalRevenue30d { get; set; }
    public int TotalOrders7d { get; set; }
    public decimal TotalRevenue7d { get; set; }
    public string ChartLabels7Json { get; set; } = "[]";
    public string ChartOrders7Json { get; set; } = "[]";
    public string ChartRevenue7Json { get; set; } = "[]";
    public string ChartLabels30Json { get; set; } = "[]";
    public string ChartOrders30Json { get; set; } = "[]";
    public string ChartRevenue30Json { get; set; } = "[]";
    public List<TopItemStat> TopItems { get; set; } = new();
}

public class TopItemStat
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Revenue { get; set; }
}
