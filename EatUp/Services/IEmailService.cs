using EatUp.Models;

namespace EatUp.Services;

public interface IEmailService
{
    void SendOrderPlaced(string email, string name, int orderId, string restaurantName,
                         string itemsSummary, decimal total, int estimatedMinutes);
    void SendOrderAccepted(string email, string name, int orderId, string restaurantName);
    void SendOrderPreparing(string email, string name, int orderId, string restaurantName);
    void SendOrderOutForDelivery(string email, string name, int orderId, string restaurantName);
    void SendOrderDelivered(string email, string name, int orderId, string restaurantName);
    void SendOrderRejected(string email, string name, int orderId, string restaurantName, string? reason);

    void SendRestaurantApproved(string email, string name, string restaurantName);
    void SendRestaurantRejected(string email, string name, string restaurantName, string reason);
    void SendChangeApproved(string email, string name, string restaurantName, string itemName, string changeType);
    void SendChangeRejected(string email, string name, string restaurantName, string itemName, string? adminNote);
}
