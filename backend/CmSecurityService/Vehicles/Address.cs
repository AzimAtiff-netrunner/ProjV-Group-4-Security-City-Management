using System.ComponentModel.DataAnnotations;

namespace CmSecurityService.Vehicles;

/// <summary>
/// Represents a shared Waterloo street address.
/// </summary>
public class Address
{
    [Required]
    [StringLength(10)]
    public string StreetNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string StreetName { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^Waterloo$", ErrorMessage = "City must be Waterloo.")]
    public string City { get; set; } = "Waterloo";

    [Required]
    [RegularExpression(
        @"^[A-Za-z]\d[A-Za-z][ -]?\d[A-Za-z]\d$",
        ErrorMessage = "Invalid Canadian postal code.")]
    public string PostalCode { get; set; } = string.Empty;
}