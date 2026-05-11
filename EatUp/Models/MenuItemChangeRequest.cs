namespace EatUp.Models;

public class MenuItemChangeRequest
{
    public int Id { get; set; }

    public int? MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }

    public int RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = null!;

    public ChangeRequestType Type { get; set; }

    public string ProposedDataJson { get; set; } = string.Empty;

    public ChangeRequestStatus Status { get; set; } = ChangeRequestStatus.Pending;

    public string? AdminNote { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
}
