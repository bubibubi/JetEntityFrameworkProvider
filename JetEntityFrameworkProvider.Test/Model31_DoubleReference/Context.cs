using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model31_DoubleReference
{
    class Context : DbContext
    {

        public Context(DbConnection connection)
            : base(connection, false)
        {
        }

        public DbSet<Person> People { get; set; }
        public DbSet<PhoneNumber> PhoneNumbers { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PhoneNumber>().HasOptional(e => e.Person).WithMany(e => e.PhoneNumbers);
        }
    }
}
