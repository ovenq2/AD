using Microsoft.EntityFrameworkCore;
using ADUserManagement.Models.Domain;

namespace ADUserManagement.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<SystemUser> SystemUsers { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<UserCreationRequest> UserCreationRequests { get; set; }
        public DbSet<UserDeletionRequest> UserDeletionRequests { get; set; }
        public DbSet<UserAttributeChangeRequest> UserAttributeChangeRequests { get; set; }
        public DbSet<DnsRequest> DnsRequests { get; set; }
        public DbSet<DhcpReservationRequest> DhcpReservationRequests { get; set; }
        public DbSet<GroupMembershipRequest> GroupMembershipRequests { get; set; } // BU SATIR OLMALI
        public DbSet<PasswordResetRequest> PasswordResetRequests { get; set; }
        public DbSet<RequestStatus> RequestStatuses { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Mevcut entity configurations...
            modelBuilder.Entity<UserCreationRequest>()
                .HasOne(r => r.RequestedBy)
                .WithMany()
                .HasForeignKey(r => r.RequestedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserCreationRequest>()
                .HasOne(r => r.ApprovedBy)
                .WithMany()
                .HasForeignKey(r => r.ApprovedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserDeletionRequest>()
                .HasOne(r => r.RequestedBy)
                .WithMany()
                .HasForeignKey(r => r.RequestedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserDeletionRequest>()
                .HasOne(r => r.ApprovedBy)
                .WithMany()
                .HasForeignKey(r => r.ApprovedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserAttributeChangeRequest>()
                .HasOne(r => r.RequestedBy)
                .WithMany()
                .HasForeignKey(r => r.RequestedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserAttributeChangeRequest>()
                .HasOne(r => r.ApprovedBy)
                .WithMany()
                .HasForeignKey(r => r.ApprovedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Yeni entity configurations
            modelBuilder.Entity<DnsRequest>()
                .HasOne(r => r.RequestedBy)
                .WithMany()
                .HasForeignKey(r => r.RequestedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DnsRequest>()
                .HasOne(r => r.ApprovedBy)
                .WithMany()
                .HasForeignKey(r => r.ApprovedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DhcpReservationRequest>()
                .HasOne(r => r.RequestedBy)
                .WithMany()
                .HasForeignKey(r => r.RequestedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DhcpReservationRequest>()
                .HasOne(r => r.ApprovedBy)
                .WithMany()
                .HasForeignKey(r => r.ApprovedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GroupMembershipRequest>()
                .HasOne(r => r.RequestedBy)
                .WithMany()
                .HasForeignKey(r => r.RequestedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GroupMembershipRequest>()
                .HasOne(r => r.ApprovedBy)
                .WithMany()
                .HasForeignKey(r => r.ApprovedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PasswordResetRequest>()
                .HasOne(r => r.RequestedBy)
                .WithMany()
                .HasForeignKey(r => r.RequestedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PasswordResetRequest>()
                .HasOne(r => r.ApprovedBy)
                .WithMany()
                .HasForeignKey(r => r.ApprovedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure nullable strings for new entities
            modelBuilder.Entity<DnsRequest>()
                .Property(e => e.Description)
                .IsRequired(false);

            modelBuilder.Entity<DnsRequest>()
                .Property(e => e.RejectionReason)
                .IsRequired(false);

            modelBuilder.Entity<DhcpReservationRequest>()
                .Property(e => e.Description)
                .IsRequired(false);

            modelBuilder.Entity<DhcpReservationRequest>()
                .Property(e => e.RejectionReason)
                .IsRequired(false);

            modelBuilder.Entity<GroupMembershipRequest>()
                .Property(e => e.Reason)
                .IsRequired(false);

            modelBuilder.Entity<GroupMembershipRequest>()
                .Property(e => e.RejectionReason)
                .IsRequired(false);

            modelBuilder.Entity<PasswordResetRequest>()
                .Property(e => e.Reason)
                .IsRequired(false);

            modelBuilder.Entity<PasswordResetRequest>()
                .Property(e => e.RejectionReason)
                .IsRequired(false);

            // Mevcut nullable configurations...
            modelBuilder.Entity<UserCreationRequest>()
                .Property(e => e.Description)
                .IsRequired(false);

            modelBuilder.Entity<UserCreationRequest>()
                .Property(e => e.RejectionReason)
                .IsRequired(false);

            modelBuilder.Entity<UserDeletionRequest>()
                .Property(e => e.RejectionReason)
                .IsRequired(false);

            modelBuilder.Entity<UserAttributeChangeRequest>()
                .Property(e => e.OldValue)
                .IsRequired(false);

            modelBuilder.Entity<UserAttributeChangeRequest>()
                .Property(e => e.ChangeReason)
                .IsRequired(false);

            modelBuilder.Entity<UserAttributeChangeRequest>()
                .Property(e => e.RejectionReason)
                .IsRequired(false);
        }
    }
}