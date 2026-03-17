using System.ComponentModel.DataAnnotations;

namespace FesbHelpdesk.Api.Models;

public class TicketReply
{
    public int Id { get; set; }

    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    public int AuthorId { get; set; }
    public User Author { get; set; } = null!;

    [Required]
    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
