﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Autofac;
using Autofac.Builder;
using Autofac.Integration.Web;
using Autofac.Modules;
using NUnit.Framework;
using Orchard.Environment;
using Orchard.Mvc;
using Orchard.Mvc.ModelBinders;
using Orchard.Mvc.Routes;
using Orchard.Packages;
using Orchard.Tests.Mvc.Routes;
using Orchard.Tests.Stubs;

namespace Orchard.Tests.Environment {
    [TestFixture]
    public class DefaultOrchardHostTests {
        private IContainer _container;
        private RouteCollection _routeCollection;
        private ModelBinderDictionary _modelBinderDictionary;
        private ControllerBuilder _controllerBuilder;

        [SetUp]
        public void Init() {
            _controllerBuilder = new ControllerBuilder();
            _routeCollection = new RouteCollection();
            _modelBinderDictionary = new ModelBinderDictionary();

            _container = OrchardStarter.CreateHostContainer(
                builder => {
                    builder.RegisterModule(new ImplicitCollectionSupportModule());
                    builder.Register<StubContainerProvider>().As<IContainerProvider>().ContainerScoped();
                    builder.Register<StubCompositionStrategy>().As<ICompositionStrategy>().ContainerScoped();
                    builder.Register<DefaultOrchardHost>().As<IOrchardHost>();
                    builder.Register<RoutePublisher>().As<IRoutePublisher>();
                    builder.Register<ModelBinderPublisher>().As<IModelBinderPublisher>();
                    builder.Register(_controllerBuilder);
                    builder.Register(_routeCollection);
                    builder.Register(_modelBinderDictionary);
                    builder.Register(new ViewEngineCollection{new WebFormViewEngine()});
                    builder.Register(new StuPackageManager()).As<IPackageManager>();
                });
        }

        public class StuPackageManager : IPackageManager {
            public IEnumerable<PackageDescriptor> AvailablePackages() {
                return Enumerable.Empty<PackageDescriptor>();
            }

            public IEnumerable<PackageEntry> ActivePackages() {
                return Enumerable.Empty<PackageEntry>();
            }
        }

        [Test]
        public void HostShouldSetControllerFactory() {
            var host = _container.Resolve<IOrchardHost>();

            Assert.That(_controllerBuilder.GetControllerFactory(), Is.TypeOf<DefaultControllerFactory>());
            host.Initialize();
            Assert.That(_controllerBuilder.GetControllerFactory(), Is.TypeOf<OrchardControllerFactory>());
        }

        public class StubCompositionStrategy : ICompositionStrategy {
            public IEnumerable<Assembly> GetAssemblies() {
                return Enumerable.Empty<Assembly>();
            }

            public IEnumerable<Type> GetModuleTypes() {
                return Enumerable.Empty<Type>();
            }

            public IEnumerable<Type> GetDependencyTypes() {
                return Enumerable.Empty<Type>();
            }

            public IEnumerable<Type> GetRecordTypes() {
                return Enumerable.Empty<Type>();
            }
        }

        [Test]
        public void DifferentShellInstanceShouldBeReturnedAfterEachCreate() {
            var host = _container.Resolve<IOrchardHost>();
            var runtime1 = host.CreateShell();
            var runtime2 = host.CreateShell();
            Assert.That(runtime1, Is.Not.SameAs(runtime2));
        }
    }
}