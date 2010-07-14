﻿using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using NHibernate;
using NHibernate.Cfg;
using Orchard.Data;
using Orchard.Data.Providers;
using Orchard.Environment.Configuration;
using Orchard.Environment.ShellBuilders.Models;
using Orchard.FileSystems.AppData;
using Orchard.Localization;
using Orchard.Logging;

namespace Orchard.Data {
    public interface ISessionFactoryHolder : ISingletonDependency {
        ISessionFactory GetSessionFactory();
        Configuration GetConfiguration();
        SessionFactoryParameters GetSessionFactoryParameters();
    }

    public interface ISessionConfigurationCache {
        void StoreConfig(string shellName, Configuration config);
        void DeleteConfig(string shellName);
        Configuration GetConfiguration(string shellName);
    }

    public class SessionConfigurationCache : ISessionConfigurationCache {
        private readonly IAppDataFolder _appDataFolder;

        public SessionConfigurationCache(IAppDataFolder appDataFolder) {
            _appDataFolder = appDataFolder;
        }

        public void StoreConfig(string shellName, Configuration config) {
            var filename = GetFileName(shellName);

            using ( var stream = File.OpenWrite(filename) ) {
                new BinaryFormatter().Serialize(stream, config);
            }
        }

        public void DeleteConfig(string shellName) {
            var filename = GetFileName(shellName);
            if(File.Exists(filename)) {
                File.Delete(filename);
            }
        }

        public Configuration GetConfiguration(string shellName) {
            var filename = GetFileName(shellName);

            if (!_appDataFolder.FileExists(filename)) {
                return null;
            }
            
            using (var stream = File.OpenRead(filename)) {
                return new BinaryFormatter().Deserialize(stream) as Configuration;
            }
        }

        private string GetFileName(string shellName) {
            return _appDataFolder.MapPath(_appDataFolder.Combine("Sites", shellName, "mappings.bin"));
        }
    }

    public class SessionFactoryHolder : ISessionFactoryHolder {
        private readonly ShellSettings _shellSettings;
        private readonly ShellBlueprint _shellBlueprint;
        private readonly IDataServicesProviderFactory _dataServicesProviderFactory;
        private readonly IAppDataFolder _appDataFolder;
        private readonly ISessionConfigurationCache _sessionConfigurationCache;

        private ISessionFactory _sessionFactory;
        private Configuration _configuration;

        public SessionFactoryHolder(
            ShellSettings shellSettings,
            ShellBlueprint shellBlueprint,
            IDataServicesProviderFactory dataServicesProviderFactory,
            IAppDataFolder appDataFolder,
            ISessionConfigurationCache sessionConfigurationCache) {
            _shellSettings = shellSettings;
            _shellBlueprint = shellBlueprint;
            _dataServicesProviderFactory = dataServicesProviderFactory;
            _appDataFolder = appDataFolder;
            _sessionConfigurationCache = sessionConfigurationCache;

            T = NullLocalizer.Instance;
            Logger = NullLogger.Instance;
        }

        public Localizer T { get; set; }
        public ILogger Logger { get; set; }

        public ISessionFactory GetSessionFactory() {
            lock (this) {
                if (_sessionFactory == null) {
                    _sessionFactory = BuildSessionFactory();
                }
            }
            return _sessionFactory;
        }

        public Configuration GetConfiguration() {
            lock ( this ) {
                if ( _configuration == null ) {
                    _configuration = BuildConfiguration();
                }
            }
            return _configuration;
        }

        private ISessionFactory BuildSessionFactory() {
            Logger.Debug("Building session factory");

            var config = GetConfiguration();
            return config.BuildSessionFactory();
        }

        private Configuration BuildConfiguration() {
            var parameters = GetSessionFactoryParameters();

            var config = _sessionConfigurationCache.GetConfiguration(_shellSettings.Name);

            if ( config == null ) {
                config = _dataServicesProviderFactory
                    .CreateProvider(parameters)
                    .BuildConfiguration(parameters);

                _sessionConfigurationCache.StoreConfig(_shellSettings.Name, config);
            }

            return config;
        }

        public SessionFactoryParameters GetSessionFactoryParameters() {
            var shellPath = _appDataFolder.Combine("Sites", _shellSettings.Name);
            _appDataFolder.CreateDirectory(shellPath);

            var shellFolder = _appDataFolder.MapPath(shellPath);

            return new SessionFactoryParameters {
                Provider = _shellSettings.DataProvider,
                DataFolder = shellFolder,
                ConnectionString = _shellSettings.DataConnectionString,
                RecordDescriptors = _shellBlueprint.Records,
            };
        }
    }


}
