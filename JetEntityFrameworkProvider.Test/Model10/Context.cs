using System;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace JetEntityFrameworkProvider.Test.Model10
{
    public class TestContext : DbContext
    {
        public TestContext(DbConnection connection) : base(connection, true){}

        public DbSet<SomeClass> SomeClasses { get; set; }
        public DbSet<Behavior> Behaviors { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();

            modelBuilder.Entity<SomeClass>()
                .HasOptional(s => s.Behavior)
                .WithRequired()
                .WillCascadeOnDelete(true);

        }
    }
}
