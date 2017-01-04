using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model07
{
    public class Context : DbContext
    {
        public Context(DbConnection connection)
            : base(connection, false)
        { }

        public DbSet<EntityA> As { get; set; }
        public DbSet<EntityB> Bs { get; set; }
        public DbSet<EntityC> Cs { get; set; }
        public DbSet<EntityA_Child> Acs { get; set; }


    }
}
