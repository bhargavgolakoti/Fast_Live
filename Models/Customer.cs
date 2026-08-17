using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.Models;

public class Customer
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    [Display(Name = "Customer name")]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [StringLength(30)]
    public string? Phone { get; set; }

    [StringLength(100)]
    public string? Company { get; set; }

    [StringLength(30)]
    public string Status { get; set; } = "Active";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}