using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity;
using System.Data.Common;

namespace JetEntityFrameworkProvider
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
        public DbSet<TableWithSeveralFieldsType> TableWithSeveralFieldsTypes { get; set; }
    }
}
