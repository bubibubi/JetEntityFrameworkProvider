using System;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Interception;

namespace JetEntityFrameworkProvider.Test.Model24_MultiTenantApp
{
    public class Context : DbContext
    {
        private readonly IDbInterceptor _dbTreeInterceptor;

        public Context(DbConnection connection)
            : base(connection, false)
        {
            // NOT THE RIGHT PLACE TO DO THIS!!!
            _dbTreeInterceptor = new TenantCommandTreeInterceptor();
            DbInterception.Add(_dbTreeInterceptor);
        }

        public DbSet<MyEntity> MyEntities { get; set; }

        protected override void Dispose(bool disposing)
        {
            DbInterception.Remove(_dbTreeInterceptor);

            base.Dispose(disposing);
        }
    }
}
