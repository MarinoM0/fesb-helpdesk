using FesbHelpdesk.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FesbHelpdesk.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketReply> TicketReplies => Set<TicketReply>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.HasIndex(c => c.Name).IsUnique();
        });

        modelBuilder.Entity<Ticket>(e =>
        {
            e.HasOne(t => t.Student)
                .WithMany(u => u.Tickets)
                .HasForeignKey(t => t.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(t => t.Category)
                .WithMany(c => c.Tickets)
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(t => t.Status);
            e.HasIndex(t => t.RecipientType);
            e.HasIndex(t => t.RecipientEmail);
        });

        modelBuilder.Entity<TicketReply>(e =>
        {
            e.HasOne(r => r.Ticket)
                .WithMany(t => t.Replies)
                .HasForeignKey(r => r.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(r => r.Author)
                .WithMany(u => u.Replies)
                .HasForeignKey(r => r.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        base.OnModelCreating(modelBuilder);
    }
}
