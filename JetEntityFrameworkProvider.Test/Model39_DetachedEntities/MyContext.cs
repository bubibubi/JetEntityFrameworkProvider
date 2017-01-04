using System;
using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model39_DetachedEntities
{
    class MyContext : DbContext
    {
        public MyContext(DbConnection connection) : base(connection, false)
        {}

        public DbSet<Grade> Grades { get; set; }
        public DbSet<GradeWidth> GradeWidths { get; set; }
    }
}
