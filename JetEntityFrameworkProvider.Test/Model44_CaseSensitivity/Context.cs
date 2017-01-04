using System;
using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model44_CaseSensitivity
{
    class Context : DbContext
    {
        public Context(DbConnection connection) : base(connection, false)
        {
            
        }

        public DbSet<Entity> Entities { get; set; }
    }
}
