using System;
using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model55_Unicode
{
    public class Context : System.Data.Entity.DbContext
    {
        // For migration test
        public Context()
        { }


        public Context(DbConnection connection)
            : base(connection, false)
        { }
        public DbSet<Entity> Entities { get; set; }
    }

}
