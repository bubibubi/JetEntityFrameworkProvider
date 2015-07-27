using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.Linq;
using System.Text;

namespace JetEntityFrameworkProvider.Test
{
    static class Helpers
    {
        public static int CountRows(JetConnection jetConnection, string sqlStatement)
        {
            DbCommand command = jetConnection.CreateCommand(sqlStatement);
            DbDataReader dataReader = command.ExecuteReader();


            int count = 0;
            while (dataReader.Read())
                count++;

            return count;

        }

        public static void ShowDataReaderContent(DbConnection dbConnection, string sqlStatement)
        {
            DbCommand command = dbConnection.CreateCommand();
            command.CommandText = sqlStatement;
            DbDataReader dataReader = command.ExecuteReader();

            bool first = true;

            for (int i = 0; i < dataReader.FieldCount; i++)
            {
                if (first)
                    first = false;
                else
                    Console.Write("\t");

                Console.Write(dataReader.GetName(i));
            }
            Console.WriteLine();

            while (dataReader.Read())
            {
                first = true;
                for (int i = 0; i < dataReader.FieldCount; i++)
                {
                    if (first)
                        first = false;
                    else
                        Console.Write("\t");

                    Console.Write("{0}", dataReader.GetValue(i));
                }
                Console.WriteLine();
            }

        }

        public static void ShowDataTableContent(DataTable dataTable)
        {

            bool first = true;

            foreach (DataColumn column in dataTable.Columns)
            {
                if (first)
                    first = false;
                else
                    Console.Write("\t");

                Console.Write(column.ColumnName);
            }
            Console.WriteLine();

            foreach (DataRow row in dataTable.Rows)
            {
                first = true;
                foreach (DataColumn column in dataTable.Columns)
                {
                    if (first)
                        first = false;
                    else
                        Console.Write("\t");

                    Console.Write("{0}", row[column]);
                }
                Console.WriteLine();
            }

        }


        public static DbConnection GetConnection()
        {
            // Take care because according to this article
            // http://msdn.microsoft.com/en-us/library/dd0w4a2z(v=vs.110).aspx
            // to make the following line work the provider must be installed in the GAC and we also need an entry in machine.config
            /*
            DbProviderFactory providerFactory = System.Data.Common.DbProviderFactories.GetFactory("JetEntityFrameworkProvider");
            
            DbConnection connection = providerFactory.CreateConnection();
            */

            DbConnection connection = new JetConnection();

            connection.ConnectionString = GetConnectionString();
            return connection;

        }

        public static string GetConnectionString()
        {
            OleDbConnectionStringBuilder oleDbConnectionStringBuilder = new OleDbConnectionStringBuilder();
            //oleDbConnectionStringBuilder.Provider = "Microsoft.Jet.OLEDB.4.0";
            oleDbConnectionStringBuilder.Provider = "Microsoft.ACE.OLEDB.12.0";
            oleDbConnectionStringBuilder.DataSource = @".\Empty.mdb";
            return oleDbConnectionStringBuilder.ToString();
        }

    }
}
