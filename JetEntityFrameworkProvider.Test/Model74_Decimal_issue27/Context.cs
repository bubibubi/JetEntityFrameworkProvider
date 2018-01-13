using System;
using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model74_Decimal_issue27
{
    public class Context : DbContext
    {
        public Context()
        { }


        public Context(DbConnection connection)
            : base(connection, true)
        { }

        public DbSet<Info> Infos { get; set; }

    }

}
