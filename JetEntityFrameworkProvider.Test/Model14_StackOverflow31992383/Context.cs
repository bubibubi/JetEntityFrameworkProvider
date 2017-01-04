using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model14_StackOverflow31992383
{
    public class TestContext : DbContext
    {
        public TestContext(DbConnection connection) : base(connection, true) { }

        public DbSet<Class1> C1s { get; set; }
        public DbSet<Class3> C3s { get; set; }


    }
}
