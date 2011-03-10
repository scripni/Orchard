﻿using System;
using System.Collections.Generic;
using Autofac;
using NUnit.Framework;
using Orchard.Caching;
using Orchard.Environment.Descriptor;
using Orchard.Environment.Descriptor.Models;
using Orchard.Environment.Extensions;
using Orchard.Environment.Extensions.Loaders;
using Orchard.FileSystems.AppData;
using Orchard.FileSystems.WebSite;
using Orchard.ImportExport.Services;
using Orchard.Recipes.Services;
using Orchard.Services;
using Orchard.Tests.Modules.Recipes.Services;
using Orchard.Tests.Stubs;

namespace Orchard.Tests.Modules.ImportExport.Services {
    [TestFixture]
    public class ImportExportManagerTests {
        private IContainer _container;
        private IImportExportService _importExportService;

        [SetUp]
        public void Init() {
            var builder = new ContainerBuilder();
            builder.RegisterType<ImportExportService>().As<IImportExportService>();
            builder.RegisterType<StubShellDescriptorManager>().As<IShellDescriptorManager>();
            builder.RegisterType<RecipeManager>().As<IRecipeManager>();
            builder.RegisterType<RecipeHarvester>().As<IRecipeHarvester>();
            builder.RegisterType<RecipeStepExecutor>().As<IRecipeStepExecutor>();
            builder.RegisterType<StubStepQueue>().As<IRecipeStepQueue>().InstancePerLifetimeScope();
            builder.RegisterType<StubRecipeJournal>().As<IRecipeJournal>();
            builder.RegisterType<StubRecipeScheduler>().As<IRecipeScheduler>();
            builder.RegisterType<ExtensionManager>().As<IExtensionManager>();
            builder.RegisterType<StubAppDataFolder>().As<IAppDataFolder>();
            builder.RegisterType<StubClock>().As<IClock>();
            builder.RegisterType<StubCacheManager>().As<ICacheManager>();
            builder.RegisterType<Environment.Extensions.ExtensionManagerTests.StubLoaders>().As<IExtensionLoader>();
            builder.RegisterType<RecipeParser>().As<IRecipeParser>();
            builder.RegisterType<StubWebSiteFolder>().As<IWebSiteFolder>();
            builder.RegisterType<CustomRecipeHandler>().As<IRecipeHandler>();

            _container = builder.Build();
            _importExportService = _container.Resolve<IImportExportService>();
        }

        [Test]
        public void ImportSucceedsWhenRecipeContainsImportSteps() {
            Assert.DoesNotThrow(() => _importExportService.Import(
                                                                    @"<Orchard>
                                                                        <Recipe>
                                                                        <Name>MyModuleInstaller</Name>
                                                                        </Recipe>
                                                                        <Settings />
                                                                    </Orchard>"));
        }

        [Test]
        public void ImportFailsWhenRecipeContainsNonImportSteps() {
            Assert.Throws(typeof(InvalidOperationException), () => _importExportService.Import(
                                                                                                @"<Orchard>
                                                                                                  <Recipe>
                                                                                                    <Name>MyModuleInstaller</Name>
                                                                                                  </Recipe>
                                                                                                  <Module name=""MyModule"" />
                                                                                                </Orchard>"));
        }
    }

    public class StubShellDescriptorManager : IShellDescriptorManager {
        public ShellDescriptor GetShellDescriptor() {
            return new ShellDescriptor();
        }

        public void UpdateShellDescriptor(int priorSerialNumber, IEnumerable<ShellFeature> enabledFeatures, IEnumerable<ShellParameter> parameters) {
        }
    }
}