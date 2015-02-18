using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.Common;
using System.Data.Entity.Core.Common;
using System.Data.Entity.Core.Common.CommandTrees;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.OleDb;
using System.IO;
using System.Data.Entity.Infrastructure.DependencyResolution;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations.Sql;

namespace JetEntityFrameworkProvider
{

    /// <summary>
    /// Notes:
    /// The ProviderServices class is:
    /// - the starting point for accessing the SQL generation layer 
    /// - retrieving point of the provider manifest
    /// - database handling not supported by Jet
    /// </summary>
    class JetProviderServices : DbProviderServices
    {
        internal static readonly JetProviderServices Instance = new JetProviderServices();

        internal const string PROVIDERINVARIANTNAME = "JetEntityFrameworkProvider";

        public JetProviderServices()
        {
            AddDependencyResolver(new SingletonDependencyResolver<IDbConnectionFactory>(new JetConnectionFactory()));

            // Adding a DbMigrationSqlGenerator, all the tables are created in this way
            AddDependencyResolver(new SingletonDependencyResolver<Func<MigrationSqlGenerator>>(() => new JetMigrationSqlGenerator(), PROVIDERINVARIANTNAME));

#warning in framework 6.1 delivered on Dec 2014 there is not TableExistenceChecker. It's only in the source code of EF6.1
            //AddDependencyResolver(new SingletonDependencyResolver<System.Data.Entity.Infrastructure.TableExistenceChecker>(new JetTableExistenceChecker()));

        }

        protected override DbCommandDefinition CreateDbCommandDefinition(DbProviderManifest manifest, DbCommandTree commandTree)
        {
            DbCommand prototype = CreateCommand(manifest, commandTree);
            DbCommandDefinition result = base.CreateCommandDefinition(prototype);
            return result;
        }

        /// <summary>
        /// Create a DbCommand object, given the provider manifest and command tree
        /// </summary>
        private DbCommand CreateCommand(DbProviderManifest manifest, DbCommandTree commandTree)
        {
            if (manifest == null)
                throw new ArgumentNullException("manifest");

            if (commandTree == null)
                throw new ArgumentNullException("commandTree");

            JetProviderManifest jetManifest = (manifest as JetProviderManifest);
            if (jetManifest == null)
                throw new ArgumentException("The provider manifest given is not of type 'JetProviderManifest'.");

            JetCommand command = new JetCommand();

            List<DbParameter> parameters;
            CommandType commandType;

            command.CommandText = SqlGenerator.GenerateSql(commandTree, out parameters, out commandType);
            command.CommandType = commandType;

            //if (command.CommandType == CommandType.Text)
            //    command.CommandText += Environment.NewLine + Environment.NewLine + "-- provider: " + this.GetType().Assembly.FullName;

            // Get the function (if any) implemented by the command tree since this influences our interpretation of parameters
            EdmFunction function = null;
            if (commandTree is DbFunctionCommandTree)
            {
                function = ((DbFunctionCommandTree)commandTree).EdmFunction;
            }

            // Now make sure we populate the command's parameters from the CQT's parameters:
            foreach (KeyValuePair<string, TypeUsage> queryParameter in commandTree.Parameters)
            {
                OleDbParameter parameter;

                // Use the corresponding function parameter TypeUsage where available (currently, the SSDL facets and 
                // type trump user-defined facets and type in the EntityCommand).
                FunctionParameter functionParameter;
                if (function != null && function.Parameters.TryGetValue(queryParameter.Key, false, out functionParameter))
                    parameter = CreateJetParameter(functionParameter.Name, functionParameter.TypeUsage, functionParameter.Mode, DBNull.Value);
                else
                    parameter = CreateJetParameter(queryParameter.Key, queryParameter.Value, ParameterMode.In, DBNull.Value);

                command.Parameters.Add(parameter);
            }

            // Now add parameters added as part of SQL gen (note: this feature is only safe for DML SQL gen which
            // does not support user parameters, where there is no risk of name collision)
            if (parameters != null && parameters.Count > 0)
            {
                if (!(commandTree is DbInsertCommandTree) &&
                    !(commandTree is DbUpdateCommandTree) &&
                    !(commandTree is DbDeleteCommandTree))
                {
                    throw new InvalidOperationException("SqlGenParametersNotPermitted");
                }

                foreach (DbParameter parameter in parameters)
                {
                    command.Parameters.Add(parameter);
                }
            }

            return command;
        }

        /// <summary>
        /// Sets the parameter value and appropriate facets for the given <see cref="T:System.Data.Entity.Core.Metadata.Edm.TypeUsage" />.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        /// <param name="parameterType">The type of the parameter.</param>
        /// <param name="value">The value of the parameter.</param>
        protected override void SetDbParameterValue(DbParameter parameter, TypeUsage parameterType, object value)
        {
            // Ensure a value that can be used with SqlParameter
            parameter.Value = EnsureJetParameterValue(value);
        }

        /// <summary>
        /// Returns provider manifest token given a connection.
        /// </summary>
        /// <param name="connection">Connection to provider.</param>
        /// <returns>
        /// The provider manifest token for the specified connection.
        /// </returns>
        /// <exception cref="System.ArgumentException">
        /// connection
        /// or
        /// The connection is not of type 'JetConnection'.
        /// or
        /// Could not determine storage version; a valid storage connection or a version hint is required.
        /// </exception>
        protected override string GetDbProviderManifestToken(DbConnection connection)
        {
            if (connection == null)
                throw new ArgumentException("connection");

            JetConnection jetConnection = connection as JetConnection;
            if (jetConnection == null)
                throw new ArgumentException("The connection is not of type 'JetConnection'.");

            if (string.IsNullOrEmpty(jetConnection.ConnectionString))
                throw new ArgumentException("Could not determine storage version because the connection string is empty; a valid storage connection or a version hint is required.");

            return "Jet";
        }

        protected override DbProviderManifest GetDbProviderManifest(string versionHint)
        {
            if (string.IsNullOrEmpty(versionHint))
                throw new ArgumentNullException("versionHint", "Could not determine store version; a valid store connection or a version hint is required.");

            return JetProviderManifest.Instance;
        }

        protected override string DbCreateDatabaseScript(string providerManifestToken, StoreItemCollection storeItemCollection)
        {
            if (providerManifestToken == null)
                throw new ArgumentNullException("providerManifestToken must not be null");

            if( storeItemCollection == null)
                throw new ArgumentNullException("storeItemCollection must not be null");

            return JetCreateDatabaseSqlGenerator.CreateObjectsScript(storeItemCollection);            
        }

        /// <summary>
        /// Creates a database indicated by connection and creates schema objects (tables, primary keys, foreign keys) based on the contents of a StoreItemCollection.
        /// Note: in EF 6.1 this is not called if the provider implements Migration classes
        /// Note: we can't create database for Jet Connections
        /// </summary>
        /// <param name="connection">Connection to a non-existent database that needs to be created and populated with the store objects indicated with the storeItemCollection parameter.</param>
        /// <param name="commandTimeout">Execution timeout for any commands needed to create the database.</param>
        /// <param name="storeItemCollection">The collection of all store items based on which the script should be created.</param>
        /// <exception cref="System.ArgumentNullException">
        /// connection must not be null
        /// or
        /// storeItemCollection must not be null
        /// </exception>
        /// <exception cref="System.ArgumentException">The connection is not of type 'JetConnection'.</exception>
        protected override void DbCreateDatabase(DbConnection connection, int? commandTimeout, StoreItemCollection storeItemCollection)
        {
            if (connection == null)
                throw new ArgumentNullException("connection must not be null");

            if (storeItemCollection == null)
                throw new ArgumentNullException("storeItemCollection must not be null");

            JetConnection jetConnection = connection as JetConnection;
            if (jetConnection == null)
                throw new ArgumentException("The connection is not of type 'JetConnection'.");



            ConnectionState oldConnectionState = connection.State;

            if (oldConnectionState == ConnectionState.Closed)
                connection.Open();

            foreach (EntityContainer container in storeItemCollection.GetItems<EntityContainer>())
            {
                var entitySets = container.BaseEntitySets.OfType<EntitySet>().OrderBy(s => s.Name);

                foreach (EntitySet entitySet in container.BaseEntitySets.OfType<EntitySet>().OrderBy(s => s.Name))
                {
                    string createObjectScript = JetCreateDatabaseSqlGenerator.CreateObjectScript(entitySet);
                    jetConnection.CreateCommand(createObjectScript, commandTimeout).ExecuteNonQuery();
                }

                foreach (AssociationSet associationSet in container.BaseEntitySets.OfType<AssociationSet>().OrderBy(s => s.Name))
                {
                    string createObjectScript = JetCreateDatabaseSqlGenerator.CreateObjectScript(associationSet);
                    jetConnection.CreateCommand(createObjectScript, commandTimeout).ExecuteNonQuery();
                }
            }

            if (oldConnectionState == ConnectionState.Closed)
                connection.Close();

        }


        /// <summary>
        /// Returns a value indicating whether a given database exists on the server.
        /// </summary>
        /// <param name="connection">Connection to a database whose existence is checked by this method.</param>
        /// <param name="commandTimeout">Execution timeout for any commands needed to determine the existence of the database.</param>
        /// <param name="storeItemCollection">The collection of all store items from the model. This parameter is no longer used for determining database existence.</param>
        /// <returns>
        /// True if the provider can deduce the database only based on the connection.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// connection must not be null
        /// or
        /// storeItemCollection must not be null
        /// </exception>
        /// <exception cref="System.ArgumentException">connection must be a valid JetConnection</exception>
        protected override bool DbDatabaseExists(DbConnection connection, int? commandTimeout, StoreItemCollection storeItemCollection)
        {
            if (connection == null)
                throw new ArgumentNullException("connection must not be null");

            if (storeItemCollection == null)
                throw new ArgumentNullException("storeItemCollection must not be null");

            JetConnection jetConnection = connection as JetConnection;
            if (jetConnection == null)
                throw new ArgumentException("connection must be a valid JetConnection");

            // No database handling provided for Jet but we need to know if there is at least the migration history table
            return jetConnection.TableExists(System.Data.Entity.Migrations.History.HistoryContext.DefaultTableName);
        }       

        protected override void DbDeleteDatabase(DbConnection connection, int? commandTimeout, StoreItemCollection storeItemCollection)
        {
            if (connection == null)
                throw new ArgumentNullException("connection must not be null");

            if (storeItemCollection == null)
                throw new ArgumentNullException("storeItemCollection must not be null");

            JetConnection jetConnection = connection as JetConnection;
            if (jetConnection == null)
                throw new ArgumentException("connection must be a valid JetConnection");

            // No database handling provided for Jet
        }


        /// <summary>
        /// Creates a OleDbParameter given a name, type, and direction
        /// </summary>
        internal static OleDbParameter CreateJetParameter(string name, TypeUsage type, ParameterMode mode, object value)
        {
            int? size;

            value = EnsureJetParameterValue(value);

            OleDbParameter result = new OleDbParameter(name, value);

            // .Direction
            result.Direction = MetadataHelpers.ParameterModeToParameterDirection(mode);

            // .Size and .SqlDbType
            // output parameters are handled differently (we need to ensure there is space for return
            // values where the user has not given a specific Size/MaxLength)
            bool isOutParam = mode != ParameterMode.In;

            string udtTypeName;
            result.OleDbType = GetJetDbType(type, isOutParam, out size, out udtTypeName);
            // JET: result.UdtTypeName = udtTypeName;

            // Note that we overwrite 'facet' parameters where either the value is different or
            // there is an output parameter.
            if (size.HasValue && (isOutParam || result.Size != size.Value))
                result.Size = size.Value;

            // .IsNullable
            bool isNullable = type.GetIsNullable();
            if (isOutParam || isNullable != result.IsNullable)
                result.IsNullable = isNullable;

            return result;
        }

        /// <summary>
        /// Converts DbGeography/DbGeometry values to corresponding Sql Server spatial values.
        /// </summary>
        /// <param name="value">value to convert</param>
        /// <returns>Sql Server spatial value for DbGeometry/DbGeography or <paramref name="value"/>.</returns>
        internal static object EnsureJetParameterValue(object value)
        {
            if (value != null &&
                value != DBNull.Value &&
                Type.GetTypeCode(value.GetType()) == TypeCode.Object)
            {
                /*
                // If the parameter is being created based on an actual value (typically for constants found in DML expressions) then a DbGeography/DbGeometry
                // value must be replaced by an an appropriate Microsoft.SqlServer.Types.SqlGeography/SqlGeometry instance. Since the DbGeography/DbGeometry
                // value may not have been originally created by this SqlClient provider services implementation, just using the ProviderValue is not sufficient.
                DbGeography geographyValue = value as DbGeography;
                if (geographyValue != null)
                {
                    value = SqlTypes.ConvertToSqlTypesGeography(geographyValue);
                }
                else
                {
                    DbGeometry geometryValue = value as DbGeometry;
                    if (geometryValue != null)
                    {
                        value = SqlTypes.ConvertToSqlTypesGeometry(geometryValue);
                    }
                }
                 * */
            }

            return value;
        }

        /// <summary>
        /// Determines SqlDbType for the given primitive type. Extracts facet
        /// information as well.
        /// </summary>
        private static OleDbType GetJetDbType(TypeUsage type, bool isOutParam, out int? size, out string udtName)
        {
            // only supported for primitive type
            PrimitiveTypeKind primitiveTypeKind = type.GetPrimitiveTypeKind();

            size = default(int?);
            udtName = null;

            // TODO add logic for Xml here
            switch (primitiveTypeKind)
            {
                case PrimitiveTypeKind.Binary:
                    // for output parameters, ensure there is space...
                    size = GetParameterSize(type, isOutParam);
                    return GetBinaryDbType(type);

                case PrimitiveTypeKind.Boolean:
                    return OleDbType.Boolean;

                case PrimitiveTypeKind.Byte:
                    return OleDbType.TinyInt;

                case PrimitiveTypeKind.Time:
                    return OleDbType.Date;

                case PrimitiveTypeKind.DateTimeOffset:
                    return OleDbType.Date;

                case PrimitiveTypeKind.DateTime:
                    return OleDbType.Date;

                case PrimitiveTypeKind.Decimal:
                    return OleDbType.Decimal;

                case PrimitiveTypeKind.Double:
                    return OleDbType.Double;

                case PrimitiveTypeKind.Guid:
                    return OleDbType.Guid;

                case PrimitiveTypeKind.Int16:
                    return OleDbType.SmallInt;

                case PrimitiveTypeKind.Int32:
                    return OleDbType.Integer;

                case PrimitiveTypeKind.Int64:
                    return OleDbType.BigInt;

                case PrimitiveTypeKind.SByte:
                    return OleDbType.SmallInt;

                case PrimitiveTypeKind.Single:
                    return OleDbType.Single;

                case PrimitiveTypeKind.String:
                    size = GetParameterSize(type, isOutParam);
                    return GetStringDbType(type);
                default:
                    throw new InvalidOperationException("unknown PrimitiveTypeKind " + primitiveTypeKind);
            }
        }

        /// <summary>
        /// Determines preferred value for SqlParameter.Size. Returns null
        /// where there is no preference.
        /// </summary>
        private static int? GetParameterSize(TypeUsage type, bool isOutParam)
        {
            int maxLength;
            if (type.TryGetMaxLength(out maxLength))
            {
                // if the MaxLength facet has a specific value use it
                return maxLength;
            }
            else if (isOutParam)
            {
                // if the parameter is a return/out/inout parameter, ensure there 
                // is space for any value
                return int.MaxValue;
            }
            else
            {
                // no value
                return default(int?);
            }
        }

        /// <summary>
        /// Chooses the appropriate OleDbType for the given string type.
        /// </summary>
        private static OleDbType GetStringDbType(TypeUsage tu)
        {
            if (tu.EdmType.BuiltInTypeKind != BuiltInTypeKind.PrimitiveType || PrimitiveTypeKind.String != ((PrimitiveType)tu.EdmType).PrimitiveTypeKind)
                throw new ArgumentException("GetStringDbType require a TypeUsage of string type", "tu");

            OleDbType dbType;
            if (tu.EdmType.Name.ToLowerInvariant() == "xml")
            {
                // JET: dbType = OleDbType.Xml;
                dbType = OleDbType.LongVarChar;
            }
            else
            {
                // Specific type depends on whether the string is a unicode string and whether it is a fixed length string.
                // By default, assume widest type (unicode) and most common type (variable length)
                bool unicode;
                bool fixedLength;
                if (!tu.TryGetIsFixedLength(out fixedLength))
                    fixedLength = false;

                unicode = tu.GetIsUnicode(); // This should change return type to NVarChar/NChar but in Jet everything is in unicode

                if (fixedLength)
                    dbType = OleDbType.Char;
                else
                    dbType = OleDbType.VarChar;
            }
            return dbType;
        }

        /// <summary>
        /// Chooses the appropriate OleDbType for the given binary type.
        /// </summary>
        private static OleDbType GetBinaryDbType(TypeUsage type)
        {
            if (type.EdmType.BuiltInTypeKind != BuiltInTypeKind.PrimitiveType || PrimitiveTypeKind.Binary != ((PrimitiveType)type.EdmType).PrimitiveTypeKind)
                throw new ArgumentException("GetBinaryDbType require a TypeUsage of binary type", "tu");

            // Specific type depends on whether the binary value is fixed length. By default, assume variable length.
            bool fixedLength;
            if (!type.TryGetIsFixedLength(out fixedLength))
                fixedLength = false;

            return fixedLength ? OleDbType.Binary : OleDbType.VarBinary;
        }
    }
}

