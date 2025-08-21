using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ReviewVisualizer.Data.Models;

namespace ReviewVisualizer.Data
{
    public class ApplicationDbContext : DbContext
    {
        private static readonly object _createNewLock = new object();
        public static ApplicationDbContext CreateNew(ApplicationDbContext original)
        {
            lock (_createNewLock)
            {
                var options = original.GetService<DbContextOptions<ApplicationDbContext>>();
                return new ApplicationDbContext(options);
            }
        }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

        public virtual DbSet<Department> Departments { get; set; }
        public virtual DbSet<Teacher> Teachers { get; set; }
        public virtual DbSet<Review> Reviews { get; set; }
        public virtual DbSet<Reviewer> Reviewers { get; set; }
        public virtual DbSet<Analyst> Analysts { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<UserClaim> UserClaims { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserClaim>()
                .HasIndex(uc => new { uc.UserId, uc.ClaimType })
                .IsUnique();
        }
    }
}
