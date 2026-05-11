using EatUp.Models;

namespace EatUp.ViewModels;

public class OrderTrackingViewModel
{
    public Order Order { get; set; } = null!;
    public bool HasReview { get; set; }
    public Review? ExistingReview { get; set; }
}
