using System;
using System.Diagnostics;
using System.Data.SqlClient;
using Microsoft.VisualStudio.Data.Services;
using Microsoft.VisualStudio.Data.Framework.AdoDotNet;
using System.Data.Common;

namespace JetDdexProvider
{
	/// <summary>
	/// Represents a custom data source information class that is able to
	/// provide data source information values that require some form of
	/// computation, perhaps based on an active connection.
	/// </summary>
	class JetSourceInformation : AdoDotNetSourceInformation
	{

		public JetSourceInformation()
		{
			AddProperty(DefaultSchema);
		}


		#region Protected Methods

		/// <summary>
		/// RetrieveValue is called once per property that was identified
		/// as existing but without a value (specified in the constructor).
		/// </summary>
		protected override object RetrieveValue(string propertyName)
		{
			if (propertyName.Equals(DefaultSchema,
					StringComparison.OrdinalIgnoreCase))
			{
				if (Site.State != DataConnectionState.Open)
					Site.Open();

                DbConnection connection = Connection as DbConnection;

                if (connection == null)
                    throw new InvalidOperationException("Connection not set or invalid connection object type");
				
                if (connection != null)
				{
					DbCommand command = connection.CreateCommand();
					try
					{
						command.CommandText = "SELECT SCHEMA_NAME()";
						return "Jet";
					}
					catch (SqlException)
					{
						// We let the base class apply default behavior
					}
				}
			}
			return base.RetrieveValue(propertyName);
		}

		#endregion
	}
}
