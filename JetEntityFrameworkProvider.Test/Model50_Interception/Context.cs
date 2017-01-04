using System;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Interception;

namespace JetEntityFrameworkProvider.Test.Model50_Interception
{
    class Context : DbContext
    {
        public Context(DbConnection connection)
            : base(connection, false)
        { }

        static Context()
        {
            DbInterception.Add(new CommandInterceptor());
        }


        public DbSet<Note> Notes { get; set; }
    }
}
