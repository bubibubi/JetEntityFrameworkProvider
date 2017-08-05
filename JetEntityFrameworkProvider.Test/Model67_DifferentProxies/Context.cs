using System;
using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model67_DifferentProxies
{
    public class Context : DbContext
    {
        public Context()
        { }


        public Context(DbConnection connection)
            : base(connection, true)
        { }

        public DbSet<Person> People { get; set; }
        public DbSet<Info> Infos { get; set; }

    }

}
