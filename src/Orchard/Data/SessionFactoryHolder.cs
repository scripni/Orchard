﻿using System;
using System.IO;
using System.Xml.Serialization;
using NHibernate;
using NHibernate.Cfg;
using Orchard.Data;
using Orchard.Data.Providers;
using Orchard.Environment;
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

    public class SessionFactoryHolder : ISessionFactoryHolder {
        private readonly ShellSettings _shellSettings;
        private readonly ShellBlueprint _shellBlueprint;
        private readonly IHostEnvironment _hostEnvironment;
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
            ISessionConfigurationCache sessionConfigurationCache,
            IHostEnvironment hostEnvironment) {
            _shellSettings = shellSettings;
            _shellBlueprint = shellBlueprint;
            _dataServicesProviderFactory = dataServicesProviderFactory;
            _appDataFolder = appDataFolder;
            _sessionConfigurationCache = sessionConfigurationCache;
            _hostEnvironment = hostEnvironment;

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
            lock (this) {
                if (_configuration == null) {
                    _configuration = BuildConfiguration();
                }
            }
            return _configuration;
        }

        private ISessionFactory BuildSessionFactory() {
            Logger.Debug("Building session factory");

            if (!_hostEnvironment.IsFullTrust)
                NHibernate.Cfg.Environment.UseReflectionOptimizer = false;

            Configuration config = GetConfiguration();
            return config.BuildSessionFactory();
        }

        private Configuration BuildConfiguration() {
            var parameters = GetSessionFactoryParameters();

            var config = _sessionConfigurationCache.GetConfiguration(() =>
                _dataServicesProviderFactory
                    .CreateProvider(parameters)
                    .BuildConfiguration(parameters));

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
