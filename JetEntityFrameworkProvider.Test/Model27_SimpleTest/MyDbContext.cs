using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model27_SimpleTest
{
    class MyDbContext : DbContext
    {
        public MyDbContext(DbConnection connection)
            : base(connection, false)
        {}
        public DbSet<User> Users { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
    }
}