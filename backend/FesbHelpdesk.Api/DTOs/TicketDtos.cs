using System.ComponentModel.DataAnnotations;

namespace FesbHelpdesk.Api.DTOs;

public class CreateTicketDto
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MinLength(10)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string RecipientType { get; set; } = string.Empty;

    [EmailAddress, MaxLength(200)]
    public string? RecipientEmail { get; set; }
}

public class TicketListItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string RecipientType { get; set; } = string.Empty;
    public string? RecipientEmail { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class TicketDetailDto : TicketListItemDto
{
    public int CategoryId { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<TicketReplyDto> Replies { get; set; } = new();
}

public class TicketReplyDto
{
    public int Id { get; set; }
    public int AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorRole { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateReplyDto
{
    [Required, MinLength(1)]
    public string Message { get; set; } = string.Empty;
}

public class UpdateStatusDto
{
    [Required]
    public string Status { get; set; } = string.Empty;
}

public class UpdateCategoryDto
{
    [Required]
    public int CategoryId { get; set; }
}

public class AssignNastavnikDto
{
    [Required, EmailAddress, MaxLength(200)]
    public string NastavnikEmail { get; set; } = string.Empty;
}

public class ReassignTicketDto
{
    [Required]
    public string RecipientType { get; set; } = string.Empty;

    [EmailAddress, MaxLength(200)]
    public string? RecipientEmail { get; set; }
}

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class CreateCategoryDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }
}
