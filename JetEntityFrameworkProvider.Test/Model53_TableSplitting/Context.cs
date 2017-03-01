using System;
using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model53_TableSplitting
{
    class Context : DbContext
    {
        public Context(DbConnection connection) : base(connection, false)
        {}

        public DbSet<Person> Persons { get; set; }
        public DbSet<Address> Addresses { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Address>()
                .HasKey(t => t.PersonID)
                .HasOptional(t => t.City)
                .WithMany()
                .Map(t => t.MapKey("CityID"));

            modelBuilder.Entity<Person>()
                .HasRequired(t => t.Address)
                .WithRequiredPrincipal(t => t.Person);

            modelBuilder.Entity<Person>().ToTable("TB_PERSON");

            modelBuilder.Entity<Address>().ToTable("TB_PERSON");

            modelBuilder.Entity<City>()
                .HasKey(t => t.CityID)
                .ToTable("City");
        }
    }
}
