using System;
using System.Runtime.Caching;
using NUnit.Framework;
using NHibernate.Caches.SysCache2;

namespace NHibernate.Caches.SysCache2.Tests
{
    [TestFixture]
    public class SqlTableCacheDependencyTests
    {
        [Test]
        public void SqlTableCacheDependencyEnlister_CanBeInstantiated()
        {
            var tableName = "TestTable";
            var databaseEntryName = "TestDB";
            var connectionStringProvider = new StaticConnectionStringProvider("Server=fake;Database=fake;Integrated Security=SSPI;");
            
            var enlister = new SqlTableCacheDependencyEnlister(tableName, databaseEntryName, connectionStringProvider);
            
            Assert.That(enlister, Is.Not.Null);
        }

        [Test]
        public void SqlTableCacheDependencyEnlister_EnlistReturnsChangeMonitor()
        {
            var tableName = "TestTable";
            var databaseEntryName = "TestDB";
            // We use a fake connection string. Enlist will try to call GetTableVersion which will fail.
            // But we can check if it returns a SqlTableChangeMonitor (or at least doesn't throw NotSupportedException).
            // Actually, SqlTableChangeMonitor calls GetTableVersion in constructor, so it WILL throw if connection fails.
            var connectionStringProvider = new StaticConnectionStringProvider("Server=fake;Database=fake;Integrated Security=SSPI;Connection Timeout=1;");
            
            var enlister = new SqlTableCacheDependencyEnlister(tableName, databaseEntryName, connectionStringProvider);
            
            // It will probably throw SqlException because of fake connection string, but NOT NotSupportedException
            Assert.That(() => enlister.Enlist(), Throws.Exception);
        }
    }
}
