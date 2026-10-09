using System;
using System.Collections.Generic;
using EventFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.Api.Data;

public partial class EventFlowDbContext : DbContext
{
    public EventFlowDbContext(DbContextOptions<EventFlowDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Event> Event { get; set; }

    public virtual DbSet<Registration> Registration { get; set; }

    public virtual DbSet<User> User { get; set; }

    public virtual DbSet<Venue> Venue { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasIndex(e => e.OwnerUserId, "IX_Event_OwnerUserId");

            entity.HasIndex(e => new { e.Status, e.StartUtc, e.EndUtc, e.VenueId }, "IX_Event_Status_Schedule");

            entity.HasIndex(e => e.Title, "IX_Event_Title");

            entity.Property(e => e.EventId).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Description).HasMaxLength(4000);
            entity.Property(e => e.EndUtc).HasPrecision(0);
            entity.Property(e => e.EventType).HasMaxLength(100);
            entity.Property(e => e.RegistrationDeadlineUtc).HasPrecision(0);
            entity.Property(e => e.StartUtc).HasPrecision(0);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.UpdatedUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Visibility).HasMaxLength(20);

            entity.HasOne(d => d.OwnerUser).WithMany(p => p.Event)
                .HasForeignKey(d => d.OwnerUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Event_OwnerUser");

            entity.HasOne(d => d.Venue).WithMany(p => p.Event)
                .HasForeignKey(d => d.VenueId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Event_Venue");
        });

        modelBuilder.Entity<Registration>(entity =>
        {
            entity.HasIndex(e => new { e.EventId, e.Status }, "IX_Registration_Event_Status");

            entity.HasIndex(e => new { e.UserId, e.Status }, "IX_Registration_User_Status");

            entity.HasIndex(e => new { e.UserId, e.EventId }, "UQ_Registration_UserEvent").IsUnique();

            entity.HasIndex(e => e.ConfirmationReference, "UX_Registration_ConfirmationReference")
                .IsUnique()
                .HasFilter("([ConfirmationReference] IS NOT NULL)");

            entity.Property(e => e.RegistrationId).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.ConfirmationReference).HasMaxLength(40);
            entity.Property(e => e.CreatedUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.DecisionReason).HasMaxLength(1000);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.UpdatedUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Event).WithMany(p => p.Registration)
                .HasForeignKey(d => d.EventId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Registration_Event");

            entity.HasOne(d => d.User).WithMany(p => p.Registration)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Registration_User");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.DemoIdentity, "UQ_User_DemoIdentity").IsUnique();

            entity.HasIndex(e => e.Email, "UQ_User_Email").IsUnique();

            entity.Property(e => e.UserId).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.DemoIdentity).HasMaxLength(100);
            entity.Property(e => e.DisplayName).HasMaxLength(120);
            entity.Property(e => e.Email).HasMaxLength(320);
            entity.Property(e => e.Role).HasMaxLength(20);
        });

        modelBuilder.Entity<Venue>(entity =>
        {
            entity.HasIndex(e => e.Name, "UQ_Venue_Name").IsUnique();

            entity.Property(e => e.VenueId).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.Address).HasMaxLength(300);
            entity.Property(e => e.CreatedUtc)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Name).HasMaxLength(160);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
