using System;
using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model73_Guid_issue20
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

    }
}
