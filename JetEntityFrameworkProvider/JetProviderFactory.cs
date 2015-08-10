using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.Data.Entity.Core.Common;

namespace JetEntityFrameworkProvider
{
    /// <summary>
    /// Jet provider factory
    /// </summary>
    public partial class JetProviderFactory : DbProviderFactory, IServiceProvider
    {
        public static readonly JetProviderFactory Instance = new JetProviderFactory();

        /// <summary>
        /// Gets the service object of the specified type.
        /// </summary>
        /// <param name="serviceType">An object that specifies the type of service object to get.</param>
        /// <returns>
        /// A service object of type <paramref name="serviceType" />.-or- null if there is no service object of type <paramref name="serviceType" />.
        /// </returns>
        object IServiceProvider.GetService(Type serviceType)
        {
            // Actually there is a mismatch between EF6 DbProviderServices and EF in Framework 4 DbProviderServices
            if (serviceType.IsAssignableFrom(typeof(DbProviderServices)))
                return JetProviderServices.Instance;
            else
                return null;
        }

        /// <summary>
        /// Specifies whether the specific <see cref="T:System.Data.Common.DbProviderFactory" /> supports the <see cref="T:System.Data.Common.DbDataSourceEnumerator" /> class.
        /// </summary>
        public override bool CanCreateDataSourceEnumerator
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns a new instance of the provider's class that implements the <see cref="T:System.Data.Common.DbCommand" /> class.
        /// </summary>
        /// <returns>
        /// A new instance of <see cref="T:System.Data.Common.DbCommand" />.
        /// </returns>
        public override DbCommand CreateCommand()
        {
            return new JetCommand();
        }

        /// <summary>
        /// Returns a new instance of the provider's class that implements the <see cref="T:System.Data.Common.DbCommandBuilder" /> class.
        /// </summary>
        /// <returns>
        /// A new instance of <see cref="T:System.Data.Common.DbCommandBuilder" />.
        /// </returns>
        public override DbCommandBuilder CreateCommandBuilder()
        {
            OleDbCommandBuilder commandBuilder = new OleDbCommandBuilder();
            commandBuilder.QuotePrefix = "[";
            commandBuilder.QuoteSuffix = "]";
            return commandBuilder;
        }

        /// <summary>
        /// Returns a new instance of the provider's class that implements the <see cref="T:System.Data.Common.DbConnection" /> class.
        /// </summary>
        /// <returns>
        /// A new instance of <see cref="T:System.Data.Common.DbConnection" />.
        /// </returns>
        public override DbConnection CreateConnection()
        {
            return new JetConnection();
        }

        /// <summary>
        /// Returns a new instance of the provider's class that implements the <see cref="T:System.Data.Common.DbConnectionStringBuilder" /> class.
        /// </summary>
        /// <returns>
        /// A new instance of <see cref="T:System.Data.Common.DbConnectionStringBuilder" />.
        /// </returns>
        public override DbConnectionStringBuilder CreateConnectionStringBuilder()
        {
            OleDbConnectionStringBuilder oleDbConnectionStringBuilder = new OleDbConnectionStringBuilder();
            
            return oleDbConnectionStringBuilder;
        }

        /// <summary>
        /// Returns a new instance of the provider's class that implements the <see cref="T:System.Data.Common.DbDataAdapter" /> class.
        /// </summary>
        /// <returns>
        /// A new instance of <see cref="T:System.Data.Common.DbDataAdapter" />.
        /// </returns>
        public override DbDataAdapter CreateDataAdapter()
        {
            return new OleDbDataAdapter();
        }

        /// <summary>
        /// Returns a new instance of the provider's class that implements the <see cref="T:System.Data.Common.DbDataSourceEnumerator" /> class.
        /// </summary>
        /// <returns>
        /// A new instance of <see cref="T:System.Data.Common.DbDataSourceEnumerator" />.
        /// </returns>
        public override DbDataSourceEnumerator CreateDataSourceEnumerator()
        {
            return null;
        }

        /// <summary>
        /// Returns a new instance of the provider's class that implements the <see cref="T:System.Data.Common.DbParameter" /> class.
        /// </summary>
        /// <returns>
        /// A new instance of <see cref="T:System.Data.Common.DbParameter" />.
        /// </returns>
        public override DbParameter CreateParameter()
        {
            return new OleDbParameter();
        }

        /// <summary>
        /// Returns a new instance of the provider's class that implements the provider's version of the <see cref="T:System.Security.CodeAccessPermission" /> class.
        /// </summary>
        /// <param name="state">One of the <see cref="T:System.Security.Permissions.PermissionState" /> values.</param>
        /// <returns>
        /// A <see cref="T:System.Security.CodeAccessPermission" /> object for the specified <see cref="T:System.Security.Permissions.PermissionState" />.
        /// </returns>
        public override System.Security.CodeAccessPermission CreatePermission(System.Security.Permissions.PermissionState state)
        {
            return new OleDbPermission(state);
        }

    }
}
