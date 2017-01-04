using System;
using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model04
{
    public class CarsContext : DbContext
    {
        public CarsContext(DbConnection connection)
            : base(connection, false)
        {}

                // For migration test
        public CarsContext()
        { }

        public DbSet<Car> Cars { get; set; }
        public DbSet<Person> Persons { get; set; }
    }
}
