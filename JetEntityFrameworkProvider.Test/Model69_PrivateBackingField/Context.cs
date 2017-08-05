using System;
using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model69_PrivateBackingField
{
    public class Context : DbContext
    {
        public Context()
        { }


        public Context(DbConnection connection)
            : base(connection, true)
        { }

        public DbSet<Info> Infos { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Configurations.Add(new Info.InfoMap());
        }
    }

}
