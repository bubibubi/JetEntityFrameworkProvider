using System;
using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model37_2Contexts_2
{
    class Context2 : DbContext
    {
        public Context2(DbConnection connection):base(connection, false)
        {}

        public DbSet<MyEntity> MyEntities { get; set; }

    }
}
