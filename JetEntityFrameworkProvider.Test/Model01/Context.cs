using System;
using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model01
{
    public class Context : System.Data.Entity.DbContext
    {
        // For migration test
        public Context()
        { }


        public Context(DbConnection connection)
            : base(connection, false)
        {}
        public DbSet<Student> Students { get; set; }
        public DbSet<Standard> Standards { get; set; }
    }
}
