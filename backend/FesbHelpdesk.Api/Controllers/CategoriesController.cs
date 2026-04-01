using FesbHelpdesk.Api.Data;
using FesbHelpdesk.Api.DTOs;
using FesbHelpdesk.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FesbHelpdesk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public CategoriesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetAll()
    {
        var categories = await _db.Categories
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description
            })
            .ToListAsync();

        return Ok(categories);
    }

    [HttpPost]
    [Authorize(Roles = UserRole.Admin)]
    public async Task<ActionResult<CategoryDto>> Create(CreateCategoryDto dto)
    {
        var name = dto.Name.Trim();

        var nameAlreadyTaken = await _db.Categories.AnyAsync(c => c.Name == name);
        if (nameAlreadyTaken)
        {
            return BadRequest(new { message = "Kategorija s ovim nazivom već postoji." });
        }

        var newCategory = new Category
        {
            Name = name,
            Description = dto.Description?.Trim()
        };

        _db.Categories.Add(newCategory);
        await _db.SaveChangesAsync();

        return Ok(ToDto(newCategory));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = UserRole.Admin)]
    public async Task<ActionResult<CategoryDto>> Update(int id, CreateCategoryDto dto)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        var name = dto.Name.Trim();

        var nameTakenByOther = await _db.Categories.AnyAsync(c => c.Name == name && c.Id != id);
        if (nameTakenByOther)
        {
            return BadRequest(new { message = "Kategorija s ovim nazivom već postoji." });
        }

        category.Name = name;
        category.Description = dto.Description?.Trim();
        await _db.SaveChangesAsync();

        return Ok(ToDto(category));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = UserRole.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        var isDefaultCategory = string.Equals(category.Name, "Ostalo", StringComparison.OrdinalIgnoreCase);
        if (isDefaultCategory)
        {
            return BadRequest(new { message = "Kategorija 'Ostalo' se ne može obrisati." });
        }

        var isInUse = await _db.Tickets.AnyAsync(t => t.CategoryId == id);
        if (isInUse)
        {
            return BadRequest(new { message = "Kategorija je povezana s upitima i ne može biti obrisana." });
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static CategoryDto ToDto(Category category)
    {
        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description
        };
    }
}
