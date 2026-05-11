using System.ComponentModel.DataAnnotations;

namespace EatUp.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Numele este obligatoriu")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

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

    [Required(ErrorMessage = "Selectează orașul tău")]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;
}
