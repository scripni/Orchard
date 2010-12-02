﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autofac;
using Moq;
using NUnit.Framework;
using Orchard.DisplayManagement.Descriptors;
using Orchard.DisplayManagement.Descriptors.ShapeTemplateStrategy;
using Orchard.Environment.Descriptor.Models;
using Orchard.Environment.Extensions;
using Orchard.Environment.Extensions.Models;
using Orchard.FileSystems.VirtualPath;

namespace Orchard.Tests.DisplayManagement.Descriptors {
    [TestFixture]
    public class ShapeTemplateBindingStrategyTests : ContainerTestBase {
        private ShellDescriptor _descriptor;
        private IList<FeatureDescriptor> _features;
        private TestViewEngine _testViewEngine;
        private TestVirtualPathProvider _testVirtualPathProvider;


        protected override void Register(Autofac.ContainerBuilder builder) {
            _descriptor = new ShellDescriptor { };
            _testViewEngine = new TestViewEngine();
            _testVirtualPathProvider = new TestVirtualPathProvider();

            builder.Register(ctx => _descriptor);
            builder.RegisterType<ShapeTemplateBindingStrategy>().As<IShapeTableProvider>();
            builder.RegisterType<BasicShapeTemplateHarvester>().As<IShapeTemplateHarvester>();
            builder.RegisterInstance(_testViewEngine).As<IShapeTemplateViewEngine>();
            builder.RegisterInstance(_testVirtualPathProvider).As<IVirtualPathProvider>();

            var extensionManager = new Mock<IExtensionManager>();
            builder.Register(ctx => extensionManager);
            builder.Register(ctx => extensionManager.Object);
        }

        public class TestViewEngine : Dictionary<string, object>, IShapeTemplateViewEngine {
            public IEnumerable<string> DetectTemplateFileNames(IEnumerable<string> fileNames) {
                return fileNames;
            }
        }

        public class TestVirtualPathProvider : IVirtualPathProvider {
            public string Combine(params string[] paths) {
                throw new NotImplementedException();
            }

            public string ToAppRelative(string virtualPath) {
                throw new NotImplementedException();
            }

            public string MapPath(string virtualPath) {
                throw new NotImplementedException();
            }

            public bool FileExists(string virtualPath) {
                throw new NotImplementedException();
            }

            public Stream OpenFile(string virtualPath) {
                throw new NotImplementedException();
            }

            public StreamWriter CreateText(string virtualPath) {
                throw new NotImplementedException();
            }

            public Stream CreateFile(string virtualPath) {
                throw new NotImplementedException();
            }

            public DateTime GetFileLastWriteTimeUtc(string virtualPath) {
                throw new NotImplementedException();
            }

            public bool DirectoryExists(string virtualPath) {
                throw new NotImplementedException();
            }

            public void CreateDirectory(string virtualPath) {
                throw new NotImplementedException();
            }

            public string GetDirectoryName(string virtualPath) {
                throw new NotImplementedException();
            }

            public IEnumerable<string> ListFiles(string path) {
                return new List<string> {"~/Modules/Alpha/Views/AlphaShape.blah"};
            }

            public IEnumerable<string> ListDirectories(string path) {
                throw new NotImplementedException();
            }
        }

        protected override void Resolve(ILifetimeScope container) {
            _features = new List<FeatureDescriptor>();

            container.Resolve<Mock<IExtensionManager>>()
                .Setup(em => em.AvailableFeatures())
                .Returns(_features);
        }

        void AddFeature(string name, params string[] dependencies) {
            var featureDescriptor = new FeatureDescriptor {
                Id = name,
                Dependencies = dependencies,
                Extension = new ExtensionDescriptor {
                    Id = name,
                    Location = "~/Modules"
                }
            };
            featureDescriptor.Extension.Features = new[] { featureDescriptor };

            _features.Add(featureDescriptor);
        }

        void AddEnabledFeature(string name, params string[] dependencies) {
            AddFeature(name, dependencies);
            _descriptor.Features = _descriptor.Features.Concat(new[] { new ShellFeature { Name = name } });
        }

        [Test]
        public void TemplateResolutionWorks() {
            AddEnabledFeature("Alpha");

            _testViewEngine.Add("~/Modules/Alpha/Views/AlphaShape.blah", null);
            var strategy = _container.Resolve<IShapeTableProvider>();

            IList<ShapeAlterationBuilder> alterationBuilders = new List<ShapeAlterationBuilder>();
            var builder = new ShapeTableBuilder(alterationBuilders,null);
            strategy.Discover(builder);
            var alterations = alterationBuilders.Select(alterationBuilder=>alterationBuilder.Build());

            Assert.That(alterations.Any(alteration => alteration.ShapeType == "AlphaShape"));
        }

    }
}
