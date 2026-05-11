using EatUp.Data;
using EatUp.Models;
using EatUp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace EatUp.Hubs;

[Authorize]
public class OrderHub : Hub
{
    private readonly ApplicationDbContext _db;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<OrderHub> _hubContext;
    private readonly IEmailService _email;

    private static readonly Dictionary<OrderStatus, OrderStatus> NextStatus = new()
    {
        [OrderStatus.Pending]        = OrderStatus.Accepted,
        [OrderStatus.Accepted]       = OrderStatus.Preparing,
        [OrderStatus.Preparing]      = OrderStatus.ReadyForPickup,
        [OrderStatus.ReadyForPickup] = OrderStatus.OutForDelivery,
        [OrderStatus.OutForDelivery] = OrderStatus.Delivered,
    };

    public OrderHub(ApplicationDbContext db, IServiceScopeFactory scopeFactory, IHubContext<OrderHub> hubContext, IEmailService email)
    {
        _db = db;
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _email = email;
    }

    public async Task JoinRestaurantGroup(int restaurantId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"restaurant-{restaurantId}");

    public async Task JoinOrderGroup(int orderId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"order-{orderId}");

    public async Task JoinCustomerGroup(int customerId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"customer-{customerId}");

    public async Task UpdateOrderStatus(int orderId, string newStatus)
    {
        if (!int.TryParse(Context.UserIdentifier, out int userId)) return;
        if (!Enum.TryParse<OrderStatus>(newStatus, out var status)) return;

        var order = await _db.Orders
            .Include(o => o.Restaurant)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null || order.Restaurant.UserId != userId) return;
        if (!NextStatus.TryGetValue(order.Status, out var expected) || expected != status) return;

        var now = DateTime.UtcNow;
        order.Status = status;
        order.UpdatedAt = now;
        if (status == OrderStatus.Accepted)            order.AcceptedAt       = now;
        else if (status == OrderStatus.Preparing)      order.PreparingAt      = now;
        else if (status == OrderStatus.ReadyForPickup) order.ReadyAt          = now;
        else if (status == OrderStatus.OutForDelivery) order.OutForDeliveryAt = now;
        else if (status == OrderStatus.Delivered)      order.DeliveredAt      = now;
        await _db.SaveChangesAsync();

        await _hubContext.Clients.Group($"order-{orderId}")
            .SendAsync("order_status_changed", orderId, newStatus);

        await _hubContext.Clients.Group($"customer-{order.CustomerId}")
            .SendAsync("order_status_changed", orderId, newStatus);

        await _hubContext.Clients.Group($"restaurant-{order.RestaurantId}")
            .SendAsync("order_updated", orderId, newStatus);

        if (order.Customer != null)
        {
            if (status == OrderStatus.Accepted)
                _email.SendOrderAccepted(order.Customer.Email, order.Customer.Name, orderId, order.Restaurant.Name);
            else if (status == OrderStatus.Preparing)
                _email.SendOrderPreparing(order.Customer.Email, order.Customer.Name, orderId, order.Restaurant.Name);
            else if (status == OrderStatus.OutForDelivery)
                _email.SendOrderOutForDelivery(order.Customer.Email, order.Customer.Name, orderId, order.Restaurant.Name);
            else if (status == OrderStatus.Delivered)
                _email.SendOrderDelivered(order.Customer.Email, order.Customer.Name, orderId, order.Restaurant.Name);
        }

        if (status == OrderStatus.OutForDelivery)
        {
            var fromLat = order.Restaurant.Lat;
            var fromLng = order.Restaurant.Lng;
            var toLat   = order.DeliveryLat;
            var toLng   = order.DeliveryLng;
            var rId     = order.RestaurantId;

            // only simulate courier movement when the restaurant has real coordinates and the customer picked a delivery location; guards against (0,0) restaurant coords and null delivery coords
            if ((fromLat != 0 || fromLng != 0) && toLat.HasValue && toLng.HasValue)
            {
                _ = Task.Run(async () =>
                {
                    try { await SimulateCourierAsync(orderId, rId, fromLat, fromLng, toLat, toLng); }
                    catch { /* simulation errors are non-fatal; marker freezes at last saved position */ }
                });
            }
        }
    }

    public async Task RejectOrder(int orderId, string reason)
    {
        if (!int.TryParse(Context.UserIdentifier, out int userId)) return;

        var order = await _db.Orders
            .Include(o => o.Restaurant)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null || order.Restaurant.UserId != userId) return;
        if (order.Status != OrderStatus.Pending) return;

        order.Status = OrderStatus.Rejected;
        order.RejectionReason = reason;
        order.RejectedAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _hubContext.Clients.Group($"order-{orderId}")
            .SendAsync("order_status_changed", orderId, "Rejected");

        await _hubContext.Clients.Group($"customer-{order.CustomerId}")
            .SendAsync("order_status_changed", orderId, "Rejected");

        await _hubContext.Clients.Group($"restaurant-{order.RestaurantId}")
            .SendAsync("order_updated", orderId, "Rejected", reason);

        if (order.Customer != null)
            _email.SendOrderRejected(order.Customer.Email, order.Customer.Name, orderId,
                order.Restaurant.Name, reason);
    }

    // 90 steps × 1 s, smoothstep ease-in-out interpolation; auto-delivers at the end
    private async Task SimulateCourierAsync(
        int orderId, int restaurantId,
        double fromLat, double fromLng,
        double? toLat, double? toLng)
    {
        const int totalSteps = 90;
        double destLat = toLat ?? fromLat;
        double destLng = toLng ?? fromLng;

        for (int step = 1; step <= totalSteps; step++)
        {
            await Task.Delay(1000);

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var order = await db.Orders.FindAsync(orderId);
            if (order == null || order.Status != OrderStatus.OutForDelivery) return;

            double t   = (double)step / totalSteps;
            t = t * t * (3 - 2 * t);              // smoothstep ease-in-out
            double lat = fromLat + (destLat - fromLat) * t;
            double lng = fromLng + (destLng - fromLng) * t;

            order.CourierLat = lat;
            order.CourierLng = lng;
            await db.SaveChangesAsync();

            await _hubContext.Clients.Group($"order-{orderId}")
                .SendAsync("courier_location", lat, lng);
        }

        // courier reached destination — auto-mark order as Delivered
        using var finalScope = _scopeFactory.CreateScope();
        var finalDb = finalScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var finalOrder = await finalDb.Orders
            .Include(o => o.Customer)
            .Include(o => o.Restaurant)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (finalOrder == null || finalOrder.Status != OrderStatus.OutForDelivery) return;

        var now = DateTime.UtcNow;
        finalOrder.Status = OrderStatus.Delivered;
        finalOrder.DeliveredAt = now;
        finalOrder.UpdatedAt = now;
        await finalDb.SaveChangesAsync();

        await _hubContext.Clients.Group($"order-{orderId}")
            .SendAsync("order_status_changed", orderId, "Delivered");
        await _hubContext.Clients.Group($"customer-{finalOrder.CustomerId}")
            .SendAsync("order_status_changed", orderId, "Delivered");
        await _hubContext.Clients.Group($"restaurant-{finalOrder.RestaurantId}")
            .SendAsync("order_updated", orderId, "Delivered");

        if (finalOrder.Customer != null)
            _email.SendOrderDelivered(finalOrder.Customer.Email, finalOrder.Customer.Name,
                orderId, finalOrder.Restaurant?.Name ?? "");
    }
}
