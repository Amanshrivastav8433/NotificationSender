using Microsoft.EntityFrameworkCore;
using NotificationSender.Models;

namespace NotificationSender.Data;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options): base(options){}

    public DbSet<NotificationQueue> NotificationQueue { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<NotificationQueue>(entity =>
        {
            entity.ToTable("notification_queue");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.NotificationType).HasColumnName("notification_type").IsRequired();
            entity.Property(x => x.Recipient).HasColumnName("recipient").HasMaxLength(500).IsRequired();
            entity.Property(x => x.Subject).HasColumnName("subject").HasMaxLength(1000);
            entity.Property(x => x.Body).HasColumnName("body").IsRequired();
            entity.Property(x => x.Status).HasColumnName("status").IsRequired();
            entity.Property(x => x.RetryCount).HasColumnName("retry_count").HasDefaultValue(0);
            entity.Property(x => x.MaxRetryCount).HasColumnName("max_retry_count").HasDefaultValue(3);
            entity.Property(x => x.ErrorMessage).HasColumnName("error_message");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone");
            entity.Property(x => x.SentAt).HasColumnName("sent_at").HasColumnType("timestamp with time zone");
            entity.Property(x => x.ProcessingStartedAt).HasColumnName("processing_started_at").HasColumnType("timestamp with time zone");
            entity.HasIndex(x => new {x.Status, x.CreatedAt}).HasDatabaseName("ix_notification_queue_status_created_at");
        });
    }
}