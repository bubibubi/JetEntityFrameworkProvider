using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model06_Inherit
{
    public class Context : DbContext
    {
        public Context(DbConnection connection)
            : base(connection, false)
        { }

        public DbSet<Address> Addresses { get; set; }
        public DbSet<User> Users { get; set; }

    }
}
