using System;
using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model19_1_1
{
    class Context : DbContext
    {
        public Context(DbConnection connection)
            : base(connection, false)
        { }

        public DbSet<ClassA> As { get; set; }
        public DbSet<ClassB> Bs { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ClassB>().HasOptional(c => c.ClassA).WithOptionalDependent(c => c.ClassB);
        }
    }
}
