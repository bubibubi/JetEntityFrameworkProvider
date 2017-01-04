using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model13_TableSplit_1_1rel
{
    public class TestContext : DbContext
    {
        public TestContext(DbConnection connection) : base(connection, true) { }

        public DbSet<Address> Adresses { get; set; }
        public DbSet<Visit> Visits { get; set; }

    }
}
