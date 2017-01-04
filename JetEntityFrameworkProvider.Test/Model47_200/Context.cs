using System;
using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model47_200
{
    class Context : DbContext
    {
        public Context(DbConnection connection) : base(connection, false)
        {}

        public DbSet<Dept> Depts { get; set; }
        public DbSet<Emp> Emps { get; set; }

protected override void OnModelCreating(DbModelBuilder modelBuilder)
{
    modelBuilder.Entity<Dept>()
        .HasRequired(_ => _.Manager)
        .WithRequiredDependent(_ => _.Department);

}
    }
}
