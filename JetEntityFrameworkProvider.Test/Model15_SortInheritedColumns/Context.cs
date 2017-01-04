using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model15_SortInheritedColumns
{
    public class TestContext : DbContext
    {
        public TestContext(DbConnection connection) : base(connection, true) { }

        public DbSet<Brand> Brands { get; set; }

    }
}
