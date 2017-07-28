using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity.Core.Common.CommandTrees;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Migrations.Model;
using System.Data.Entity.Migrations.Sql;
using System.Diagnostics;


namespace JetEntityFrameworkProvider
{
    /// <summary>
    /// Migration Ddl generator for Jet
    /// </summary>
    public class JetMigrationSqlGenerator : MigrationSqlGenerator
    {

        const string BATCHTERMINATOR = ";\r\n";

        /// <summary>
        /// Initializes a new instance of the <see cref="JetMigrationSqlGenerator"/> class.
        /// </summary>
        public JetMigrationSqlGenerator()
        {
            base.ProviderManifest = JetProviderServices.Instance.GetProviderManifest("Jet");
        }

        /// <summary>
        /// Converts a set of migration operations into database provider specific SQL.
        /// </summary>
        /// <param name="migrationOperations">The operations to be converted.</param>
        /// <param name="providerManifestToken">Token representing the version of the database being targeted.</param>
        /// <returns>
        /// A list of SQL statements to be executed to perform the migration operations.
        /// </returns>
        public override IEnumerable<MigrationStatement> Generate(IEnumerable<MigrationOperation> migrationOperations, string providerManifestToken)
        {
            List<MigrationStatement> migrationStatements = new List<MigrationStatement>();

            foreach (MigrationOperation migrationOperation in migrationOperations)
                migrationStatements.Add(GenerateStatement(migrationOperation));
            return migrationStatements;
        }

        private MigrationStatement GenerateStatement(MigrationOperation migrationOperation)
        {
            MigrationStatement migrationStatement = new MigrationStatement();
            migrationStatement.BatchTerminator = BATCHTERMINATOR;
            migrationStatement.Sql = GenerateSqlStatement(migrationOperation);
            return migrationStatement;
        }

        private string GenerateSqlStatement(MigrationOperation migrationOperation)
        {
            dynamic concreteMigrationOperation = migrationOperation;
            return GenerateSqlStatementConcrete(concreteMigrationOperation);
        }

        // ReSharper disable once UnusedParameter.Local
        private string GenerateSqlStatementConcrete(MigrationOperation migrationOperation)
        {
            Debug.Assert(false);
            return string.Empty;
        }


        #region History operations

        private string GenerateSqlStatementConcrete(HistoryOperation migrationOperation)
        {
            JetDdlBuilder ddlBuilder = new JetDdlBuilder();

            foreach (DbModificationCommandTree commandTree in migrationOperation.CommandTrees)
            {
                List<DbParameter> parameters;
                // Take care because here we have several queries so we can't use parameters...
                switch (commandTree.CommandTreeKind)
                {
                    case DbCommandTreeKind.Insert:
                        ddlBuilder.AppendSql(JetDmlBuilder.GenerateInsertSql((DbInsertCommandTree)commandTree, out parameters, true));
                        break;
                    case DbCommandTreeKind.Delete:
                        ddlBuilder.AppendSql(JetDmlBuilder.GenerateDeleteSql((DbDeleteCommandTree)commandTree, out parameters, true));
                        break;
                    case DbCommandTreeKind.Update:
                        ddlBuilder.AppendSql(JetDmlBuilder.GenerateUpdateSql((DbUpdateCommandTree)commandTree, out parameters, true));
                        break;
                    // ReSharper disable RedundantCaseLabel
                    case DbCommandTreeKind.Function:
                    case DbCommandTreeKind.Query:
                    // ReSharper restore RedundantCaseLabel
                    default:
                        throw new InvalidOperationException(string.Format("Command tree of type {0} not supported in migration of history operations", commandTree.CommandTreeKind));
                }
                ddlBuilder.AppendSql(BATCHTERMINATOR);
            }

            return ddlBuilder.GetCommandText();

        }

        #endregion

        #region Move operations (not supported by Jet)

        // ReSharper disable once UnusedParameter.Local
        private string GenerateSqlStatementConcrete(MoveProcedureOperation migrationOperation)
        {
            throw new NotSupportedException("Move operations not supported by Jet (Jet does not support schemas at all)");
        }

        // ReSharper disable once UnusedParameter.Local
        private string GenerateSqlStatementConcrete(MoveTableOperation migrationOperation)
        {
            throw new NotSupportedException("Move operations not supported by Jet (Jet does not support schemas at all)");
        }

        #endregion


        #region Procedure related operations (not supported by Jet)
        // ReSharper disable once UnusedParameter.Local
        private string GenerateSqlStatementConcrete(AlterProcedureOperation migrationOperation)
        {
            throw new NotSupportedException("Procedures are not supported by Jet");
        }

        // ReSharper disable once UnusedParameter.Local
        private string GenerateSqlStatementConcrete(CreateProcedureOperation migrationOperation)
        {
            throw new NotSupportedException("Procedures are not supported by Jet");
        }


        // ReSharper disable once UnusedParameter.Local
        private string GenerateSqlStatementConcrete(DropProcedureOperation migrationOperation)
        {
            throw new NotSupportedException("Procedures are not supported by Jet");
        }


        // ReSharper disable once UnusedParameter.Local
        private string GenerateSqlStatementConcrete(RenameProcedureOperation migrationOperation)
        {
            throw new NotSupportedException("Procedures are not supported by Jet");
        }

        #endregion


        #region Rename operations (not supported by Jet)


        // ReSharper disable once UnusedParameter.Local
        private string GenerateSqlStatementConcrete(RenameColumnOperation migrationOperation)
        {
            throw new NotSupportedException("Cannot rename objects with Jet");
        }

        // ReSharper disable once UnusedParameter.Local
        private string GenerateSqlStatementConcrete(RenameIndexOperation migrationOperation)
        {
            throw new NotSupportedException("Cannot rename objects with Jet");
        }

        // ReSharper disable once UnusedParameter.Local
        private string GenerateSqlStatementConcrete(RenameTableOperation migrationOperation)
        {
            throw new NotSupportedException("Cannot rename objects with Jet");

            // Here there is a working example but we should implement all the constraints and table related objects updates
            /*
            JetDdlBuilder ddlBuilder = new JetDdlBuilder();

            ddlBuilder.AppendSql("SELECT * FROM ");
            ddlBuilder.AppendIdentifier(migrationOperation.Name);
            ddlBuilder.AppendSql(" INTO ");
            ddlBuilder.AppendIdentifier(migrationOperation.NewName);

            ddlBuilder.AppendSql(BATCHTERMINATOR);

            ddlBuilder.AppendSql("DROP TABLE ");
            ddlBuilder.AppendIdentifier(migrationOperation.Name);

            return ddlBuilder.GetCommandText();
            */

        }

        #endregion

        #region Columns
        private string GenerateSqlStatementConcrete(AddColumnOperation migrationOperation)
        {
            JetDdlBuilder ddlBuilder = new JetDdlBuilder();

            ddlBuilder.AppendSql("ALTER TABLE ");
            ddlBuilder.AppendIdentifier(migrationOperation.Table);
            ddlBuilder.AppendSql(" ADD COLUMN ");

            ColumnModel column = migrationOperation.Column;

            ddlBuilder.AppendIdentifier(column.Name);
            ddlBuilder.AppendSql(" ");
            TypeUsage storeType = JetProviderManifest.Instance.GetStoreType(column.TypeUsage);
            ddlBuilder.AppendType(storeType, column.IsNullable ?? true, column.IsIdentity, column.DefaultValueSql);
            ddlBuilder.AppendNewLine();


            return ddlBuilder.GetCommandText();
        }

        private string GenerateSqlStatementConcrete(DropColumnOperation migrationOperation)
        {
            JetDdlBuilder ddlBuilder = new JetDdlBuilder();

            ddlBuilder.AppendSql("ALTER TABLE ");
            ddlBuilder.AppendIdentifier(migrationOperation.Table);
            ddlBuilder.AppendSql(" DROP COLUMN ");
            ddlBuilder.AppendIdentifier(migrationOperation.Name);

            return ddlBuilder.GetCommandText();

        }

        private string GenerateSqlStatementConcrete(AlterColumnOperation migrationOperation)
        {
            JetDdlBuilder ddlBuilder = new JetDdlBuilder();

            ddlBuilder.AppendSql("ALTER TABLE ");
            ddlBuilder.AppendIdentifier(migrationOperation.Table);
            ddlBuilder.AppendSql(" ALTER COLUMN ");

            ColumnModel column = migrationOperation.Column;

            ddlBuilder.AppendIdentifier(column.Name);
            ddlBuilder.AppendSql(" ");
            TypeUsage storeType = JetProviderManifest.Instance.GetStoreType(column.TypeUsage);
            ddlBuilder.AppendType(storeType, column.IsNullable ?? true, column.IsIdentity, column.DefaultValueSql);
            ddlBuilder.AppendNewLine();


            return ddlBuilder.GetCommandText();
        }

        #endregion


        #region Foreign keys creation

        private string GenerateSqlStatementConcrete(AddForeignKeyOperation migrationOperation)
        {
            JetDdlBuilder ddlBuilder = new JetDdlBuilder();
            ddlBuilder.AppendSql("ALTER TABLE ");
            ddlBuilder.AppendIdentifier(migrationOperation.DependentTable);
            ddlBuilder.AppendSql(" ADD CONSTRAINT ");
            ddlBuilder.AppendIdentifier(migrationOperation.Name.Replace("dbo.", "").Replace("Jet.", ""));
            ddlBuilder.AppendSql(" FOREIGN KEY (");
            ddlBuilder.AppendIdentifierList(migrationOperation.DependentColumns);
            ddlBuilder.AppendSql(")");
            ddlBuilder.AppendSql(" REFERENCES ");
            ddlBuilder.AppendIdentifier(migrationOperation.PrincipalTable);
            ddlBuilder.AppendSql(" (");
            ddlBuilder.AppendIdentifierList(migrationOperation.PrincipalColumns);
            ddlBuilder.AppendSql(")");

            return ddlBuilder.GetCommandText();
        }

        #endregion

        #region Primary keys creation

        private string GenerateSqlStatementConcrete(AddPrimaryKeyOperation migrationOperation)
        {

            JetDdlBuilder ddlBuilder = new JetDdlBuilder();
            ddlBuilder.AppendSql("ALTER TABLE ");
            ddlBuilder.AppendIdentifier(migrationOperation.Table);
            ddlBuilder.AppendSql(" ADD CONSTRAINT ");

            // respect name given to primary keys in migrations
            string pkName = migrationOperation.HasDefaultName ?
                ddlBuilder.CreateConstraintName("PK", migrationOperation.Table) : // Take care because here names starts with "dbo."
                migrationOperation.Name;

            ddlBuilder.AppendIdentifier(pkName);
            ddlBuilder.AppendSql(" PRIMARY KEY (");
            ddlBuilder.AppendIdentifierList(migrationOperation.Columns);
            ddlBuilder.AppendSql(")");
            return ddlBuilder.GetCommandText();
        }

        #endregion

        #region Table operations

        private string GenerateSqlStatementConcrete(AlterTableOperation migrationOperation)
        {
            JetDdlBuilder ddlBuilder = new JetDdlBuilder();


            foreach (ColumnModel column in migrationOperation.Columns)
            {
                ddlBuilder.AppendSql("ALTER TABLE ");
                ddlBuilder.AppendIdentifier(migrationOperation.Name);
                ddlBuilder.AppendSql(" ALTER COLUMN ");

                ddlBuilder.AppendIdentifier(column.Name);
                ddlBuilder.AppendSql(" ");
                TypeUsage storeType = JetProviderManifest.Instance.GetStoreType(column.TypeUsage);
                ddlBuilder.AppendType(storeType, column.IsNullable ?? true, column.IsIdentity, column.DefaultValueSql);
                ddlBuilder.AppendSql(BATCHTERMINATOR);
            }

            return ddlBuilder.GetCommandText();
        }

        private string GenerateSqlStatementConcrete(CreateTableOperation migrationOperation)
        {
            JetDdlBuilder ddlBuilder = new JetDdlBuilder();

            ddlBuilder.AppendSql("CREATE TABLE ");
            ddlBuilder.AppendIdentifier(migrationOperation.Name);
            ddlBuilder.AppendSql(" (");
            ddlBuilder.AppendNewLine();

            bool first = true;
            foreach (ColumnModel column in migrationOperation.Columns)
            {
                if (first)
                    first = false;
                else
                    ddlBuilder.AppendSql(",");

                ddlBuilder.AppendSql(" ");
                ddlBuilder.AppendIdentifier(column.Name);
                ddlBuilder.AppendSql(" ");
                TypeUsage storeType = JetProviderManifest.Instance.GetStoreType(column.TypeUsage);
                ddlBuilder.AppendType(storeType, column.IsNullable ?? true, column.IsIdentity, column.DefaultValueSql);
                ddlBuilder.AppendNewLine();
            }

            ddlBuilder.AppendSql(")");

            if (migrationOperation.PrimaryKey != null)
            {
                ddlBuilder.AppendSql(BATCHTERMINATOR);
                ddlBuilder.AppendSql(GenerateSqlStatementConcrete(migrationOperation.PrimaryKey));
            }

            return ddlBuilder.GetCommandText();
        }

        #endregion

        #region Index

        private string GenerateSqlStatementConcrete(CreateIndexOperation migrationOperation)
        {
            JetDdlBuilder ddlBuilder = new JetDdlBuilder();
            ddlBuilder.AppendSql("CREATE ");
            if (migrationOperation.IsUnique)
                ddlBuilder.AppendSql("UNIQUE ");
            ddlBuilder.AppendSql("INDEX ");
            ddlBuilder.AppendIdentifier(migrationOperation.Name);
            ddlBuilder.AppendSql(" ON ");
            ddlBuilder.AppendIdentifier(migrationOperation.Table);
            ddlBuilder.AppendSql(" (");
            ddlBuilder.AppendIdentifierList(migrationOperation.Columns);
            ddlBuilder.AppendSql(")");

            return ddlBuilder.GetCommandText();
        }

        #endregion

        #region Drop

        private string GenerateSqlStatementConcrete(DropForeignKeyOperation migrationOperation)
        {
            JetDdlBuilder ddlBuilder = new JetDdlBuilder();
            ddlBuilder.AppendSql("ALTER TABLE ");
            ddlBuilder.AppendIdentifier(migrationOperation.PrincipalTable);
            ddlBuilder.AppendSql(" DROP CONSTRAINT ");
            ddlBuilder.AppendIdentifier(migrationOperation.Name);
            return ddlBuilder.GetCommandText();

        }

        private string GenerateSqlStatementConcrete(DropPrimaryKeyOperation migrationOperation)
        {
            JetDdlBuilder ddlBuilder = new JetDdlBuilder();
            ddlBuilder.AppendSql("ALTER TABLE ");
            ddlBuilder.AppendIdentifier(migrationOperation.Table);
            ddlBuilder.AppendSql(" DROP CONSTRAINT ");
            ddlBuilder.AppendIdentifier(migrationOperation.Name);
            return ddlBuilder.GetCommandText();
        }

        private string GenerateSqlStatementConcrete(DropIndexOperation migrationOperation)
        {
            JetDdlBuilder ddlBuilder = new JetDdlBuilder();
            ddlBuilder.AppendSql("DROP INDEX ");
            ddlBuilder.AppendIdentifier(migrationOperation.Name);
            ddlBuilder.AppendSql(" ON ");
            ddlBuilder.AppendIdentifier(migrationOperation.Table);
            return ddlBuilder.GetCommandText();
        }

        private string GenerateSqlStatementConcrete(DropTableOperation migrationOperation)
        {
            JetDdlBuilder ddlBuilder = new JetDdlBuilder();
            ddlBuilder.AppendSql("DROP TABLE ");
            ddlBuilder.AppendIdentifier(migrationOperation.Name);
            return ddlBuilder.GetCommandText();
        }

        #endregion

        #region Direct SQL statements

        private string GenerateSqlStatementConcrete(SqlOperation migrationOperation)
        {
            return migrationOperation.Sql;
        }

        #endregion
    }
}
