using System;
using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model56_SkipTake
{
    public class Context : DbContext
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
