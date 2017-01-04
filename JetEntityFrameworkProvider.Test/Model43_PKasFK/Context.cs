using System;
using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model43_PKasFK
{
    class Context : DbContext
    {
        public Context(DbConnection connection) : base(connection, false)
        {
            
        }

        public DbSet<Parent> Parents { get; set; }
        public DbSet<Child> Children { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Parent>()
                .HasMany(_ => _.Children)
                .WithRequired(_ => _.Parent)
                .HasForeignKey(_ => _.ParentName);
        }
    }
}
