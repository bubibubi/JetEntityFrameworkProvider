using System;
using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model05_WithIndex
{
    public class Context : DbContext
    {
        public Context(DbConnection connection)
            : base(connection, false)
        {}

        public DbSet<Foo> Foos { get; set; }
        public DbSet<Bar> Bars { get; set; }

    }
}
