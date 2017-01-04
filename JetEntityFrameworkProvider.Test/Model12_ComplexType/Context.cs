using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model12_ComplexType
{
    public class TestContext : DbContext
    {
        public TestContext(DbConnection connection) : base(connection, true) { }

        public DbSet<Friend> Friends { get; set; }
        public DbSet<LessThanFriend> LessThanFriends { get; set; }
    }
}
