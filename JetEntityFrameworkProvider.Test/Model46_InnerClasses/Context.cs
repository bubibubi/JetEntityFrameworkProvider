using System;
using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model46_InnerClasses
{
    class Context : DbContext
    {
        public Context(DbConnection connection) : base(connection, false)
        {}

        public DbSet<ClassA> ClassAs { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
