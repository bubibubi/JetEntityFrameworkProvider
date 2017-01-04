using System;
using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model36_DetachedScenario
{
    class Context : DbContext
    {
        public Context(DbConnection connection) : base(connection, false)
        {
        }

        public DbSet<Holder> Holders { get; set; }
        public DbSet<Thing> Things { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Holder>()
                .HasOptional(_ => _.Thing)
                .WithMany(_ => _.Holders)
                .Map(_ => _.MapKey("ThingId"));
        }
    }
}
