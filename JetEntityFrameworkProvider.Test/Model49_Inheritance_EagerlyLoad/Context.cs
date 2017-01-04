using System;
using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model49_Inheritance_EagerlyLoad
{
    class Context : DbContext
    {
        public Context(DbConnection connection) : base(connection, false)
        {}

        public DbSet<A> A { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
