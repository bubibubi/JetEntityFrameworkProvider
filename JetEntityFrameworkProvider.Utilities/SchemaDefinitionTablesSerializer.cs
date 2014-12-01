using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Entity.Core.Common;
using System.Data.Entity.Core.Metadata.Edm;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JetEntityFrameworkProvider.Utilities
{
    class SchemaDefinitionTablesSerializer
    {
        public static void Start()
        {

            string basePath = "C:\\Temp";

            Schema schema = (Schema)XmlObjectSerializer.GetObject(Properties.Resources.SchemaDefinitionSqlServerQueries, typeof(Schema));


            // Open a connection to local sql server to simulate query running
            DbProviderFactory providerFactory = System.Data.SqlClient.SqlClientFactory.Instance;
            DbConnection connection = providerFactory.CreateConnection();
            connection.ConnectionString = "Server=.;Database=master;User id=sa;Password=mysecretpassword";
            connection.Open();

            foreach (var query in schema.EntityContainer[0].EntitySet)
            {
                string tableName = query.Name.Substring(1);
                DbDataAdapter dataAdapter = providerFactory.CreateDataAdapter();
                DbCommand selectCommand = connection.CreateCommand();
                DataTable dataTable = new DataTable(tableName);
                selectCommand.CommandText = query.DefiningQuery;
                dataAdapter.SelectCommand = selectCommand;
                dataAdapter.Fill(dataTable);

                string filePath = Path.Combine(basePath, "StoreSchemaDefinition." + tableName) + ".xml";

                XmlObjectSerializer.WriteFile(filePath, dataTable);
            }

            connection.Dispose();

        }

    }
}
