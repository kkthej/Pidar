using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Pidar.Areas.Identity.Data;

namespace Pidar.Data
{
    public class ApplicationDbContext : IdentityDbContext<PidarUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // PostgreSQL-specific identity model configuration
            modelBuilder.HasDefaultSchema("public");

            // Fix for PostgreSQL identity tables
            modelBuilder.Entity<PidarUser>(entity =>
            {
                entity.ToTable("users");
            });

            modelBuilder.Entity<IdentityRole>(entity =>
            {
                entity.ToTable("roles");
            });

            modelBuilder.Entity<IdentityUserRole<string>>(entity =>
            {
                entity.ToTable("user_roles");
            });

            modelBuilder.Entity<IdentityUserClaim<string>>(entity =>
            {
                entity.ToTable("user_claims");
            });

            modelBuilder.Entity<IdentityUserLogin<string>>(entity =>
            {
                entity.ToTable("user_logins");
            });

            modelBuilder.Entity<IdentityRoleClaim<string>>(entity =>
            {
                entity.ToTable("role_claims");
            });

            modelBuilder.Entity<IdentityUserToken<string>>(entity =>
            {
                entity.ToTable("user_tokens");
            });
        }
    }
}