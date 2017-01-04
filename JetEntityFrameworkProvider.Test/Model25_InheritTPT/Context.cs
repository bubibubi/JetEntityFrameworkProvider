using System;
using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model25_InheritTPT
{
    public class Context : DbContext
    {

        public Context(DbConnection connection)
            : base(connection, false)
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Configurations.Add(new CompanyMap());
            modelBuilder.Configurations.Add(new SupplierMap());
        }


        public DbSet<Company> Companies { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }

    }
}
