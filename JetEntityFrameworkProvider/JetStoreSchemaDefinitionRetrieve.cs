using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.Diagnostics;
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
            // show <what> [where <condition>] [order by <order>]
            //    <what> can be
            //          tables
            //          tablecolumns
            //
            //          indexes | ix
            //          indexcolumns | ixc
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
            //
            //
            //          constraints
            //          constraintcolumns
            //          checkconstraints | cc
            //          foreignkeyconstraints | fkc
            //          foreignkeys | fk
            //          viewconstraints | vc
            //          viewconstraintcolumns | vcc
            //          viewforeignkeys | vfk





            if (_regExParseShowCommand == null)
                _regExParseShowCommand = new Regex(
                    @"^\s*show\s*(?<object>\w*)\s*(where\s+(?<condition>.+?))?\s*(order\s+by\s+(?<order>.+))?$",
                    RegexOptions.IgnoreCase);

            Match match = _regExParseShowCommand.Match(commandText);

            if (!match.Success)
                throw new Exception(string.Format("Unrecognized show statement '{0}'. show syntax is show <object> [where <condition>]", commandText));

            string dbObject = match.Groups["object"].Value;
            string condition = match.Groups["condition"].Value;
            string order = match.Groups["order"].Value;

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
                    case "ix":
                    case "indexes":
                        dataTable = GetIndexes(oleDbConnection);
                        break;
                    case "ixc":
                    case "indexcolumns":
                        dataTable = GetIndexColumns(oleDbConnection);
                        break;
                    case "views":
                        dataTable = GetViews(oleDbConnection);
                        break;
                    case "viewcolumns":
                        dataTable = GetViewColumns(oleDbConnection);
                        break;
                    case "functions":
                        dataTable = (DataTable)XmlObjectSerializer.GetObject(Properties.Resources.StoreSchemaDefinition_Functions, typeof(DataTable));
                        break;
                    case "functionparameters":
                        dataTable = (DataTable)XmlObjectSerializer.GetObject(Properties.Resources.StoreSchemaDefinition_FunctionParameters, typeof(DataTable));
                        break;
                    case "functionreturntablecolumns":
                        dataTable = (DataTable)XmlObjectSerializer.GetObject(Properties.Resources.StoreSchemaDefinition_FunctionReturnTableColumns, typeof(DataTable));
                        break;
                    case "procedures":
                        dataTable = (DataTable)XmlObjectSerializer.GetObject(Properties.Resources.StoreSchemaDefinition_Procedures, typeof(DataTable));
                        break;
                    case "procedureparameters":
                        dataTable = (DataTable)XmlObjectSerializer.GetObject(Properties.Resources.StoreSchemaDefinition_ProcedureParameters, typeof(DataTable));
                        break;
                    case "constraints":
                        dataTable = GetConstraints(oleDbConnection);
                        break;
                    case "constraintcolumns":
                        dataTable = GetConstraintColumns(oleDbConnection);
                        break;
                    case "cc":
                    case "checkconstraints":
                        dataTable = GetCheckConstraints(oleDbConnection);
                        break;
                    case "foreignkeyconstraints":
                    case "fkc":
                        dataTable = GetForeignKeyConstraints(oleDbConnection);
                        break;
                    case "foreignkeys":
                    case "fk":
                        dataTable = GetForeignKeyConstraintColumns(oleDbConnection);
                        break;
                    case "viewconstraints":
                    case "vc":
                        dataTable = (DataTable)XmlObjectSerializer.GetObject(Properties.Resources.StoreSchemaDefinition_ViewConstraints, typeof(DataTable));
                        break;
                    case "viewconstraintcolumns":
                    case "vcc":
                        dataTable = (DataTable)XmlObjectSerializer.GetObject(Properties.Resources.StoreSchemaDefinition_ViewConstraintColumns, typeof(DataTable));
                        break;
                    case "viewforeignkeys":
                    case "vfk":
                        dataTable = (DataTable)XmlObjectSerializer.GetObject(Properties.Resources.StoreSchemaDefinition_ViewForeignKeys, typeof(DataTable));
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


            DataRow[] selectedRows;
            DataTable selectedDataTable = dataTable.Clone();

            if (!string.IsNullOrWhiteSpace(condition) && !string.IsNullOrWhiteSpace(order))
                selectedRows = dataTable.Select(condition, order);
            else if (!string.IsNullOrWhiteSpace(condition))
                selectedRows = dataTable.Select(condition);
            else if (!string.IsNullOrWhiteSpace(order))
                selectedRows = dataTable.Select("1=1", order);
            else
                return dataTable.CreateDataReader();

            foreach (DataRow row in selectedRows)
                selectedDataTable.ImportRow(row);

            return selectedDataTable.CreateDataReader();
        }

        #region Tables

        private static DataTable GetTables(OleDbConnection connection)
        {
            DataTable dataTable = (DataTable)XmlObjectSerializer.GetObject(Properties.Resources.StoreSchemaDefinition_Tables, typeof(DataTable));

            DataTable schemaTable = connection.GetOleDbSchemaTable(
              System.Data.OleDb.OleDbSchemaGuid.Tables,
              new object[] { null, null, null, "TABLE" });

            foreach (System.Data.DataRow table in schemaTable.Rows)
                dataTable.Rows.Add(
                    table["TABLE_NAME"], 
                    "Jet", 
                    "Jet", 
                    table["TABLE_NAME"], 
                    table["TABLE_NAME"].ToString().ToLower().StartsWith("msys") ? "SYSTEM" : "USER");

            dataTable.AcceptChanges();

            return dataTable;
        }

        private static DataTable GetTableColumns(OleDbConnection connection)
        {
            DataTable dataTable = (DataTable)XmlObjectSerializer.GetObject(Properties.Resources.StoreSchemaDefinition_TableColumns, typeof(DataTable));
            Dictionary<string, string> objectsToGet = GetTablesOrViewDictionary(connection, true);

            GetTableOrViewColumns(connection, dataTable, objectsToGet);

            return dataTable;
        }

        #endregion


        #region Indexes

        private static DataTable GetIndexes(OleDbConnection connection)
        {
            DataTable dataTable = (DataTable)XmlObjectSerializer.GetObject(Properties.Resources.StoreSchemaDefinition_Indexes, typeof(DataTable));

            DataTable schemaTable = connection.GetOleDbSchemaTable(
              System.Data.OleDb.OleDbSchemaGuid.Indexes,
              new object[] {});

            foreach (System.Data.DataRow table in schemaTable.Rows)
                if (
                    Convert.ToInt32(table["ORDINAL_POSITION"]) == 1  // Only the first field of the index
                    )
                    dataTable.Rows.Add(
                        (string)table["TABLE_NAME"] + "." + (string)table["INDEX_NAME"], // Id
                        table["TABLE_NAME"], // ParentId
                        table["TABLE_NAME"], // Table
                        table["INDEX_NAME"], // Name
                        Convert.ToBoolean(table["UNIQUE"]), // IsUnique
                        Convert.ToBoolean(table["PRIMARY_KEY"]) // IsPrimary
                    );

            dataTable.AcceptChanges();

            return dataTable;
        }

        private static DataTable GetIndexColumns(OleDbConnection connection)
        {
            DataTable dataTable = (DataTable)XmlObjectSerializer.GetObject(Properties.Resources.StoreSchemaDefinition_IndexColumns, typeof(DataTable));

            DataTable schemaTable = connection.GetOleDbSchemaTable(
              System.Data.OleDb.OleDbSchemaGuid.Indexes,
              new object[] { });

            foreach (System.Data.DataRow table in schemaTable.Rows)
                    dataTable.Rows.Add(
                        (string)table["TABLE_NAME"] + "." + (string)table["INDEX_NAME"] + "." + (string)table["COLUMN_NAME"], // Id
                        table["TABLE_NAME"] + "." + (string)table["INDEX_NAME"], // ParentId
                        table["TABLE_NAME"] + "." + table["COLUMN_NAME"], // ColumnId
                        table["TABLE_NAME"], // Table
                        table["INDEX_NAME"], // Index
                        table["COLUMN_NAME"], // Name
                        Convert.ToInt32(table["ORDINAL_POSITION"]) // Ordinal
                    );

            dataTable.AcceptChanges();

            return dataTable;
        }

        #endregion



        #region Views

        private static DataTable GetViews(OleDbConnection connection)
        {
            DataTable dataTable = (DataTable)XmlObjectSerializer.GetObject(Properties.Resources.StoreSchemaDefinition_Views, typeof(DataTable));

            DataTable schemaTable = connection.GetSchema("Views");

            foreach (System.Data.DataRow table in schemaTable.Rows)
                dataTable.Rows.Add(table["TABLE_NAME"], "Jet", "Jet", table["TABLE_NAME"], table["VIEW_DEFINITION"], table["IS_UPDATABLE"]);

            dataTable.AcceptChanges();

            return dataTable;
        }

        /// <summary>
        /// Gets the views via OleDb schema table.
        /// No definition can be retrieved
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <returns></returns>
        private static DataTable GetViewsViaGetOleDbSchemaTable(OleDbConnection connection)
        {
            DataTable dataTable = (DataTable)XmlObjectSerializer.GetObject(Properties.Resources.StoreSchemaDefinition_Views, typeof(DataTable));

            DataTable schemaTable = connection.GetOleDbSchemaTable(
              System.Data.OleDb.OleDbSchemaGuid.Tables,
              new object[] { null, null, null, "VIEW" });

            foreach (System.Data.DataRow table in schemaTable.Rows)
                dataTable.Rows.Add(table["TABLE_NAME"], "Jet", "Jet", table["TABLE_NAME"], DBNull.Value, 0);

            dataTable.AcceptChanges();

            return dataTable;
        }

        private static DataTable GetViewColumns(OleDbConnection connection)
        {
            DataTable dataTable = (DataTable)XmlObjectSerializer.GetObject(Properties.Resources.StoreSchemaDefinition_ViewColumns, typeof(DataTable));
            Dictionary<string, string> objectsToGet = GetTablesOrViewDictionary(connection, false);

            GetTableOrViewColumns(connection, dataTable, objectsToGet);

            return dataTable;
        }



        #endregion

        #region Constraints


        private static DataTable GetConstraints(OleDbConnection connection)
        {
            DataTable dataTable = (DataTable)XmlObjectSerializer.GetObject(Properties.Resources.StoreSchemaDefinition_Constraints, typeof(DataTable));

            DataTable schemaTable = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Check_Constraints, new object[] { });

            foreach (System.Data.DataRow table in schemaTable.Rows)
                dataTable.Rows.Add(
                    table["CONSTRAINT_NAME"], // Id
                    DBNull.Value, // ParentId
                    table["CONSTRAINT_NAME"], // Name
                    "CHECK", // ConstraintType
                    false,  // IsDeferrable
                    false   // IsIntiallyDeferred
                    );

            schemaTable = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Foreign_Keys, new object[] {});

            foreach (System.Data.DataRow table in schemaTable.Rows)
                if (Convert.ToInt32(table["ORDINAL"]) == 1)
                    dataTable.Rows.Add(
                        table["FK_NAME"], // Id
                        table["PK_TABLE_NAME"], // ParentId
                        table["FK_NAME"], // Name
                        "FOREIGN KEY", // ConstraintType
                        table["DEFERRABILITY"],  // IsDeferrable
                        false   // IsIntiallyDeferred
                        );

            schemaTable = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Primary_Keys, new object[] { });

            foreach (System.Data.DataRow table in schemaTable.Rows)
                if (Convert.ToInt32(table["ORDINAL"]) == 1)
                    dataTable.Rows.Add(
                        table["TABLE_NAME"] + "." + table["PK_NAME"], // Id
                        table["TABLE_NAME"], // ParentId
                        table["PK_NAME"], // Name
                        "PRIMARY KEY", // ConstraintType
                        false,  // IsDeferrable
                        false   // IsIntiallyDeferred
                        );

            schemaTable = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Indexes, new object[] { });

            foreach (System.Data.DataRow table in schemaTable.Rows)
                if (
                    Convert.ToInt32(table["ORDINAL_POSITION"]) == 1  &&  // Only the first field of the index
                    Convert.ToBoolean(table["PRIMARY_KEY"]) == false && // Not a primary key
                    Convert.ToBoolean(table["UNIQUE"]) == true           // Unique constraint
                    )
                    dataTable.Rows.Add(
                        (string)table["TABLE_NAME"] + "." + (string)table["INDEX_NAME"], // Id
                        table["TABLE_NAME"], // ParentId
                        table["INDEX_NAME"], // Name
                        "UNIQUE", // ConstraintType
                        false,  // IsDeferrable
                        false   // IsIntiallyDeferred
                        );

            dataTable.AcceptChanges();

            return dataTable;
        }



        private static DataTable GetConstraintColumns(OleDbConnection connection)
        {
            DataTable dataTable = (DataTable)XmlObjectSerializer.GetObject(Properties.Resources.StoreSchemaDefinition_ConstraintColumns, typeof(DataTable));


            DataTable schemaTable = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Foreign_Keys, new object[] { });

            foreach (System.Data.DataRow table in schemaTable.Rows)
                dataTable.Rows.Add(
                    table["FK_NAME"], // ConstraintId
                    table["FK_TABLE_NAME"] + "." + table["FK_COLUMN_NAME"] // ColumnId
                    );

            schemaTable = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Primary_Keys, new object[] { });

            foreach (System.Data.DataRow table in schemaTable.Rows)
                dataTable.Rows.Add(
                    table["TABLE_NAME"] + "." + table["PK_NAME"], // ConstraintId
                    table["TABLE_NAME"] + "." + table["COLUMN_NAME"] // ColumnId
                    );

            schemaTable = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Indexes, new object[] { });

            foreach (System.Data.DataRow table in schemaTable.Rows)
                if (
                    Convert.ToBoolean(table["PRIMARY_KEY"]) == false && // Not a primary key
                    Convert.ToBoolean(table["UNIQUE"]) == true           // Unique constraint
                    )
                    dataTable.Rows.Add(
                        table["TABLE_NAME"] + "." + table["INDEX_NAME"], // ConstraintId
                        table["TABLE_NAME"] + "." + table["COLUMN_NAME"] // ColumnId
                        );

            dataTable.AcceptChanges();

            return dataTable;
        }


        #endregion

        #region CheckConstraints

        private static DataTable GetCheckConstraints(OleDbConnection connection)
        {
            DataTable dataTable = (DataTable)XmlObjectSerializer.GetObject(Properties.Resources.StoreSchemaDefinition_CheckConstraints, typeof(DataTable));

            DataTable schemaTable = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Check_Constraints, new object[] { });

            foreach (System.Data.DataRow table in schemaTable.Rows)
                dataTable.Rows.Add(
                    table["CONSTRAINT_NAME"], // Id
                    table["CHECK_CLAUSE"] // Expression
                    );

            dataTable.AcceptChanges();

            return dataTable;
        }

        #endregion

        #region Foreign Key Constraint


        private static DataTable GetForeignKeyConstraints(OleDbConnection connection)
        {
            DataTable dataTable = (DataTable)XmlObjectSerializer.GetObject(Properties.Resources.StoreSchemaDefinition_ForeignKeyConstraints, typeof(DataTable));

            DataTable schemaTable = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Foreign_Keys, new object[] { });

            foreach (System.Data.DataRow table in schemaTable.Rows)
                if (Convert.ToInt32(table["ORDINAL"]) == 1)
                    dataTable.Rows.Add(
                        table["FK_NAME"], // Id
                        table["PK_TABLE_NAME"], // ToTableId
                        table["FK_TABLE_NAME"], // FromTableId
                        table["UPDATE_RULE"], // Update rule
                        table["DELETE_RULE"] // Delete rule
                        );


            dataTable.AcceptChanges();

            return dataTable;
        }

        private static DataTable GetForeignKeyConstraintColumns(OleDbConnection connection)
        {
            DataTable dataTable = (DataTable)XmlObjectSerializer.GetObject(Properties.Resources.StoreSchemaDefinition_ForeignKeys, typeof(DataTable));

            DataTable schemaTable = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Foreign_Keys, new object[] { });

            foreach (System.Data.DataRow table in schemaTable.Rows)
                dataTable.Rows.Add(
                    table["FK_NAME"] + "." + table["ORDINAL"], // Id
                    table["PK_TABLE_NAME"] + "." + table["PK_COLUMN_NAME"], // ToColumnId
                    table["FK_TABLE_NAME"] + "." + table["FK_COLUMN_NAME"], // FromColumnId
                    table["PK_TABLE_NAME"] , // ToTable
                    table["PK_COLUMN_NAME"], // ToColumn
                    table["FK_TABLE_NAME"], // FromTable
                    table["FK_COLUMN_NAME"], // FromColumn
                    table["FK_NAME"], // ConstraintId
                    table["ORDINAL"] // Ordinal
                    );


            dataTable.AcceptChanges();

            return dataTable;
        }

        #endregion

        #region General purpose methods

        private static void GetTableOrViewColumns(OleDbConnection connection, DataTable dataTable, Dictionary<string, string> objectsToGet)
        {

            DataTable schemaTable = connection.GetOleDbSchemaTable(
              System.Data.OleDb.OleDbSchemaGuid.Columns,
              new object[] { null, null, null, null });

            foreach (System.Data.DataRow rowColumn in schemaTable.Rows)
                if (objectsToGet.ContainsKey(rowColumn["TABLE_NAME"].ToString()))
                {
                    dataTable.Rows.Add(
                        rowColumn["TABLE_NAME"] + "." + rowColumn["COLUMN_NAME"], // Id
                        rowColumn["TABLE_NAME"], // ParentId
                        rowColumn["TABLE_NAME"], // Table
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
                        GetIsIdentity(connection, rowColumn), // IsIdentity
                        Convert.ToBoolean(rowColumn["COLUMN_HASDEFAULT"]) ? 1 : 0, // IsStoreGenerated
                        rowColumn["COLUMN_DEFAULT"] // Default
                        );
                    if (Convert.ToInt32(rowColumn["COLUMN_FLAGS"]) == 0x5a && Convert.ToInt32(rowColumn["DATA_TYPE"]) == 3)
                        Console.WriteLine();
                }

            dataTable.AcceptChanges();
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
                case OleDbType.Guid:
                    return "guid";
                case OleDbType.BSTR:
                case OleDbType.Variant:
                case OleDbType.VarWChar:
                case OleDbType.VarNumeric:
                case OleDbType.Error:
                case OleDbType.DBTime:
                case OleDbType.Empty:
                case OleDbType.Filetime:
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


        static string _lastTableName;
        static DataTable _lastStructureDataTable;

        private static bool GetIsIdentity(IDbConnection connection, DataRow rowColumn)
        {
            if (Convert.ToInt32(rowColumn["COLUMN_FLAGS"]) != 0x5a || Convert.ToInt32(rowColumn["DATA_TYPE"]) != 3)
                return false;

            if (_lastTableName != (string)rowColumn["TABLE_NAME"])
            {
                _lastTableName = (string)rowColumn["TABLE_NAME"];

                // This is the standard read column for DBMS
                string sql = string.Empty;

                sql += "Select ";
                sql += "    * ";
                sql += "From ";
                sql += string.Format("    {0} ", JetProviderManifest.QuoteIdentifier(_lastTableName));
                sql += "Where ";
                sql += "    1 = 2 ";

                IDbCommand command = null;
                IDataReader dataReader = null;

                try
                {
                    command = connection.CreateCommand();
                    command.CommandText = sql;

                    dataReader = command.ExecuteReader(CommandBehavior.KeyInfo);

                    _lastStructureDataTable = dataReader.GetSchemaTable();
                }                    
                    
                finally
                {
                    // Exceptions will not be catched but these instructions will be executed anyway
                    if (command != null)
                        command.Dispose();

                    if (dataReader != null)
                        dataReader.Dispose();

                }
            }

            string columnName = (string)rowColumn["COLUMN_NAME"];

            DataRow[] fieldRows = _lastStructureDataTable.Select(string.Format("ColumnName = '{0}'", columnName.Replace("'", "''")));

            if (fieldRows.Length != 1) // 0 columns or more column with that name
            {
                Debug.Assert(false);
                return false;
            }

            DataRow fieldRow = fieldRows[0];

            return (bool)fieldRow["IsAutoIncrement"];

            /*
             * This are all the types we can use
                        c.Name = (string)fieldRow["ColumnName"];
                        c.Length = (int)fieldRow["ColumnSize"];
                        c.NumericPrecision = DBToShort(fieldRow["NumericPrecision"]);
                        c.NumericScale = DBToShort(fieldRow["NumericScale"]);
                        c.NetType = (System.Type)fieldRow["DataType"];
                        c.ProviderType = fieldRow["ProviderType"];
                        if (connection.DBType == DBType.SQLite)
                            c.IsLong = c.Length > 65535;
                        else
                            c.IsLong = (bool)fieldRow["IsLong"];
                        c.AllowDBNull = (bool)fieldRow["AllowDBNull"];
                        c.IsUnique = (bool)fieldRow["IsUnique"];

                        c.IsKey = (bool)fieldRow["IsKey"];

                        c.SchemaName = DBToString(fieldRow["BaseSchemaName"]);

                            c.CatalogName = DBToString(fieldRow["BaseCatalogName"]);

                        c.ComputeSQLDataType();

             */
        }

        #endregion


    }
}
