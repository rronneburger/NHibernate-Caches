using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using NHibernate.Cache;

namespace NHibernate.Caches.SysCache2
{
	/// <summary>
	/// Cache provider using the System.Runtime.Caching.MemoryCache classes.
	/// </summary>
	public class SysCacheProvider : ICacheProvider
	{
		/// <summary>Pre-configured cache region settings.</summary>
		private static readonly ConcurrentDictionary<string, Lazy<CacheBase>> CacheRegions =
			new ConcurrentDictionary<string, Lazy<CacheBase>>();

		/// <summary>List of pre configured already built cache regions.</summary>
		private static Dictionary<string, CacheRegionElement> _cacheRegionSettings;

		private static Dictionary<string, CacheRegionElement> CacheRegionSettings
		{
			get
			{
				if (_cacheRegionSettings == null)
				{
					lock (CacheRegions)
					{
						if (_cacheRegionSettings == null)
						{
							_cacheRegionSettings = InitCacheRegionSettings();
						}
					}
				}
				return _cacheRegionSettings;
			}
		}

		/// <summary>Log4net logger.</summary>
		private static readonly INHibernateLogger Log = NHibernateLogger.For(typeof(SysCacheProvider));

		/// <summary>
		/// Set a region configuration.
		/// </summary>
		/// <param name="configuration">The region configuration.</param>
		public static void SetRegionConfiguration(CacheRegionElement configuration)
		{
			CacheRegionSettings[configuration.Name] = configuration;
		}

		private static Dictionary<string, CacheRegionElement> InitCacheRegionSettings()
		{
			var configSection = GetConfigSection();
			Dictionary<string, CacheRegionElement> settings;

			if (configSection != null && configSection.CacheRegions.Count > 0)
			{
				settings = new Dictionary<string, CacheRegionElement>(configSection.CacheRegions.Count);
				foreach (var cacheRegion in configSection.CacheRegions)
				{
					if (cacheRegion is CacheRegionElement element)
					{
						settings.Add(element.Name, element);
					}
				}
			}
			else
			{
				settings = new Dictionary<string, CacheRegionElement>(0);
				Log.Info(
					"No cache regions specified. Cache regions can be specified in sysCache configuration section with custom settings.");
			}
			return settings;
		}

		#region ICacheProvider Members

		/// <inheritdoc />
#pragma warning disable 618
		public ICache BuildCache(string regionName, IDictionary<string, string> properties)
#pragma warning restore 618
		{
			// Return a configured cache region if we have one for the region already.
			// This may happen if there is a query cache specified for a region that is configured,
			// since query caches are not configured at session factory startup. This may also happen
			// if many session factories are built.
			// This cache avoids to duplicate the configured SQL dependencies registration in above cases.
			if (!string.IsNullOrEmpty(regionName)
				// We do not cache non-configured caches, so must first look-up settings for knowing if it
				// is a configured one.
				&& CacheRegionSettings.TryGetValue(regionName, out var regionSettings))
			{
				// The Lazy<T> is required for ensuring the cache is built only once. ConcurrentDictionary
				// may run concurrently the value factory for the same key, but it will yield only one
				// of the resulting Lazy<T>. The lazy will then actually build the cache when accessing its
				// value after having obtained it, and it will not do that concurrently.
				// https://stackoverflow.com/a/31637510/1178314
				var cache = CacheRegions.GetOrAdd(regionName,
					r => new Lazy<CacheBase>(() => BuildCache(r, properties, regionSettings)));
				return cache.Value;
			}

			// We will end up creating cache regions here for cache regions that NHibernate
			// uses internally and cache regions that weren't specified in the application config file
			return BuildCache(regionName, properties, null);
		}

		private CacheBase BuildCache(string regionName, IDictionary<string, string> properties, CacheRegionElement settings)
		{
			Log.Debug(
				settings != null
					? "building cache region, '{0}', from configuration"
					: "building non-configured cache region : {0}", regionName);
			return new SysCacheRegion(regionName, settings, properties);
		}

		/// <inheritdoc />
		public long NextTimestamp()
		{
			return Timestamper.Next();
		}

		/// <inheritdoc />
		public void Start(IDictionary<string, string> properties)
		{
		}

		/// <inheritdoc />
		public void Stop()
		{
		}

		#endregion

		private static SysCacheSection GetConfigSection()
		{
			var section = SysCacheSection.GetSection();

			if (section == null)
			{
				// In .NET Core/8.0, ConfigurationManager might not automatically find the config file
				// when running from a test runner. We try to find it in the loaded assemblies.
				foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
				{
					if (assembly.IsDynamic || string.IsNullOrEmpty(assembly.Location))
						continue;

					var assemblyName = assembly.GetName().Name;
					if (assemblyName.StartsWith("NHibernate.Caches.") && assemblyName.EndsWith(".Tests"))
					{
						var config = ConfigurationManager.OpenExeConfiguration(assembly.Location);
						if (config.HasFile)
						{
							section = config.GetSection("syscache2") as SysCacheSection;
							if (section != null)
								break;
						}
					}
				}
			}

			if (section == null)
			{
				var config = ConfigurationManager.OpenExeConfiguration(typeof(SysCacheProvider).Assembly.Location);
				if (config.HasFile)
				{
					section = config.GetSection("syscache2") as SysCacheSection;
				}
			}

			return section;
		}
	}
}
