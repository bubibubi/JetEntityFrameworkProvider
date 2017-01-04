using System;
using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model35_OneToZeroOneDeleteCascade
{
    class Context : DbContext
    {
        public Context(DbConnection connection) : base(connection, false) { }

        public DbSet<Principal> Principals { get; set; }
        public DbSet<Dependent> Dependents { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Configurations.Add(new PrincipalMap());
            modelBuilder.Configurations.Add(new DependentMap());
        }

    }
}
