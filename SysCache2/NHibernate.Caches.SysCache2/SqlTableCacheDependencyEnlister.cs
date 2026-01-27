using System;
using System.Runtime.Caching;

namespace NHibernate.Caches.SysCache2
{
	/// <summary>
	/// Creates SqlChangeMonitor objects dependent on data changes in a table and registers the dependency for
	/// change notifications if necessary.
	/// </summary>
	public class SqlTableCacheDependencyEnlister : ICacheDependencyEnlister
	{
		/// <summary>The name of the database entry to use for connection info.</summary>
		private readonly string databaseEntryName;

		/// <summary>The name of the table to monitor.</summary>
		private readonly string tableName;

		/// <summary>The connection string provider.</summary>
		private readonly IConnectionStringProvider connectionStringProvider;

		/// <summary>
		/// Initializes a new instance of the <see cref="SqlTableCacheDependencyEnlister"/> class.
		/// </summary>
		/// <param name="tableName">Name of the table to monitor.</param>
		/// <param name="databaseEntryName">The name of the database entry to use for connection information.</param>
		/// <param name="connectionStringProvider">The connection string provider.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="tableName"/> or
		/// <paramref name="databaseEntryName"/> is null or empty.</exception>
		public SqlTableCacheDependencyEnlister(string tableName, string databaseEntryName, IConnectionStringProvider connectionStringProvider)
		{
			//validate the params
			if (String.IsNullOrEmpty(tableName))
			{
				throw new ArgumentNullException("tableName");
			}

			if (String.IsNullOrEmpty(databaseEntryName))
			{
				throw new ArgumentNullException("databaseEntryName");
			}

			if (connectionStringProvider == null)
			{
				throw new ArgumentNullException("connectionStringProvider");
			}

			this.tableName = tableName;
			this.databaseEntryName = databaseEntryName;
			this.connectionStringProvider = connectionStringProvider;
		}

		#region ICacheDependencyEnlister Members

		/// <summary>
		/// Enlists a cache dependency to recieve change notifciations with an underlying resource.
		/// </summary>
		/// <returns>
		/// The cache dependency linked to the notification subscription.
		/// </returns>
		public ChangeMonitor Enlist()
		{
			var connectionString = String.IsNullOrEmpty(databaseEntryName)
				? connectionStringProvider.GetConnectionString()
				: connectionStringProvider.GetConnectionString(databaseEntryName);

			return new SqlTableChangeMonitor(tableName, connectionString);
		}

		#endregion
	}
}
