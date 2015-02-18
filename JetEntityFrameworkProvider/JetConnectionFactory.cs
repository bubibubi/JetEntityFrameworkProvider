using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;

namespace JetEntityFrameworkProvider
{
    public sealed class JetConnectionFactory : IDbConnectionFactory
    {
        /// <summary>
        /// Creates a connection based on the given database name or connection string.
        /// </summary>
        /// <param name="nameOrConnectionString">The database name or connection string.</param>
        /// <returns>
        /// An initialized DbConnection.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public System.Data.Common.DbConnection CreateConnection(string nameOrConnectionString)
        {
#warning Here we should support also names
            return new JetConnection(nameOrConnectionString);
        }
    }
}
