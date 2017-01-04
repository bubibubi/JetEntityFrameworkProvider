using System;
using System.Data.Common;
using System.Data.Entity;
using JetEntityFrameworkProvider.Test.Model29_InheritTPH;

namespace JetEntityFrameworkProvider.Test.Model25_InheritTPH
{
    class Context : DbContext
    {

        public Context(DbConnection connection)
            : base(connection, false)
        {
        }

        public DbSet<Derived1Model> M1s { get; set; }
        public DbSet<Derived2Model> M2s { get; set; }
        public DbSet<Derived3Model> M3s { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<User>()
            //    .HasMany(u => u.Advertisements)
            //    .WithRequired(x => x.User);

            //modelBuilder.Entity<Advertisement>()
            //    .HasMany(a => a.AdImages)
            //    .WithRequired(x => x.Advertisement);

            //modelBuilder.Entity<AdImage>()
            //    .HasRequired(x => x.Advertisement);

            base.OnModelCreating(modelBuilder);

        }
    }
}
