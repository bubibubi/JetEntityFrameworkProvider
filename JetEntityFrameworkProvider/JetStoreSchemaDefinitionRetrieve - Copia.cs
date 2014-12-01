using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace JetEntityFrameworkProvider
{
    /// <summary>
    /// Retrieve metadata from oledb or from system tables
    /// About oledb parameters see here
    /// http://msdn.microsoft.com/en-us/library/cc716722(v=vs.110).aspx
    /// </summary>
    static class JetStoreSchemaDefinitionRetrieve
    {

        static Regex _regExParseShowCommand = null;

        internal static System.Data.Common.DbDataReader GetDbDataReader(DbConnection connection, string commandText)
        {
            // Command text format is
            // show <what> [where <condition>]
            //    <what> can be
            //          tables
            //          tablecolumns
            //
            //          views
            //          viewscolumns
            //          viewconstraints
            //          viewconstraintcolumns
            //          viewforeignkeys
            //
            //          checkconstraints
            //          foreignkeys
            //          foreingkeyconstraints
            //          constraintcolumns
            //
            //          functions
            //          functionparameters
            //          functionreturntablecolumns
            //
            //          procedures
            //          procedureparameters


            if (_regExParseShowCommand == null)
                _regExParseShowCommand = new Regex(
                    @"^\s*show\s*(?<object>\w*)\s*(where\s*(?<condition>.*))?$",
                    RegexOptions.IgnoreCase);

            Match match = _regExParseShowCommand.Match(commandText);

            if (!match.Success)
                throw new Exception(string.Format("Unrecognized show statement '{0}'. show syntax is show <object> [where <condition>]", commandText));

            string dbObject = match.Groups["object"].Value;
            string condition = match.Groups["condition"].Value;

            DataTable dataTable;

            OleDbConnection oleDbConnection = (OleDbConnection)connection;

            ConnectionState oldConnectionState = connection.State;

            if (oldConnectionState != ConnectionState.Open)
                connection.Open();

            try
            {
                switch (dbObject.ToLower())
                {
                    case "tables":
                        dataTable = GetTables(oleDbConnection);
                        break;
                    case "tablecolumns":
                        dataTable = GetTableColumns(oleDbConnection);
                        break;
                    default:
                        throw new Exception(string.Format("Unknown metadata object type {0}", dbObject));
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (oldConnectionState != ConnectionState.Open)
                    connection.Close();
            }

            if (!string.IsNullOrWhiteSpace(condition))
            {
                foreach (DataRow row in dataTable.Select(string.Format("NOT ({0})", condition)))
                    row.Delete();
                dataTable.AcceptChanges();
            }

            return dataTable.CreateDataReader();
        }

        private static DataTable GetTableColumns(OleDbConnection connection)
        {
            DataTable dataTable = (DataTable)XmlObjectSerializer.GetObject(Properties.Resources.StoreSchemaDefinition_TableColumns, typeof(DataTable));

            Dictionary<string, string> objectsToGet = GetTablesOrViewDictionary(connection, true);

            // Take care because there is no way to retrieve the columns with all the properties (i.e. autonum properties) using OleDb
            /*
            DataTable schemaTable = connection.GetOleDbSchemaTable(
              System.Data.OleDb.OleDbSchemaGuid.Columns,
              new object[] { null, null, null, null });


            foreach (System.Data.DataRow rowColumn in schemaTable.Rows)
                if (objectsToGet.ContainsKey(rowColumn["TABLE_NAME"].ToString()))
                {
                    dataTable.Rows.Add(
                        rowColumn["TABLE_NAME"] + "." + rowColumn["COLUMN_NAME"], // Id
                        rowColumn["TABLE_NAME"], // ParentId
                        rowColumn["COLUMN_NAME"], // Name
                        rowColumn["ORDINAL_POSITION"],  // Ordinal
                        Convert.ToBoolean(rowColumn["IS_NULLABLE"]) ? 1 : 0, // IsNullable
                        ConvertToJetDataType(Convert.ToInt32(rowColumn["DATA_TYPE"]), Convert.ToInt32(rowColumn["COLUMN_FLAGS"])), // TypeName
                        rowColumn["CHARACTER_MAXIMUM_LENGTH"], // Max length
                        rowColumn["NUMERIC_PRECISION"], // Precision
                        rowColumn["DATETIME_PRECISION"], //DateTimePrecision
                        rowColumn["NUMERIC_SCALE"], // Scale

                        rowColumn["COLLATION_CATALOG"],	//CollationCatalog
                        rowColumn["COLLATION_SCHEMA"],	//CollationSchema
                        rowColumn["COLLATION_NAME"], //CollationName
                        rowColumn["CHARACTER_SET_CATALOG"],	//CharacterSetCatalog
                        rowColumn["CHARACTER_SET_SCHEMA"], //CharacterSetSchema
                        rowColumn["CHARACTER_SET_NAME"], //CharacterSetName
                        0,      //IsMultiSet
                        Convert.ToInt32(rowColumn["COLUMN_FLAGS"]) == 0x5a && Convert.ToInt32(rowColumn["DATA_TYPE"]) == 3 ? 1 : 0, // IsIdentity
                        Convert.ToBoolean(rowColumn["COLUMN_HASDEFAULT"]) ? 1 : 0, // IsStoreGenerated
                        rowColumn["COLUMN_DEFAULT"] // Default
                        );
                }

            */

            foreach (var kvPair in objectsToGet)
            {
                string tableName = kvPair.Value;

            string sql = string.Empty;

            sql += "Select ";
            sql += "    * ";
            sql += "From ";
            sql += string.Format("    {0} ", JetProviderManifest.QuoteIdentifier(tableName));
            sql += "Where ";
            sql += "    1 = 2 ";

            IDbCommand command = null;
            IDataReader dataReader = null;
            DataTable dataTable = null;

            try
            {
                command = connection.CreateCommand(sql);

                // If the dbType is cobol file the read column is different
                if (connection.DBType == DBType.CobolFile)
                    // KeyInfo parameter is needed otherwise not all the information about the schema are retrieved
                    dataReader = command.ExecuteReader(CommandBehavior.SchemaOnly);
                else
                    // KeyInfo parameter is needed otherwise not all the information about the schema are retrieved
                    dataReader = command.ExecuteReader(CommandBehavior.KeyInfo);

                // Retrieve the schema
                dataTable = dataReader.GetSchemaTable();

                this.Clear();
                foreach (DataRow row in dataTable.Rows)
                {
                    if (skipHiddenColumns)
                    {
                        bool isHidden;
                        try
                        {
                            isHidden = (bool)row["IsHidden"];
                        }
                        catch
                        {
                            isHidden = false;
                        }

                        if (isHidden)
                            continue;
                    }

                    ColumnDefinition c = new ColumnDefinition();

                    c.Name = (string)row["ColumnName"];
                    c.Length = (int)row["ColumnSize"];
                    c.NumericPrecision = DBToShort(row["NumericPrecision"]);
                    c.NumericScale = DBToShort(row["NumericScale"]);
                    c.NetType = (System.Type)row["DataType"];
                    c.ProviderType = row["ProviderType"];
                    if (connection.DBType == DBType.SQLite)
                        c.IsLong = c.Length > 65535;
                    else
                        c.IsLong = (bool)row["IsLong"];
                    c.AllowDBNull = (bool)row["AllowDBNull"];
                    c.IsUnique = (bool)row["IsUnique"];

#if AS400NativeClient
          if (connection._InnerConnection is iDB2Connection)
            c.IsKey = (bool)row["IsKeyColumn"];
          else
            c.IsKey = (bool)row["IsKey"];
#else
                    c.IsKey = (bool)row["IsKey"];
#endif
                    try
                    {
                        c.AutoIncrement = (bool)row["IsAutoIncrement"];
                    }
                    catch
                    {
                        c.AutoIncrement = false;
                    }
                    c.SchemaName = DBToString(row["BaseSchemaName"]);

                    try
                    {
                        c.CatalogName = DBToString(row["BaseCatalogName"]);
                    }
                    catch
                    {
                        c.CatalogName = "";
                    }

                    c.ComputeSQLDataType();

                    this.Add(c);
                }
            }
            finally
            {
                // Exceptions will not be catched but these instructions will be executed anyway
                if (command != null)
                    command.Dispose();

                if (dataReader != null)
                    dataReader.Dispose();

                if (dataTable != null)
                    dataTable.Dispose();
            }

            this.IsRefreshed = true;

            }


            return dataTable;
        }

        private static DataTable GetTables(OleDbConnection connection)
        {
            DataTable dataTable = (DataTable)XmlObjectSerializer.GetObject(Properties.Resources.StoreSchemaDefinition_Tables, typeof(DataTable));

            DataTable schemaTable = connection.GetOleDbSchemaTable(
              System.Data.OleDb.OleDbSchemaGuid.Tables,
              new object[] { null, null, null, "TABLE" });

            foreach (System.Data.DataRow table in schemaTable.Rows)
                dataTable.Rows.Add(table["TABLE_NAME"], "Jet", "Jet", table["TABLE_NAME"]);

            dataTable.AcceptChanges();

            return dataTable;
        }

        private static Dictionary<string, string> GetTablesOrViewDictionary(OleDbConnection connection, bool getTables)
        {
            DataTable schemaTable = connection.GetOleDbSchemaTable(
              System.Data.OleDb.OleDbSchemaGuid.Tables,
              new object[] { null, null, null, getTables ? "TABLE" : "VIEW" });

            Dictionary<string, string> list = new Dictionary<string, string>(schemaTable.Rows.Count, StringComparer.InvariantCultureIgnoreCase);

            foreach (System.Data.DataRow table in schemaTable.Rows)
                list.Add(table["TABLE_NAME"].ToString(), table["TABLE_NAME"].ToString());

            return list;
        }

        private static string ConvertToJetDataType(int intOleDbType, int intFlags)
        {

            OleDbColumnFlag flags = (OleDbColumnFlag)intFlags;

            switch (((OleDbType)intOleDbType))
            {
                case OleDbType.BigInt:
                    return "int";       // In Jet this is 32 bit while bigint is 64 bits
                case OleDbType.Binary:
                    if (flags.HasFlag(OleDbColumnFlag.IsLong))
                        return "image";
                    else if (flags.HasFlag(OleDbColumnFlag.IsFixedLength))
                        return "binary";
                    else
                        return "varbinary";
                case OleDbType.Boolean:
                    return "bit";
                case OleDbType.Char:
                    return "char";
                case OleDbType.Currency:
                    return "decimal";
                case OleDbType.DBDate:
                case OleDbType.Date:
                case OleDbType.DBTimeStamp:
                    return "datetime";
                case OleDbType.Decimal:
                case OleDbType.Numeric:
                    return "decimal";
                case OleDbType.Double:
                    return "double";
                case OleDbType.Integer:
                    return "int";
                case OleDbType.Single:
                    return "single";
                case OleDbType.SmallInt:
                    return "smallint";
                case OleDbType.TinyInt:
                    return "smallint";  // Signed byte not handled by jet so we need 16 bits
                case OleDbType.UnsignedTinyInt:
                    return "byte";
                case OleDbType.LongVarBinary:
                case OleDbType.VarBinary:
                    return "varbinary";
                case OleDbType.VarChar:
                case OleDbType.LongVarChar:
                    return "varchar";
                case OleDbType.WChar:
                    if (flags.HasFlag(OleDbColumnFlag.IsLong))
                        return "text";
                    else if (flags.HasFlag(OleDbColumnFlag.IsFixedLength))
                        return "char";
                    else
                        return "varchar";

                case OleDbType.BSTR:
                case OleDbType.Variant:
                case OleDbType.VarWChar:
                case OleDbType.VarNumeric:
                case OleDbType.Error:
                case OleDbType.DBTime:
                case OleDbType.Empty:
                case OleDbType.Filetime:
                case OleDbType.Guid:
                case OleDbType.IDispatch:
                case OleDbType.IUnknown:
                case OleDbType.UnsignedBigInt:
                case OleDbType.UnsignedInt:
                case OleDbType.UnsignedSmallInt:
                case OleDbType.PropVariant:
                default:
                    throw new ArgumentException(string.Format("The data type {0} is not handled by Jet. Did you retrieve this from Jet?", ((OleDbType)intOleDbType)));
            }
        }

    }
}
