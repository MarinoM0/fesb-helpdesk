using System.ComponentModel.DataAnnotations;

namespace FesbHelpdesk.Api.Models;

public class Category
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
