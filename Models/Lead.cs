using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.Models;

public class Lead
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Range(0, 100000000)]
    [Display(Name = "Estimated value")]
    public decimal EstimatedValue { get; set; }

    [StringLength(30)]
    public string Status { get; set; } = "New";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}