using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model09
{
    public class Context : DbContext
    {
        public Context(DbConnection connection)
            : base(connection, false)
        { }

        public DbSet<One> Ones { get; set; }
        public DbSet<Two> Twos { get; set; }
        public DbSet<Three> Threes { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Configurations.Add(new OneMap());
            modelBuilder.Configurations.Add(new TwoMap());
            modelBuilder.Configurations.Add(new ThreeMap());
        }
    }
}