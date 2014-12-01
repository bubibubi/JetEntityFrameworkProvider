using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.OleDb;
using System.Diagnostics;
using System.Globalization;
using Microsoft.VisualStudio.Data.Framework;
using Microsoft.VisualStudio.Data.Framework.AdoDotNet;
using Microsoft.VisualStudio.Data.Services;
using Microsoft.VisualStudio.Data.Services.SupportEntities;

namespace JetDdexProvider
{
	/// <summary>
	/// Represents a custom data object selector to supplement or replace
	/// the schema collections supplied by the .NET Framework Data Provider.
	/// Many of the enumerations here are required for full
	/// support of the built in data design scenarios.
	/// </summary>
	class JetObjectSelector : DataObjectSelector
	{

        public JetObjectSelector() 
        { }

        #region Queries

        private const string rootEnumerationSql =
            "SELECT '{0}' AS [File] FROM (SELECT COUNT(*) FROM MSysRelationships)";

        private const string tableEnumerationSql =
            "SHOW Tables " +
            "    {0} " +
            " ORDER BY" +
            "	Name";
        private static string[] tableEnumerationDefaults =
		{
			"Name",
            "Type"
		};

        private const string tableColumnEnumerationSql =
        "SHOW " +
        "    TableColumns " +
        "    {0} " +
        "ORDER BY" +
        "	Ordinal";
        private static string[] tableColumnEnumerationDefaults =
		{
			"ParentId",
            "Name",
            "Ordinal"
		};


        private const string viewEnumerationSql =
        "SHOW " +
        "    Views " +
        "    {0} " +
        "ORDER BY" +
        "	Name";
        private static string[] viewEnumerationDefaults =
		{
			"Name",
            "Type"
		};

        private const string viewColumnEnumerationSql =
        "SHOW " +
        "    ViewColumns " +
        "    {0} " +
        "ORDER BY" +
        "	Ordinal";
        private static string[] viewColumnEnumerationDefaults =
		{
            "ParentId",
			"Name",
            "Ordinal"
		};




        private const string indexEnumerationSql =
            "SHOW " +
            "    INDEXES " +
            "    {0} " +
            "ORDER BY" +
            "	Table, Name";
        private static string[] indexEnumerationDefaults =
		{
			"Table",
			"Name"
		};

        private const string indexColumnEnumerationSql =
            "SHOW " +
            "    INDEXCOLUMNS" +
            "    {0} " +
            "ORDER BY" +
            "	Table, Index, Ordinal";
        private static string[] indexColumnEnumerationDefaults =
		{
			"Table",
			"Index",
			"Name"
		};

        private const string foreignKeyEnumerationSql =
            "SHOW FOREIGNKEYCONSTRAINTS " +
            "WHERE " +
            "    {0} " +
            " ORDER BY" +
            "	Id";
        private static string[] foreignKeyEnumerationDefaults =
		{
			"ToTableId",
			"Id"
		};

        private const string foreignKeyColumnEnumerationSql =
            "SHOW FOREIGNKEYS " +
            "WHERE" +
            "    {0} " +
            " ORDER BY" +
            "	Id, Ordinal";
        private static string[] foreignKeyColumnEnumerationDefaults =
		{
			"ToTable",
			"Id",
			"ToColumn"
		};

        #endregion

		#region Protected Methods

        /// <summary>
        /// Gets the required restrictions.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">typeName</exception>
		protected override IList<string> GetRequiredRestrictions(string typeName, object[] parameters)
		{
			if (typeName == null)
				throw new ArgumentNullException("typeName");


            // No required restrictions.
            // Required restrictions (i.e. Table name in Columns typeName) are already passed
			if (!typeName.Equals(JetObjectTypes.Root, StringComparison.OrdinalIgnoreCase))
			{
				return new string[] {};
			}
            IList<string> requiredRestrictions = base.GetRequiredRestrictions(typeName, parameters);
			return requiredRestrictions;
		}

		protected override IVsDataReader SelectObjects(string typeName, object[] restrictions, string[] properties, object[] parameters)
		{
			if (typeName == null)
				throw new ArgumentNullException("typeName");

			// Execute a SQL statement to get the property values
            DbConnection connection = Site.GetLockedProviderObject() as DbConnection;

            if (connection == null)
				throw new InvalidOperationException("Invalid provider object");

            string fileName = null;

            try
            {
                fileName = new OleDbConnectionStringBuilder(connection.ConnectionString).DataSource;
            }
            catch (Exception)
            {}

            if (string.IsNullOrWhiteSpace(fileName))
                fileName = "Unknown";

            try
			{
				// Ensure the connection is open
				if (Site.State != DataConnectionState.Open)
					Site.Open();

				// Create a command object
				DbCommand command = (DbCommand)connection.CreateCommand();

				// Choose and format SQL based on the type
				if (typeName.Equals(JetObjectTypes.Root, StringComparison.OrdinalIgnoreCase))
					command.CommandText = string.Format(rootEnumerationSql, fileName.Replace("'", "''"));
                else if (typeName.Equals(JetObjectTypes.Table, StringComparison.OrdinalIgnoreCase))
                {
                    command.CommandText = FormatSqlString(
                        tableEnumerationSql,
                        restrictions,
                        tableEnumerationDefaults);
                }
                else if (typeName.Equals(JetObjectTypes.View, StringComparison.OrdinalIgnoreCase))
                {
                    command.CommandText = FormatSqlString(
                        viewEnumerationSql,
                        restrictions,
                        viewEnumerationDefaults);
                }
                else if (restrictions.Length == 0 || !(restrictions[0] is string)) // Only nodes above this check allow no restrictions
                    throw new ArgumentException("Missing required restriction(s).");
                else if (typeName.Equals(JetObjectTypes.Column, StringComparison.OrdinalIgnoreCase))
                {
                    command.CommandText = FormatSqlString(
                        tableColumnEnumerationSql,
                        restrictions,
                        tableColumnEnumerationDefaults);
                }
                else if (typeName.Equals(JetObjectTypes.ViewColumn, StringComparison.OrdinalIgnoreCase))
                {
                    command.CommandText = FormatSqlString(
                        viewColumnEnumerationSql,
                        restrictions,
                        viewColumnEnumerationDefaults);
                }
                else if (typeName.Equals(JetObjectTypes.Index, StringComparison.OrdinalIgnoreCase))
				{
					command.CommandText = FormatSqlString(
						indexEnumerationSql,
						restrictions,
						indexEnumerationDefaults);
				}
				else if (typeName.Equals(JetObjectTypes.IndexColumn,
					StringComparison.OrdinalIgnoreCase))
				{
					command.CommandText = FormatSqlString(
						indexColumnEnumerationSql,
						restrictions,
						indexColumnEnumerationDefaults);
				}
				else if (typeName.Equals(JetObjectTypes.ForeignKey,
					StringComparison.OrdinalIgnoreCase))
				{
					command.CommandText = FormatSqlString(
						foreignKeyEnumerationSql,
						restrictions,
						foreignKeyEnumerationDefaults);
				}
				else if (typeName.Equals(JetObjectTypes.ForeignKeyColumn,
					StringComparison.OrdinalIgnoreCase))
				{
					command.CommandText = FormatSqlString(
						foreignKeyColumnEnumerationSql,
						restrictions,
						foreignKeyColumnEnumerationDefaults);
				}
				else
					throw new NotSupportedException(string.Format("Specified typeName '{0}' not supported", typeName));


				return new AdoDotNetReader(command.ExecuteReader());
			}
			finally
			{
				Site.UnlockProviderObject();
			}
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// This method formats a SQL string by specifying format arguments
		/// based on restrictions.  All enumerations require at least a
		/// database restriction, which is specified twice with different
		/// escape characters.  This is followed by each restriction in turn
		/// with the quote character escaped.  Where there is no restriction,
		/// a default restriction value is added to ensure the SQL statement
		/// is still valid.
		/// </summary>
		private static string FormatSqlString(string sql, object[] restrictions, object[] defaultRestrictions)
		{
            Debug.Assert(sql != null);

            if (restrictions == null)
                return String.Format(sql, "");

			Debug.Assert(restrictions != null);
			Debug.Assert(restrictions.Length > 0);
			Debug.Assert(defaultRestrictions != null);
			Debug.Assert(defaultRestrictions.Length >= restrictions.Length);

            string whereClause = string.Empty;
            bool first = true;
            for (int i = 0; i < restrictions.Length; i++)
            {
                if (restrictions[i] != null)
                {
                    if (first)
                        first = false;
                    else
                        whereClause += "AND ";

                    whereClause += string.Format("[{0}] = '{1}' ", defaultRestrictions[i], restrictions[i].ToString().Replace("'", "''"));
                }
            }

            if (!string.IsNullOrEmpty(whereClause))
                whereClause = " WHERE " + whereClause;

            return String.Format(sql, whereClause);
		}

		#endregion

	}
}
