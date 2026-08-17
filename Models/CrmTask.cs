using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.Models;

public class CrmTask
{
    public int Id { get; set; }

    [Required, StringLength(160)]
    public string Title { get; set; } = string.Empty;

    public DateTime DueDate { get; set; } = DateTime.Today;

    public bool IsCompleted { get; set; }
}