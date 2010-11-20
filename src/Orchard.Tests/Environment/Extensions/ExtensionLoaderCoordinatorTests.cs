using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autofac;
using NUnit.Framework;
using Orchard.Caching;
using Orchard.Environment.Extensions;
using Orchard.Environment.Extensions.Folders;
using Orchard.Environment.Extensions.Loaders;
using Orchard.Environment.Extensions.Models;
using Orchard.FileSystems.Dependencies;
using Orchard.Tests.Extensions.ExtensionTypes;
using Orchard.Tests.Stubs;

namespace Orchard.Tests.Environment.Extensions {
    [TestFixture]
    public class ExtensionLoaderCoordinatorTests {
        private IContainer _container;
        private IExtensionManager _manager;
        private StubFolders _folders;

        [SetUp]
        public void Init() {
            var builder = new ContainerBuilder();
            _folders = new StubFolders("Module");
            builder.RegisterInstance(_folders).As<IExtensionFolders>();
            builder.RegisterType<ExtensionManager>().As<IExtensionManager>();
            builder.RegisterType<StubCacheManager>().As<ICacheManager>();

            _container = builder.Build();
            _manager = _container.Resolve<IExtensionManager>();
        }

        public class StubFolders : IExtensionFolders {
            private readonly string _extensionType;

            public StubFolders(string extensionType) {
                _extensionType = extensionType;
                Manifests = new Dictionary<string, string>();
            }

            public IDictionary<string, string> Manifests { get; set; }

            public IEnumerable<ExtensionDescriptor> AvailableExtensions() {
                foreach (var e in Manifests) {
                    string name = e.Key;
                    yield return ExtensionFolders.GetDescriptorForExtension("~/", name, _extensionType, Manifests[name]);
                }
            }
        }

        public class StubLoaders : IExtensionLoader {
            #region Implementation of IExtensionLoader

            public int Order {
                get { return 1; }
            }

            public string Name {
                get { return this.GetType().Name; }
            }

            public Assembly LoadReference(DependencyReferenceDescriptor reference) {
                throw new NotImplementedException();
            }

            public void ReferenceActivated(ExtensionLoadingContext context, ExtensionReferenceProbeEntry referenceEntry) {
                throw new NotImplementedException();
            }

            public void ReferenceDeactivated(ExtensionLoadingContext context, ExtensionReferenceProbeEntry referenceEntry) {
                throw new NotImplementedException();
            }

            public bool IsCompatibleWithModuleReferences(ExtensionDescriptor extension, IEnumerable<ExtensionProbeEntry> references) {
                throw new NotImplementedException();
            }

            public ExtensionProbeEntry Probe(ExtensionDescriptor descriptor) {
                return new ExtensionProbeEntry { Descriptor = descriptor, Loader = this };
            }

            public IEnumerable<ExtensionReferenceProbeEntry> ProbeReferences(ExtensionDescriptor extensionDescriptor) {
                throw new NotImplementedException();
            }

            public ExtensionEntry Load(ExtensionDescriptor descriptor) {
                return new ExtensionEntry { Descriptor = descriptor, ExportedTypes = new[] { typeof(Alpha), typeof(Beta), typeof(Phi) } };
            }

            public void ExtensionActivated(ExtensionLoadingContext ctx, ExtensionDescriptor extension) {
                throw new NotImplementedException();
            }

            public void ExtensionDeactivated(ExtensionLoadingContext ctx, ExtensionDescriptor extension) {
                throw new NotImplementedException();
            }

            public void ExtensionRemoved(ExtensionLoadingContext ctx, DependencyDescriptor dependency) {
                throw new NotImplementedException();
            }

            public void Monitor(ExtensionDescriptor extension, Action<IVolatileToken> monitor) {
                throw new NotImplementedException();
            }

            public string GetWebFormAssemblyDirective(DependencyDescriptor dependency) {
                throw new NotImplementedException();
            }

            public IEnumerable<string> GetWebFormVirtualDependencies(DependencyDescriptor dependency) {
                throw new NotImplementedException();
            }

            public IEnumerable<string> GetFileDependencies(DependencyDescriptor dependency, string virtualPath) {
                throw new NotImplementedException();
            }

            #endregion
        }


        [Test]
        public void AvailableExtensionsShouldFollowCatalogLocations() {
            _folders.Manifests.Add("foo", "Name: Foo");
            _folders.Manifests.Add("bar", "Name: Bar");
            _folders.Manifests.Add("frap", "Name: Frap");
            _folders.Manifests.Add("quad", "Name: Quad");

            var available = _manager.AvailableExtensions();

            Assert.That(available.Count(), Is.EqualTo(4));
            Assert.That(available, Has.Some.Property("Name").EqualTo("foo"));
        }

        [Test]
        public void ExtensionDescriptorsShouldHaveNameAndVersion() {

            _folders.Manifests.Add("Sample", @"
Name: Sample Extension
Version: 2.x
");

            var descriptor = _manager.AvailableExtensions().Single();
            Assert.That(descriptor.Name, Is.EqualTo("Sample"));
            Assert.That(descriptor.DisplayName, Is.EqualTo("Sample Extension"));
            Assert.That(descriptor.Version, Is.EqualTo("2.x"));
        }

        [Test]
        public void ExtensionDescriptorsShouldBeParsedForMinimalModuleTxt() {

            _folders.Manifests.Add("SuperWiki", @"
Name: SuperWiki
Version: 1.0.3
OrchardVersion: 1
Features:
    SuperWiki: 
        Description: My super wiki module for Orchard.
");

            var descriptor = _manager.AvailableExtensions().Single();
            Assert.That(descriptor.Name, Is.EqualTo("SuperWiki"));
            Assert.That(descriptor.Version, Is.EqualTo("1.0.3"));
            Assert.That(descriptor.OrchardVersion, Is.EqualTo("1"));
            Assert.That(descriptor.Features.Count(), Is.EqualTo(1));
            Assert.That(descriptor.Features.First().Name, Is.EqualTo("SuperWiki"));
            Assert.That(descriptor.Features.First().Extension.Name, Is.EqualTo("SuperWiki"));
            Assert.That(descriptor.Features.First().Description, Is.EqualTo("My super wiki module for Orchard."));
        }

        [Test]
        public void ExtensionDescriptorsShouldBeParsedForCompleteModuleTxt() {

            _folders.Manifests.Add("MyCompany.AnotherWiki", @"
Name: AnotherWiki
Author: Coder Notaprogrammer
Website: http://anotherwiki.codeplex.com
Version: 1.2.3
OrchardVersion: 1
Features:
    AnotherWiki: 
        Description: My super wiki module for Orchard.
        Dependencies: Versioning, Search
        Category: Content types
    AnotherWiki Editor:
        Description: A rich editor for wiki contents.
        Dependencies: TinyMCE, AnotherWiki
        Category: Input methods
    AnotherWiki DistributionList:
        Description: Sends e-mail alerts when wiki contents gets published.
        Dependencies: AnotherWiki, Email Subscriptions
        Category: Email
    AnotherWiki Captcha:
        Description: Kills spam. Or makes it zombie-like.
        Dependencies: AnotherWiki, reCaptcha
        Category: Spam
");

            var descriptor = _manager.AvailableExtensions().Single();
            Assert.That(descriptor.Name, Is.EqualTo("MyCompany.AnotherWiki"));
            Assert.That(descriptor.DisplayName, Is.EqualTo("AnotherWiki"));
            Assert.That(descriptor.Author, Is.EqualTo("Coder Notaprogrammer"));
            Assert.That(descriptor.WebSite, Is.EqualTo("http://anotherwiki.codeplex.com"));
            Assert.That(descriptor.Version, Is.EqualTo("1.2.3"));
            Assert.That(descriptor.OrchardVersion, Is.EqualTo("1"));
            Assert.That(descriptor.Features.Count(), Is.EqualTo(5));
            foreach (var featureDescriptor in descriptor.Features) {
                switch (featureDescriptor.Name) {
                    case "AnotherWiki":
                        Assert.That(featureDescriptor.Extension, Is.SameAs(descriptor));
                        Assert.That(featureDescriptor.Description, Is.EqualTo("My super wiki module for Orchard."));
                        Assert.That(featureDescriptor.Category, Is.EqualTo("Content types"));
                        Assert.That(featureDescriptor.Dependencies.Count(), Is.EqualTo(2));
                        Assert.That(featureDescriptor.Dependencies.Contains("Versioning"));
                        Assert.That(featureDescriptor.Dependencies.Contains("Search"));
                        break;
                    case "AnotherWiki Editor":
                        Assert.That(featureDescriptor.Extension, Is.SameAs(descriptor));
                        Assert.That(featureDescriptor.Description, Is.EqualTo("A rich editor for wiki contents."));
                        Assert.That(featureDescriptor.Category, Is.EqualTo("Input methods"));
                        Assert.That(featureDescriptor.Dependencies.Count(), Is.EqualTo(2));
                        Assert.That(featureDescriptor.Dependencies.Contains("TinyMCE"));
                        Assert.That(featureDescriptor.Dependencies.Contains("AnotherWiki"));
                        break;
                    case "AnotherWiki DistributionList":
                        Assert.That(featureDescriptor.Extension, Is.SameAs(descriptor));
                        Assert.That(featureDescriptor.Description, Is.EqualTo("Sends e-mail alerts when wiki contents gets published."));
                        Assert.That(featureDescriptor.Category, Is.EqualTo("Email"));
                        Assert.That(featureDescriptor.Dependencies.Count(), Is.EqualTo(2));
                        Assert.That(featureDescriptor.Dependencies.Contains("AnotherWiki"));
                        Assert.That(featureDescriptor.Dependencies.Contains("Email Subscriptions"));
                        break;
                    case "AnotherWiki Captcha":
                        Assert.That(featureDescriptor.Extension, Is.SameAs(descriptor));
                        Assert.That(featureDescriptor.Description, Is.EqualTo("Kills spam. Or makes it zombie-like."));
                        Assert.That(featureDescriptor.Category, Is.EqualTo("Spam"));
                        Assert.That(featureDescriptor.Dependencies.Count(), Is.EqualTo(2));
                        Assert.That(featureDescriptor.Dependencies.Contains("AnotherWiki"));
                        Assert.That(featureDescriptor.Dependencies.Contains("reCaptcha"));
                        break;
                    // default feature.
                    case "MyCompany.AnotherWiki":
                        Assert.That(featureDescriptor.Extension, Is.SameAs(descriptor));
                        break;
                    default:
                        Assert.Fail("Features not parsed correctly");
                        break;
                }
            }
        }

        [Test]
        public void ExtensionManagerShouldLoadFeatures() {
            var extensionLoader = new StubLoaders();
            var extensionFolder = new StubFolders("Module");

            extensionFolder.Manifests.Add("TestModule", @"
Name: TestModule
Version: 1.0.3
OrchardVersion: 1
Features:
    TestModule: 
        Description: My test module for Orchard.
    TestFeature:
        Description: Contains the Phi type.
");

            IExtensionManager extensionManager = new ExtensionManager(new[] { extensionFolder }, new[] { extensionLoader }, new StubCacheManager());
            var testFeature = extensionManager.AvailableExtensions()
                .SelectMany(x => x.Features);

            var features = extensionManager.LoadFeatures(testFeature);
            var types = features.SelectMany(x => x.ExportedTypes);

            Assert.That(types.Count(), Is.Not.EqualTo(0));
        }

        [Test]
        public void ExtensionManagerFeaturesContainNonAbstractClasses() {
            var extensionLoader = new StubLoaders();
            var extensionFolder = new StubFolders("Module");

            extensionFolder.Manifests.Add("TestModule", @"
Name: TestModule
Version: 1.0.3
OrchardVersion: 1
Features:
    TestModule: 
        Description: My test module for Orchard.
    TestFeature:
        Description: Contains the Phi type.
");

            IExtensionManager extensionManager = new ExtensionManager(new[] { extensionFolder }, new[] { extensionLoader }, new StubCacheManager());
            var testFeature = extensionManager.AvailableExtensions()
                .SelectMany(x => x.Features);

            var features = extensionManager.LoadFeatures(testFeature);
            var types = features.SelectMany(x => x.ExportedTypes);

            foreach (var type in types) {
                Assert.That(type.IsClass);
                Assert.That(!type.IsAbstract);
            }
        }

        [Test, Ignore("This assertion appears to be inconsistent with the comment in extension manager - an empty feature is returned")]
        public void ExtensionManagerShouldThrowIfFeatureDoesNotExist() {
            var featureDescriptor = new FeatureDescriptor { Name = "NoSuchFeature" };
            Assert.Throws<ArgumentException>(() => _manager.LoadFeatures(new[] { featureDescriptor }));
        }

        [Test]
        public void ExtensionManagerTestFeatureAttribute() {
            var extensionLoader = new StubLoaders();
            var extensionFolder = new StubFolders("Module");

            extensionFolder.Manifests.Add("TestModule", @"
Name: TestModule
Version: 1.0.3
OrchardVersion: 1
Features:
    TestModule: 
        Description: My test module for Orchard.
    TestFeature:
        Description: Contains the Phi type.
");

            IExtensionManager extensionManager = new ExtensionManager(new[] { extensionFolder }, new[] { extensionLoader }, new StubCacheManager());
            var testFeature = extensionManager.AvailableExtensions()
                .SelectMany(x => x.Features)
                .Single(x => x.Name == "TestFeature");

            foreach (var feature in extensionManager.LoadFeatures(new[] { testFeature })) {
                foreach (var type in feature.ExportedTypes) {
                    foreach (OrchardFeatureAttribute featureAttribute in type.GetCustomAttributes(typeof(OrchardFeatureAttribute), false)) {
                        Assert.That(featureAttribute.FeatureName, Is.EqualTo("TestFeature"));
                    }
                }
            }
        }

        [Test]
        public void ExtensionManagerLoadFeatureReturnsTypesFromSpecificFeaturesWithFeatureAttribute() {
            var extensionLoader = new StubLoaders();
            var extensionFolder = new StubFolders("Module");

            extensionFolder.Manifests.Add("TestModule", @"
Name: TestModule
Version: 1.0.3
OrchardVersion: 1
Features:
    TestModule: 
        Description: My test module for Orchard.
    TestFeature:
        Description: Contains the Phi type.
");

            IExtensionManager extensionManager = new ExtensionManager(new[] { extensionFolder }, new[] { extensionLoader }, new StubCacheManager());
            var testFeature = extensionManager.AvailableExtensions()
                .SelectMany(x => x.Features)
                .Single(x => x.Name == "TestFeature");

            foreach (var feature in extensionManager.LoadFeatures(new[] { testFeature })) {
                foreach (var type in feature.ExportedTypes) {
                    Assert.That(type == typeof(Phi));
                }
            }
        }

        [Test]
        public void ExtensionManagerLoadFeatureDoesNotReturnTypesFromNonMatchingFeatures() {
            var extensionLoader = new StubLoaders();
            var extensionFolder = new StubFolders("Module");

            extensionFolder.Manifests.Add("TestModule", @"
Name: TestModule
Version: 1.0.3
OrchardVersion: 1
Features:
    TestModule: 
        Description: My test module for Orchard.
    TestFeature:
        Description: Contains the Phi type.
");

            IExtensionManager extensionManager = new ExtensionManager(new[] { extensionFolder }, new[] { extensionLoader }, new StubCacheManager());
            var testModule = extensionManager.AvailableExtensions()
                .SelectMany(x => x.Features)
                .Single(x => x.Name == "TestModule");

            foreach (var feature in extensionManager.LoadFeatures(new[] { testModule })) {
                foreach (var type in feature.ExportedTypes) {
                    Assert.That(type != typeof(Phi));
                    Assert.That((type == typeof(Alpha) || (type == typeof(Beta))));
                }
            }
        }

        [Test]
        public void ModuleNameIsIntroducedAsFeatureImplicitly() {
            var extensionLoader = new StubLoaders();
            var extensionFolder = new StubFolders("Module");

            extensionFolder.Manifests.Add("Minimalistic", @"
Name: Minimalistic
Version: 1.0.3
OrchardVersion: 1
");

            IExtensionManager extensionManager = new ExtensionManager(new[] { extensionFolder }, new[] { extensionLoader }, new StubCacheManager());
            var minimalisticModule = extensionManager.AvailableExtensions().Single(x => x.Name == "Minimalistic");

            Assert.That(minimalisticModule.Features.Count(), Is.EqualTo(1));
            Assert.That(minimalisticModule.Features.Single().Name, Is.EqualTo("Minimalistic"));
        }


        [Test]
        public void ThemeNameIsIntroducedAsFeatureImplicitly() {
            var extensionLoader = new StubLoaders();
            var extensionFolder = new StubFolders("Theme");

            extensionFolder.Manifests.Add("Minimalistic", @"
Name: Minimalistic
Version: 1.0.3
OrchardVersion: 1
");

            IExtensionManager extensionManager = new ExtensionManager(new[] { extensionFolder }, new[] { extensionLoader }, new StubCacheManager());
            var minimalisticModule = extensionManager.AvailableExtensions().Single(x => x.Name == "Minimalistic");

            Assert.That(minimalisticModule.Features.Count(), Is.EqualTo(1));
            Assert.That(minimalisticModule.Features.Single().Name, Is.EqualTo("Minimalistic"));
        }
    }
}