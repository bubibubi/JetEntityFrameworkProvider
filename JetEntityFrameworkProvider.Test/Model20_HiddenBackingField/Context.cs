using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model20_HiddenBackingField
{
    class Context : DbContext
    {
        public Context(DbConnection connection)
            : base(connection, false)
        { }

        public DbSet<Company> Companies { get; set; }
        public DbSet<Employee> Employees { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Configurations.Add(new Entity.EntityMap());
        }
    }
}
