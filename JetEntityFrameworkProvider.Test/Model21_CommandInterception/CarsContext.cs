using System;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Interception;

namespace JetEntityFrameworkProvider.Test.Model21_CommandInterception
{
    public class CarsContext : DbContext
    {
        private readonly IDbInterceptor _dbInterceptor;
        private readonly IDbInterceptor _dbTreeInterceptor;

        public CarsContext(DbConnection connection)
            : base(connection, false)
        {
            _dbInterceptor = new DbCommandInterceptor();
            _dbTreeInterceptor = new DbCommandTreeInterceptor();
            DbInterception.Add(_dbInterceptor);
            DbInterception.Add(_dbTreeInterceptor);
        }

        // For migration test
        public CarsContext()
        { }

        public DbSet<Car> Cars { get; set; }


        protected override void Dispose(bool disposing)
        {
            DbInterception.Remove(_dbInterceptor);
            DbInterception.Remove(_dbTreeInterceptor);
            base.Dispose(disposing);
        }
    }
}
