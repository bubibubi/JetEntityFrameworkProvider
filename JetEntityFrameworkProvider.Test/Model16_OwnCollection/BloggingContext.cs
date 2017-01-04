using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model16_OwnCollection
{
    public class BloggingContext : DbContext
    {
        public BloggingContext(DbConnection connection)
            : base(connection, false)
        {}

                // For migration test
        public BloggingContext()
        { }

        public DbSet<Blog> Blogs { get; set; }
        public DbSet<Post> Posts { get; set; } 
    }
}
