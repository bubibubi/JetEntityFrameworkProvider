using System;
using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model18_CompositeKeys
{
    public class TestContext : DbContext
    {
        public TestContext(DbConnection connection)
            : base(connection, false)
        {}

        public DbSet<Product> Products { get; set; }
        public DbSet<GoodsIssueProcess> Processes { get; set; }

    }
}
