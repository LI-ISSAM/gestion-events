using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using GestionEvenements.Models;

namespace GestionEvenements.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Event> Events { get; set; }
        public DbSet<Inscription> Inscriptions { get; set; }
        public DbSet<StatisticViewModel> EventStatistics { get; set; }
        public DbSet<EventCategory> Categories { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Comment> Comments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Event>()
                .Property(e => e.Prix)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Event>()
                .HasOne(e => e.CreatedByUser)
                .WithMany(u => u.CreatedEvents)
                .HasForeignKey(e => e.CreatedById)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Event>()
                .HasOne(e => e.Categorie)
                .WithMany()
                .HasForeignKey(e => e.CategorieId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Event>()
                .HasOne(e => e.Statistiques)
                .WithOne(s => s.Event)
                .HasForeignKey<StatisticViewModel>(s => s.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Inscription>()
                .HasOne(i => i.User)
                .WithMany(u => u.Inscriptions)
                .HasForeignKey(i => i.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Inscription>()
                .HasOne(i => i.Event)
                .WithMany(e => e.Inscriptions)
                .HasForeignKey(i => i.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Inscription>()
                .HasIndex(i => new { i.UserId, i.EventId })
                .IsUnique(true)
                .HasName("IX_UniqueUserEventInscription");

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany(u => u.Commentaires)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Event)
                .WithMany(e => e.Commentaires)
                .HasForeignKey(c => c.EventId)
                .OnDelete(DeleteBehavior.Cascade);

           modelBuilder.Entity<Comment>()
    .HasIndex(c => new { c.UserId, c.EventId })
    .HasDatabaseName("IX_UserEventComment");

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EventCategory>()
                .HasKey(c => c.Id);

            modelBuilder.Entity<Event>()
                .HasIndex(e => e.DateEvent)
                .HasDatabaseName("IX_EventDate");

            modelBuilder.Entity<Event>()
                .HasIndex(e => e.CreatedById)
                .HasDatabaseName("IX_EventCreatedBy");

            modelBuilder.Entity<Inscription>()
                .HasIndex(i => i.DateInscription)
                .HasDatabaseName("IX_InscriptionDate");

            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.UserId, n.EstLue })
                .HasDatabaseName("IX_UserNotificationRead");

            modelBuilder.Entity<Comment>()
                .HasIndex(c => c.DateCreation)
                .HasDatabaseName("IX_CommentDate");
        }
    }
}
