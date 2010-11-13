using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using NHibernate.Cfg;
using Orchard.Environment.Configuration;
using Orchard.Environment.ShellBuilders.Models;
using Orchard.FileSystems.AppData;
using Orchard.Logging;
using Orchard.Utility;

namespace Orchard.Data {
    public class SessionConfigurationCache : ISessionConfigurationCache {
        private readonly ShellSettings _shellSettings;
        private readonly ShellBlueprint _shellBlueprint;
        private readonly IAppDataFolder _appDataFolder;

        public SessionConfigurationCache(ShellSettings shellSettings, ShellBlueprint shellBlueprint, IAppDataFolder appDataFolder) {
            _shellSettings = shellSettings;
            _shellBlueprint = shellBlueprint;
            _appDataFolder = appDataFolder;

            Logger = NullLogger.Instance;
        }

        public ILogger Logger { get; set; }

        public Configuration GetConfiguration(Func<Configuration> builder) {
            var hash = ComputeHash().Value;

            // Return previous configuration if it exsists and has the same hash as
            // the current blueprint.
            var previousConfig = ReadConfiguration(hash);
            if (previousConfig != null) {
                return previousConfig.Configuration;
            }

            // Create cache and persist it
            var cache = new ConfigurationCache {
                Hash = hash,
                Configuration = builder()
            };

            StoreConfiguration(cache);
            return cache.Configuration;
        }

        private class ConfigurationCache {
            public string Hash { get; set; }
            public Configuration Configuration { get; set; }
        }

        private void StoreConfiguration(ConfigurationCache cache) {
            var pathName = GetPathName(_shellSettings.Name);

            try {
                var formatter = new BinaryFormatter();
                using (var stream = _appDataFolder.CreateFile(pathName)) {
                    formatter.Serialize(stream, cache.Hash);
                    formatter.Serialize(stream, cache.Configuration);
                }
            }
            catch (Exception e) {
                //Note: This can happen when multiple processes/AppDomains try to save
                //      the cached configuration at the same time. Only one concurrent
                //      writer will win, and it's harmless for the other ones to fail.
                for (var scan = e; scan != null; scan = scan.InnerException)
                    Logger.Warning("Error storing new NHibernate cache configuration: {0}", scan.Message);
            }
        }

        private ConfigurationCache ReadConfiguration(string hash) {
            var pathName = GetPathName(_shellSettings.Name);

            if (!_appDataFolder.FileExists(pathName))
                return null;

            try {
                var formatter = new BinaryFormatter();
                using (var stream = _appDataFolder.OpenFile(pathName)) {

                    var oldHash = (string)formatter.Deserialize(stream);
                    if (hash != oldHash) {
                        Logger.Information("The cached NHibernate configuration is out of date. A new one will be re-generated.");
                        return null;
                    }

                    var oldConfig = (Configuration)formatter.Deserialize(stream);

                    return new ConfigurationCache {
                        Hash = oldHash,
                        Configuration = oldConfig
                    };
                }
            }
            catch (Exception e) {
                for (var scan = e; scan != null; scan = scan.InnerException)
                    Logger.Warning("Error reading the cached NHibernate configuration: {0}", scan.Message);
                Logger.Information("A new one will be re-generated.");
                return null;
            }
        }

        private Hash ComputeHash() {
            var hash = new Hash();

            hash.AddString(_shellSettings.DataProvider);
            hash.AddString(_shellSettings.DataTablePrefix);
            hash.AddString(_shellSettings.DataConnectionString);
            hash.AddString(_shellSettings.Name);

            // We need to hash the assemnly names, record names and property names
            foreach (var tableName in _shellBlueprint.Records.Select(x => x.TableName)) {
                hash.AddString(tableName);
            }

            foreach (var recordType in _shellBlueprint.Records.Select(x => x.Type)) {
                hash.AddTypeReference(recordType);

                if (recordType.BaseType != null)
                    hash.AddTypeReference(recordType.BaseType);

                foreach (var property in recordType.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public)) {
                    hash.AddString(property.Name);
                    hash.AddTypeReference(property.PropertyType);

                    foreach (var attr in property.GetCustomAttributesData()) {
                        hash.AddTypeReference(attr.Constructor.DeclaringType);
                    }
                }
            }

            return hash;
        }

        private string GetPathName(string shellName) {
            return _appDataFolder.Combine("Sites", shellName, "mappings.bin");
        }
    }
}
