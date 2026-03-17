using System.ComponentModel.DataAnnotations;

namespace FesbHelpdesk.Api.Models;

public class Ticket
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Status { get; set; } = TicketStatus.Novo;

    [Required, MaxLength(20)]
    public string RecipientType { get; set; } = Models.RecipientType.Referada;

    [MaxLength(200)]
    public string? RecipientEmail { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public int StudentId { get; set; }
    public User Student { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TicketReply> Replies { get; set; } = new List<TicketReply>();
}
