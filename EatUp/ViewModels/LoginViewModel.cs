using System.ComponentModel.DataAnnotations;

namespace EatUp.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Email-ul este obligatoriu")]
    [EmailAddress(ErrorMessage = "Email invalid")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Parola este obligatorie")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}
