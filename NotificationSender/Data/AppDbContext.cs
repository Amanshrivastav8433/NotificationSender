using Microsoft.EntityFrameworkCore;
using NotificationSender.Models;

namespace NotificationSender.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<EmailToBeSent> Emails
        => Set<EmailToBeSent>();
}