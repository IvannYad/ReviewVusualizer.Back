using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ReviewVisualizer.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReviewVisualizer.Data
{
    public class ApplicationDbContext :DbContext
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

        public DbSet<Department> Departments { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Reviewer> Reviewers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
