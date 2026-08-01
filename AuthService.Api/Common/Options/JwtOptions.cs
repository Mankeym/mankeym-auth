using System.ComponentModel.DataAnnotations;

namespace AuthService.Api.Common.Options;

public class JwtOptions
{
    public const string SectionName = "JwtSettings";
    [Required]
    public string PrivateKey { get; set; } = string.Empty; // Для подписи (AuthService)

    [Required]
    public string PublicKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "Issuer is required")]
    public string Issuer { get; set; } = string.Empty;

    [Required(ErrorMessage = "Audience is required")]
    public string Audience { get; set; } = string.Empty;

    [Range(1, 10080, ErrorMessage = "ExpiryMinutes must be between 1 and 10080 (1 week)")]
    public double ExpiryMinutes { get; set; } = 60;
}
