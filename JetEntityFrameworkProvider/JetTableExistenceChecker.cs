using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Entity.Core.Common;
using System.Data.Entity.Core.EntityClient;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Infrastructure.Interception;
using System.Linq;
using System.Text;

namespace JetEntityFrameworkProvider
{
    class JetTableExistenceChecker:TableExistenceChecker
    {
        public override bool AnyModelTableExistsInDatabase(ObjectContext context, DbConnection connection, IEnumerable<EntitySet> modelTables, string edmMetadataContextTableName)
        {

            foreach (var modelTable in modelTables)
            {
                if (CheckForTable(context, connection, GetTableName(modelTable)))
                    return true;
            }

            return CheckForTable(context, connection, edmMetadataContextTableName);
        }

        private static bool CheckForTable(ObjectContext context, DbConnection connection, string tableName)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = string.Format("select * from [{0}] where 1=2", tableName);

                var shouldClose = true;

                if (DbInterception.Dispatch.Connection.GetState(connection, context.InterceptionContext) == ConnectionState.Open)
                {
                    shouldClose = false;

                    var entityTransaction = ((EntityConnection)context.Connection).CurrentTransaction;
                    if (entityTransaction != null)
                    {
                        command.Transaction = entityTransaction.StoreTransaction;
                    }
                }

                var executionStrategy = DbProviderServices.GetExecutionStrategy(connection);
                try
                {
                    return executionStrategy.Execute(
                        () =>
                        {
                            if (DbInterception.Dispatch.Connection.GetState(connection, context.InterceptionContext)
                                == ConnectionState.Broken)
                            {
                                DbInterception.Dispatch.Connection.Close(connection, context.InterceptionContext);
                            }

                            if (DbInterception.Dispatch.Connection.GetState(connection, context.InterceptionContext)
                                == ConnectionState.Closed)
                            {
                                DbInterception.Dispatch.Connection.Open(connection, context.InterceptionContext);
                            }

                            try
                            {
                                DbInterception.Dispatch.Command.Scalar(
                                    command, new DbCommandInterceptionContext(context.InterceptionContext));
                                return true;

                            }
                            catch (Exception)
                            {
                                return false;
                            }
                        });
                }
                finally
                {
                    if (shouldClose
                        && DbInterception.Dispatch.Connection.GetState(connection, context.InterceptionContext) != ConnectionState.Closed)
                    {
                        DbInterception.Dispatch.Connection.Close(connection, context.InterceptionContext);
                    }
                }
            }
        }

    }
}
