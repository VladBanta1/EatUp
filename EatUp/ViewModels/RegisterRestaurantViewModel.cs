using System.ComponentModel.DataAnnotations;

namespace EatUp.ViewModels;

public class DaySchedule
{
    public string Day { get; set; } = string.Empty;
    public bool IsClosed { get; set; } = false;
    public string Open { get; set; } = "10:00";
    public string Close { get; set; } = "22:00";
}

public class RegisterRestaurantViewModel
{
    [Required(ErrorMessage = "Numele este obligatoriu")]
    [MaxLength(100)]
    public string OwnerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email-ul este obligatoriu")]
    [EmailAddress(ErrorMessage = "Email invalid")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Număr de telefon invalid")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Parola este obligatorie")]
    [MinLength(6, ErrorMessage = "Parola trebuie să aibă cel puțin 6 caractere")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirmarea parolei este obligatorie")]
    [Compare("Password", ErrorMessage = "Parolele nu coincid")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Numele restaurantului este obligatoriu")]
    [MaxLength(200)]
    public string RestaurantName { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required(ErrorMessage = "Adresa este obligatorie")]
    public string Address { get; set; } = string.Empty;

    public double Lat { get; set; } = 46.7712;
    public double Lng { get; set; } = 23.5938;

    public List<string> SelectedCategories { get; set; } = new();

    [Range(0, 999, ErrorMessage = "Taxa de livrare invalidă")]
    public decimal DeliveryFee { get; set; }

    [Range(0, 9999, ErrorMessage = "Valoare minimă invalidă")]
    public decimal MinOrderAmount { get; set; }

    [Range(1, 180, ErrorMessage = "Timp estimat invalid")]
    public int EstimatedDeliveryTime { get; set; } = 30;

    public IFormFile? Logo { get; set; }
    public IFormFile? CoverImage { get; set; }

    public List<DaySchedule> OpeningHours { get; set; } = new()
    {
        new() { Day = "Monday" },
        new() { Day = "Tuesday" },
        new() { Day = "Wednesday" },
        new() { Day = "Thursday" },
        new() { Day = "Friday" },
        new() { Day = "Saturday", Open = "11:00" },
        new() { Day = "Sunday", Open = "11:00" }
    };
}
