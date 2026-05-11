namespace EatUp.Models;

public enum UserRole
{
    Customer,
    Restaurant,
    Admin
}

public enum OrderStatus
{
    Pending,
    Accepted,
    Preparing,
    ReadyForPickup,
    OutForDelivery,
    Delivered,
    Rejected,
    Cancelled
}

public enum PaymentMethod
{
    Card,
    Cash
}

public enum PaymentStatus
{
    Pending,
    Paid,
    Failed
}

public enum DiscountType
{
    Percentage,
    Fixed
}

public enum ChangeRequestType
{
    Create,
    Update,
    Delete
}

public enum ChangeRequestStatus
{
    Pending,
    Approved,
    Rejected
}
