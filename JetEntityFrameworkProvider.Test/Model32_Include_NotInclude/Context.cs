using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model32_Include_NotInclude
{
    public class TestContext : DbContext
    {
        public TestContext(DbConnection connection) : base(connection, false) { }

        public DbSet<Address> Adresses { get; set; }
        public DbSet<Visit> Visits { get; set; }

    }
}
