using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model72_ComplexTypeWNavyProp
{
    public class TestContext : DbContext
    {
        public TestContext(DbConnection connection) : base(connection, true) { }

        public DbSet<Friend> Friends { get; set; }
        public DbSet<City> Cities { get; set; }
    }
}
