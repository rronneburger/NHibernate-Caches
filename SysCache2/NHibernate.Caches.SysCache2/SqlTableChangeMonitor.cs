using System;
using System.Data;
using System.Data.SqlClient;
using System.Runtime.Caching;
using System.Threading;

namespace NHibernate.Caches.SysCache2
{
    /// <summary>
    /// Custom ChangeMonitor that polls a SQL Server table for changes using the same mechanism as System.Web.Caching.SqlCacheDependency.
    /// </summary>
    public class SqlTableChangeMonitor : ChangeMonitor
    {
        private readonly string _connectionString;
        private readonly string _tableName;
        private readonly string _uniqueId;
        private Timer _timer;
        private int _version = -1;
        private bool _disposed;
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5); // Default polling interval

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlTableChangeMonitor"/> class.
        /// </summary>
        /// <param name="tableName">Name of the table to monitor.</param>
        /// <param name="connectionString">The connection string.</param>
        public SqlTableChangeMonitor(string tableName, string connectionString)
        {
            _tableName = tableName;
            _connectionString = connectionString;
            _uniqueId = Guid.NewGuid().ToString();

            // Initial poll to get the current version
            _version = GetTableVersion();

            // Start polling
            _timer = new Timer(Poll, null, PollInterval, PollInterval);

            InitializationComplete();
        }

        /// <inheritdoc />
        public override string UniqueId => _uniqueId;

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_timer != null)
                    {
                        _timer.Dispose();
                        _timer = null;
                    }
                }
                _disposed = true;
            }
        }

        private void Poll(object state)
        {
            if (_disposed) return;

            try
            {
                int currentVersion = GetTableVersion();
                if (currentVersion != _version)
                {
                    OnChanged(null);
                    _timer?.Dispose();
                    _timer = null;
                }
            }
            catch
            {
                // If polling fails, we might want to invalidate just in case, or keep trying.
                // System.Web implementation usually invalidates on error to be safe.
                OnChanged(null);
                _timer?.Dispose();
                _timer = null;
            }
        }

        private int GetTableVersion()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new System.Data.SqlClient.SqlCommand("AspNet_SqlCachePollingStoredProcedure", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string tableName = reader.GetString(0);
                            if (string.Equals(tableName, _tableName, StringComparison.OrdinalIgnoreCase))
                            {
                                return reader.GetInt32(1);
                            }
                        }
                    }
                }
            }
            return -1;
        }
    }
}
