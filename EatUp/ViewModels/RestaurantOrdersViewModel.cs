using EatUp.Models;

namespace EatUp.ViewModels;

public class RestaurantOrdersViewModel
{
    public Restaurant Restaurant { get; set; } = null!;
    public List<Order> ActiveOrders { get; set; } = new();
    public List<Order> CompletedOrders { get; set; } = new();

    private static readonly OrderStatus[] ActiveStatuses =
    [
        OrderStatus.Pending,
        OrderStatus.Accepted,
        OrderStatus.Preparing,
        OrderStatus.ReadyForPickup,
        OrderStatus.OutForDelivery
    ];

    public static bool IsActive(OrderStatus s) => ActiveStatuses.Contains(s);
}
