using System;
using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model33_OneToOneDeleteCascade
{
    class TestContext : DbContext
    {
        public TestContext(DbConnection connection) : base(connection, false) { }

        public DbSet<Person> Adresses { get; set; }
        public DbSet<Address> Visits { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
             modelBuilder.Entity<Person>()
             .HasRequired(s => s.Address)
             .WithRequiredPrincipal(ad => ad.Person)
             .WillCascadeOnDelete(true);

        }
    }
}
