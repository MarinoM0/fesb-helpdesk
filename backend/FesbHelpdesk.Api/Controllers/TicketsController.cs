using System.Net;
using System.Security.Claims;
using FesbHelpdesk.Api.Data;
using FesbHelpdesk.Api.DTOs;
using FesbHelpdesk.Api.Models;
using FesbHelpdesk.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FesbHelpdesk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TicketsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IGeminiCategoryService _gemini;
    private readonly IEmailService _email;
    private readonly ILogger<TicketsController> _logger;

    public TicketsController(
        AppDbContext db,
        IGeminiCategoryService gemini,
        IEmailService email,
        ILogger<TicketsController> logger)
    {
        _db = db;
        _gemini = gemini;
        _email = email;
        _logger = logger;
    }

    private int CurrentUserId
    {
        get
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0";
            return int.Parse(raw);
        }
    }

    private string CurrentUserEmail
    {
        get { return User.FindFirstValue(ClaimTypes.Email) ?? string.Empty; }
    }

    private string CurrentUserRole
    {
        get { return User.FindFirstValue(ClaimTypes.Role) ?? string.Empty; }
    }

    [HttpGet]
    public async Task<ActionResult<List<TicketListItemDto>>> List(
        [FromQuery] string? status,
        [FromQuery] int? categoryId,
        [FromQuery] string? q)
    {
        IQueryable<Ticket> query = _db.Tickets
            .Include(t => t.Category)
            .Include(t => t.Student)
            .AsQueryable();

        query = FilterTicketsByCurrentUserRole(query);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(t => t.Status == status);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(t => t.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var searchTerm = q.Trim().ToLower();
            query = query.Where(t =>
                t.Title.ToLower().Contains(searchTerm) ||
                t.Description.ToLower().Contains(searchTerm));
        }

        var items = await query
            .OrderByDescending(t => t.UpdatedAt)
            .Select(t => new TicketListItemDto
            {
                Id = t.Id,
                Title = t.Title,
                Status = t.Status,
                RecipientType = t.RecipientType,
                RecipientEmail = t.RecipientEmail,
                CategoryName = t.Category.Name,
                StudentName = t.Student.FirstName + " " + t.Student.LastName,
                StudentEmail = t.Student.Email,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("stats")]
    public async Task<ActionResult<object>> Stats()
    {
        IQueryable<Ticket> query = _db.Tickets.AsQueryable();
        query = FilterTicketsByCurrentUserRole(query);

        var total = await query.CountAsync();
        var novo = await query.CountAsync(t => t.Status == TicketStatus.Novo);
        var uObradi = await query.CountAsync(t => t.Status == TicketStatus.UObradi);
        var rijeseno = await query.CountAsync(t => t.Status == TicketStatus.Rijeseno);

        return Ok(new { total, novo, uObradi, rijeseno });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TicketDetailDto>> Get(int id)
    {
        var ticket = await _db.Tickets
            .Include(t => t.Category)
            .Include(t => t.Student)
            .Include(t => t.Replies)
                .ThenInclude(r => r.Author)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket is null)
        {
            return NotFound();
        }

        if (!UserCanAccess(ticket))
        {
            return Forbid();
        }

        var replies = new List<TicketReplyDto>();
        foreach (var reply in ticket.Replies.OrderBy(r => r.CreatedAt))
        {
            replies.Add(new TicketReplyDto
            {
                Id = reply.Id,
                AuthorId = reply.AuthorId,
                AuthorName = reply.Author.FirstName + " " + reply.Author.LastName,
                AuthorRole = reply.Author.Role,
                Message = reply.Message,
                CreatedAt = reply.CreatedAt
            });
        }

        var dto = new TicketDetailDto
        {
            Id = ticket.Id,
            Title = ticket.Title,
            Description = ticket.Description,
            Status = ticket.Status,
            RecipientType = ticket.RecipientType,
            RecipientEmail = ticket.RecipientEmail,
            CategoryId = ticket.CategoryId,
            CategoryName = ticket.Category.Name,
            StudentName = ticket.Student.FirstName + " " + ticket.Student.LastName,
            StudentEmail = ticket.Student.Email,
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt,
            Replies = replies
        };

        return Ok(dto);
    }

    [HttpPost]
    [Authorize(Roles = UserRole.Student)]
    public async Task<ActionResult<TicketDetailDto>> Create(CreateTicketDto dto)
    {
        if (!RecipientType.All.Contains(dto.RecipientType))
        {
            return BadRequest(new { message = "Neispravan tip primatelja." });
        }

        string? recipientEmail = null;
        if (dto.RecipientType == RecipientType.Nastavnik)
        {
            if (string.IsNullOrWhiteSpace(dto.RecipientEmail))
            {
                return BadRequest(new { message = "Email nastavnika je obavezan." });
            }

            recipientEmail = dto.RecipientEmail.Trim().ToLowerInvariant();
        }

        var category = await ClassifyAndPickCategoryAsync(dto.Title, dto.Description);
        if (category is null)
        {
            return StatusCode(500, new { message = "Kategorija 'Ostalo' ne postoji u bazi." });
        }

        var now = DateTime.UtcNow;
        var ticket = new Ticket
        {
            Title = dto.Title.Trim(),
            Description = dto.Description.Trim(),
            Status = TicketStatus.Novo,
            RecipientType = dto.RecipientType,
            RecipientEmail = recipientEmail,
            CategoryId = category.Id,
            StudentId = CurrentUserId,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

        await NotifyRecipientOfNewTicket(ticket);

        return await Get(ticket.Id);
    }

    [HttpPost("{id:int}/replies")]
    public async Task<ActionResult<TicketReplyDto>> Reply(int id, CreateReplyDto dto)
    {
        var ticket = await _db.Tickets
            .Include(t => t.Student)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket is null)
        {
            return NotFound();
        }

        if (!UserCanAccess(ticket))
        {
            return Forbid();
        }

        var author = await _db.Users.FindAsync(CurrentUserId);
        if (author is null)
        {
            return Unauthorized();
        }

        var now = DateTime.UtcNow;
        var reply = new TicketReply
        {
            TicketId = ticket.Id,
            AuthorId = author.Id,
            Message = dto.Message.Trim(),
            CreatedAt = now
        };

        ticket.UpdatedAt = now;

        _db.TicketReplies.Add(reply);
        await _db.SaveChangesAsync();

        await NotifyNewReply(ticket, author, reply.Message);

        return Ok(new TicketReplyDto
        {
            Id = reply.Id,
            AuthorId = author.Id,
            AuthorName = author.FirstName + " " + author.LastName,
            AuthorRole = author.Role,
            Message = reply.Message,
            CreatedAt = reply.CreatedAt
        });
    }

    [HttpPut("{id:int}/status")]
    [Authorize(Roles = $"{UserRole.Referada},{UserRole.Nastavnik},{UserRole.Admin}")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateStatusDto dto)
    {
        if (!TicketStatus.All.Contains(dto.Status))
        {
            return BadRequest(new { message = "Neispravan status." });
        }

        var ticket = await _db.Tickets
            .Include(t => t.Student)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket is null)
        {
            return NotFound();
        }

        if (!UserCanModify(ticket))
        {
            return Forbid();
        }

        if (ticket.Status == dto.Status)
        {
            return NoContent();
        }

        ticket.Status = dto.Status;
        ticket.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await NotifyStatusChange(ticket);

        return NoContent();
    }

    [HttpPut("{id:int}/category")]
    [Authorize(Roles = $"{UserRole.Referada},{UserRole.Admin}")]
    public async Task<IActionResult> UpdateCategory(int id, UpdateCategoryDto dto)
    {
        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == id);
        if (ticket is null)
        {
            return NotFound();
        }

        if (!UserCanModify(ticket))
        {
            return Forbid();
        }

        var category = await _db.Categories.FindAsync(dto.CategoryId);
        if (category is null)
        {
            return BadRequest(new { message = "Kategorija ne postoji." });
        }

        ticket.CategoryId = category.Id;
        ticket.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("{id:int}/assign-nastavnik")]
    [Authorize(Roles = UserRole.Referada)]
    public async Task<IActionResult> AssignNastavnik(int id, AssignNastavnikDto dto)
    {
        var ticket = await _db.Tickets
            .Include(t => t.Student)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket is null)
        {
            return NotFound();
        }

        if (ticket.RecipientType != RecipientType.Referada)
        {
            return BadRequest(new { message = "Upit nije dodijeljen referadi." });
        }

        var nastavnikEmail = dto.NastavnikEmail.Trim().ToLowerInvariant();

        ticket.RecipientType = RecipientType.Nastavnik;
        ticket.RecipientEmail = nastavnikEmail;
        ticket.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await NotifyRecipientOfNewTicket(ticket);

        return NoContent();
    }

    [HttpPut("{id:int}/reassign")]
    [Authorize(Roles = UserRole.Admin)]
    public async Task<IActionResult> Reassign(int id, ReassignTicketDto dto)
    {
        if (!RecipientType.All.Contains(dto.RecipientType))
        {
            return BadRequest(new { message = "Neispravan tip primatelja." });
        }

        var ticket = await _db.Tickets
            .Include(t => t.Student)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket is null)
        {
            return NotFound();
        }

        string? newRecipientEmail = null;
        if (dto.RecipientType == RecipientType.Nastavnik)
        {
            if (string.IsNullOrWhiteSpace(dto.RecipientEmail))
            {
                return BadRequest(new { message = "Email nastavnika je obavezan." });
            }

            newRecipientEmail = dto.RecipientEmail.Trim().ToLowerInvariant();
        }

        var nothingChanged = ticket.RecipientType == dto.RecipientType
            && ticket.RecipientEmail == newRecipientEmail;

        if (nothingChanged)
        {
            return NoContent();
        }

        ticket.RecipientType = dto.RecipientType;
        ticket.RecipientEmail = newRecipientEmail;
        ticket.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await NotifyRecipientOfNewTicket(ticket);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = UserRole.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == id);
        if (ticket is null)
        {
            return NotFound();
        }

        _db.Tickets.Remove(ticket);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Admin {AdminId} deleted ticket {TicketId}", CurrentUserId, id);
        return NoContent();
    }

    private IQueryable<Ticket> FilterTicketsByCurrentUserRole(IQueryable<Ticket> query)
    {
        var role = CurrentUserRole;

        if (role == UserRole.Admin)
        {
            return query;
        }

        if (role == UserRole.Student)
        {
            return query.Where(t => t.StudentId == CurrentUserId);
        }

        if (role == UserRole.Referada)
        {
            return query.Where(t => t.RecipientType == RecipientType.Referada);
        }

        if (role == UserRole.Nastavnik)
        {
            var email = CurrentUserEmail;
            return query.Where(t =>
                t.RecipientType == RecipientType.Nastavnik &&
                t.RecipientEmail == email);
        }

        return query.Where(_ => false);
    }

    private bool UserCanAccess(Ticket ticket)
    {
        var role = CurrentUserRole;

        if (role == UserRole.Admin)
        {
            return true;
        }

        if (role == UserRole.Student)
        {
            return ticket.StudentId == CurrentUserId;
        }

        if (role == UserRole.Referada)
        {
            return ticket.RecipientType == RecipientType.Referada;
        }

        if (role == UserRole.Nastavnik)
        {
            return ticket.RecipientType == RecipientType.Nastavnik
                && ticket.RecipientEmail == CurrentUserEmail;
        }

        return false;
    }

    private bool UserCanModify(Ticket ticket)
    {
        var role = CurrentUserRole;

        if (role == UserRole.Admin)
        {
            return true;
        }

        if (role == UserRole.Referada)
        {
            return ticket.RecipientType == RecipientType.Referada;
        }

        if (role == UserRole.Nastavnik)
        {
            return ticket.RecipientType == RecipientType.Nastavnik
                && ticket.RecipientEmail == CurrentUserEmail;
        }

        return false;
    }

    private async Task<Category?> ClassifyAndPickCategoryAsync(string title, string description)
    {
        var allCategoryNames = await _db.Categories.Select(c => c.Name).ToListAsync();
        var classifiedName = await _gemini.ClassifyAsync(title, description, allCategoryNames);

        var matched = await _db.Categories.FirstOrDefaultAsync(c => c.Name == classifiedName);
        if (matched is not null)
        {
            return matched;
        }

        return await _db.Categories.FirstOrDefaultAsync(c => c.Name == "Ostalo");
    }

    private async Task NotifyRecipientOfNewTicket(Ticket ticket)
    {
        var student = ticket.Student ?? await _db.Users.FindAsync(ticket.StudentId);
        if (student is null)
        {
            return;
        }

        var recipientEmail = await ResolveRecipientEmailAsync(ticket);
        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            return;
        }

        var studentFullName = student.FirstName + " " + student.LastName;
        var subject = $"Novi upit: {ticket.Title}";
        var body = $@"
<p>Poštovani,</p>
<p>Student <b>{WebUtility.HtmlEncode(studentFullName)}</b>
({WebUtility.HtmlEncode(student.Email)}) poslao vam je upit.</p>
<p><b>Naslov:</b> {WebUtility.HtmlEncode(ticket.Title)}</p>
<p><b>Opis:</b><br/>{WebUtility.HtmlEncode(ticket.Description).Replace("\n", "<br/>")}</p>
<hr/>
<p>FESB Helpdesk</p>";

        await _email.SendAsync(recipientEmail, subject, body);
    }

    private async Task NotifyStatusChange(Ticket ticket)
    {
        var student = ticket.Student ?? await _db.Users.FindAsync(ticket.StudentId);
        if (student is null)
        {
            return;
        }

        var subject = $"Status upita promijenjen: {ticket.Title}";
        var body = $@"
<p>Poštovani,</p>
<p>Status vašeg upita <b>{WebUtility.HtmlEncode(ticket.Title)}</b> promijenjen je na:
<b>{WebUtility.HtmlEncode(ticket.Status)}</b>.</p>
<p>Pozdrav,<br/>FESB Helpdesk</p>";

        await _email.SendAsync(student.Email, subject, body);
    }

    private async Task NotifyNewReply(Ticket ticket, User author, string message)
    {
        string? recipientEmail;

        if (author.Role == UserRole.Student)
        {
            recipientEmail = await ResolveRecipientEmailAsync(ticket);
        }
        else
        {
            var student = ticket.Student ?? await _db.Users.FindAsync(ticket.StudentId);
            recipientEmail = student?.Email;
        }

        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            return;
        }

        var authorDisplay = BuildReplyAuthorDisplay(author);
        var subject = $"Novi odgovor na upit: {ticket.Title}";
        var body = $@"
<p>Poštovani,</p>
<p>Primljen je novi odgovor na upit <b>{WebUtility.HtmlEncode(ticket.Title)}</b>.</p>
<p><b>Autor:</b> {WebUtility.HtmlEncode(authorDisplay)}</p>
<p><b>Poruka:</b><br/>{WebUtility.HtmlEncode(message).Replace("\n", "<br/>")}</p>
<hr/>
<p>FESB Helpdesk</p>";

        await _email.SendAsync(recipientEmail, subject, body);
    }

    private static string BuildReplyAuthorDisplay(User author)
    {
        if (author.Role == UserRole.Referada)
        {
            return "Studentska referada";
        }

        return $"{author.FirstName} {author.LastName} ({author.Role})";
    }

    private async Task<string?> ResolveRecipientEmailAsync(Ticket ticket)
    {
        if (ticket.RecipientType == RecipientType.Nastavnik)
        {
            return ticket.RecipientEmail;
        }

        var referada = await _db.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Referada);
        return referada?.Email;
    }
}
