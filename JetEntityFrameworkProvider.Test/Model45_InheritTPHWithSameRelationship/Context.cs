using System;
using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model45_InheritTPHWithSameRelationship
{
    class Context : DbContext
    {

        public Context(DbConnection connection)
            : base(connection, false)
        {
        }

        // For TPH
        public DbSet<Base> Bases { get; set; }

        public DbSet<Inherited1> M1s { get; set; }
        public DbSet<Inherited2> M2s { get; set; }
        public DbSet<Type1> T1s { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Inherited1>()
                .HasRequired(_ => _.Rel)
                .WithMany()
                .Map(_ => _.MapKey("Rel_Id"));

            modelBuilder.Entity<Inherited2>()
                .HasRequired(_ => _.Rel)
                .WithMany()
                .Map(_ => _.MapKey("Rel_Id"));

        }
    }
}
