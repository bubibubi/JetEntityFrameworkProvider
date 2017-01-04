using System;
using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model42
{
    class Context : DbContext
    {
        public Context(DbConnection connection) : base(connection, false)
        {
            
        }

        public DbSet<Foo> Foos { get; set; }
        public DbSet<Bar> Bars { get; set; }


        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Foo>()
                .HasMany(_ => _.Bars)
                .WithMany(_ => _.Foos);
        }
    }
}
