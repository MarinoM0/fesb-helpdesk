using FesbHelpdesk.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FesbHelpdesk.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.MigrateAsync();

        if (!await db.Categories.AnyAsync())
        {
            db.Categories.AddRange(
                new Category { Name = "Ostalo", Description = "Upiti koji ne pripadaju drugim kategorijama" },
                new Category { Name = "Upis godine", Description = "Upiti vezani uz upis akademske godine" },
                new Category { Name = "Ispiti", Description = "Upiti vezani uz ispite i ispitne rokove" },
                new Category { Name = "Predavanja", Description = "Upiti vezani uz predavanja i nastavu" },
                new Category { Name = "Stipendije", Description = "Upiti o stipendijama i financiranju" },
                new Category { Name = "Potvrde i dokumenti", Description = "Zahtjevi za potvrde i dokumente" },
                new Category { Name = "Studentska praksa", Description = "Upiti o studentskoj praksi" },
                new Category { Name = "Završni rad", Description = "Upiti o završnom ili diplomskom radu" },
                new Category { Name = "Raspored", Description = "Upiti o rasporedu sati" },
                new Category { Name = "Tehnička podrška", Description = "Problemi s e-učenjem, MS Teams, studomatom" }
            );
            await db.SaveChangesAsync();
        }

        if (!await db.Users.AnyAsync())
        {
            db.Users.AddRange(
                new User
                {
                    Email = "admin@fesb.hr",
                    FirstName = "Admin",
                    LastName = "Korisnik",
                    Role = UserRole.Admin,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!")
                },
                new User
                {
                    Email = "referada@fesb.hr",
                    FirstName = "Referada",
                    LastName = "Studentska",
                    Role = UserRole.Referada,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Referada123!")
                },
                new User
                {
                    Email = "nastavnik@fesb.hr",
                    FirstName = "Ivan",
                    LastName = "Horvat",
                    Role = UserRole.Nastavnik,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Nastavnik123!")
                },
                new User
                {
                    Email = "student@fesb.hr",
                    FirstName = "Marko",
                    LastName = "Marić",
                    Role = UserRole.Student,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Student123!")
                }
            );
            await db.SaveChangesAsync();
        }
    }
}
