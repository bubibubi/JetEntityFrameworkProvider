using System;
using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model37_2Contexts_1
{
    class Context1 : DbContext
    {
        public Context1(DbConnection connection):base(connection, false)
        {}

        public DbSet<MyEntity> MyEntities { get; set; }

    }
}
