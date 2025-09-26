using System.ComponentModel.DataAnnotations;

public class SignUpDto
{
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Username { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; }

    [Compare("Password", ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; }
}