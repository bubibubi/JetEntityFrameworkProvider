using System;
using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model75_Include_issue28
{
    public class Context : DbContext
    {
        public Context()
        { }


        public Context(DbConnection connection)
            : base(connection, true)
        { }

        public DbSet<ReferredClass> ReferredClasses { get; set; }
        public DbSet<ReferringClass1> ReferringClass1s { get; set; }
        public DbSet<ReferringClass2> ReferringClass2s { get; set; }

    }

}
