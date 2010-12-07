﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Orchard.Caching;
using Orchard.Environment.Extensions.Compilers;
using Orchard.Environment.Extensions.Models;
using Orchard.FileSystems.Dependencies;
using Orchard.FileSystems.VirtualPath;
using Orchard.Logging;

namespace Orchard.Environment.Extensions.Loaders {
    public class DynamicExtensionLoader : ExtensionLoaderBase {
        private readonly IBuildManager _buildManager;
        private readonly IVirtualPathProvider _virtualPathProvider;
        private readonly IVirtualPathMonitor _virtualPathMonitor;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly IAssemblyProbingFolder _assemblyProbingFolder;
        private readonly IProjectFileParser _projectFileParser;
        private readonly ReloadWorkaround _reloadWorkaround = new ReloadWorkaround();

        public DynamicExtensionLoader(
            IBuildManager buildManager,
            IVirtualPathProvider virtualPathProvider,
            IVirtualPathMonitor virtualPathMonitor,
            IHostEnvironment hostEnvironment,
            IAssemblyProbingFolder assemblyProbingFolder,
            IDependenciesFolder dependenciesFolder,
            IProjectFileParser projectFileParser)
            : base(dependenciesFolder) {

            _buildManager = buildManager;
            _virtualPathProvider = virtualPathProvider;
            _virtualPathMonitor = virtualPathMonitor;
            _hostEnvironment = hostEnvironment;
            _assemblyProbingFolder = assemblyProbingFolder;
            _projectFileParser = projectFileParser;

            Logger = NullLogger.Instance;
        }

        public ILogger Logger { get; set; }

        public override int Order { get { return 100; } }

        public override string GetWebFormAssemblyDirective(DependencyDescriptor dependency) {
            return string.Format("<%@ Assembly Src=\"{0}\"%>", dependency.VirtualPath);
        }

        public override IEnumerable<string> GetWebFormVirtualDependencies(DependencyDescriptor dependency) {
            // Return csproj and all .cs files
            return GetDependencies(dependency.VirtualPath);
        }

        public override IEnumerable<string> GetFileDependencies(DependencyDescriptor dependency, string virtualPath){
            virtualPath = _virtualPathProvider.ToAppRelative(virtualPath);

            if (StringComparer.OrdinalIgnoreCase.Equals(virtualPath, dependency.VirtualPath)) {
                return GetSourceFiles(virtualPath);
            }
            return base.GetFileDependencies(dependency, virtualPath);
        }

        public override void Monitor(ExtensionDescriptor descriptor, Action<IVolatileToken> monitor) {
            // Monitor .csproj and all .cs files
            string projectPath = GetProjectPath(descriptor);
            if (projectPath != null) {
                foreach (var path in GetDependencies(projectPath)) {
                    Logger.Information("Monitoring virtual path \"{0}\"", path);

                    monitor(_virtualPathMonitor.WhenPathChanges(path));
                    _reloadWorkaround.Monitor(_virtualPathMonitor.WhenPathChanges(path));
                }
            }
        }

        public override void ExtensionRemoved(ExtensionLoadingContext ctx, DependencyDescriptor dependency) {
        }

        public override void ExtensionDeactivated(ExtensionLoadingContext ctx, ExtensionDescriptor extension) {
        }

        public override void ExtensionActivated(ExtensionLoadingContext ctx, ExtensionDescriptor extension) {
            if (_reloadWorkaround.AppDomainRestartNeeded) {
                Logger.Information("ExtensionActivated: Module \"{0}\" has changed, forcing AppDomain restart", extension.Id);
                ctx.RestartAppDomain = _reloadWorkaround.AppDomainRestartNeeded;
            }
        }

        public override IEnumerable<ExtensionReferenceProbeEntry> ProbeReferences(ExtensionDescriptor descriptor) {
            string projectPath = GetProjectPath(descriptor);
            if (projectPath == null)
                return Enumerable.Empty<ExtensionReferenceProbeEntry>();

            using(var stream = _virtualPathProvider.OpenFile(projectPath)) {
                var projectFile = _projectFileParser.Parse(stream);

                return projectFile.References.Select(r => new ExtensionReferenceProbeEntry {
                    Descriptor = descriptor,
                    Loader = this,
                    Name = r.SimpleName,
                    VirtualPath = GetReferenceVirtualPath(projectPath, r.SimpleName)
                });
            }
        }

        public override void ReferenceActivated(ExtensionLoadingContext context, ExtensionReferenceProbeEntry referenceEntry) {
            //Note: This is the same implementation as "PrecompiledExtensionLoader"
            if (string.IsNullOrEmpty(referenceEntry.VirtualPath))
                return;

            string sourceFileName = _virtualPathProvider.MapPath(referenceEntry.VirtualPath);

            // Copy the assembly if it doesn't exist or if it is older than the source file.
            bool copyAssembly =
                !_assemblyProbingFolder.AssemblyExists(referenceEntry.Name) ||
                File.GetLastWriteTimeUtc(sourceFileName) > _assemblyProbingFolder.GetAssemblyDateTimeUtc(referenceEntry.Name);

            if (copyAssembly) {
                context.CopyActions.Add(() => _assemblyProbingFolder.StoreAssembly(referenceEntry.Name, sourceFileName));

                // We need to restart the appDomain if the assembly is loaded
                if (_hostEnvironment.IsAssemblyLoaded(referenceEntry.Name)) {
                    Logger.Information("ReferenceActivated: Reference \"{0}\" is activated with newer file and its assembly is loaded, forcing AppDomain restart", referenceEntry.Name);
                    context.RestartAppDomain = true;
                }
            }
        }

        private string GetReferenceVirtualPath(string projectPath, string referenceName) {
            var path = _virtualPathProvider.GetDirectoryName(projectPath);
            path = _virtualPathProvider.Combine(path, "bin", referenceName + ".dll");
            if (_virtualPathProvider.FileExists(path))
                return path;
            return null;
        }

        public override Assembly LoadReference(DependencyReferenceDescriptor reference) {
            // DynamicExtensionLoader has 2 types of references: assemblies from module bin directory
            // and .csproj.
            if (StringComparer.OrdinalIgnoreCase.Equals(Path.GetExtension(reference.VirtualPath), ".dll"))
                return _assemblyProbingFolder.LoadAssembly(reference.Name);

            return _buildManager.GetCompiledAssembly(reference.VirtualPath);
        }

        public override ExtensionProbeEntry Probe(ExtensionDescriptor descriptor) {
            string projectPath = GetProjectPath(descriptor);
            if (projectPath == null)
                return null;

            return new ExtensionProbeEntry {
                Descriptor = descriptor,
                LastWriteTimeUtc = GetDependencies(projectPath).Max(f => _virtualPathProvider.GetFileLastWriteTimeUtc(f)),
                Loader = this,
                VirtualPath = projectPath
            };
        }

        protected override ExtensionEntry LoadWorker(ExtensionDescriptor descriptor) {
            string projectPath = GetProjectPath(descriptor);
            if (projectPath == null)
                return null;

            var assembly = _buildManager.GetCompiledAssembly(projectPath);
            if (assembly == null)
                return null;
            //Logger.Information("Loading extension \"{0}\": assembly name=\"{1}\"", descriptor.Name, assembly.GetName().Name);

            return new ExtensionEntry {
                Descriptor = descriptor,
                Assembly = assembly,
                ExportedTypes = assembly.GetExportedTypes(),
            };
        }

        private IEnumerable<string> GetDependencies(string projectPath) {
            return new[] {projectPath}.Concat(GetSourceFiles(projectPath));
        }

        private IEnumerable<string> GetSourceFiles(string projectPath) {
            var basePath = _virtualPathProvider.GetDirectoryName(projectPath);

            using (var stream = _virtualPathProvider.OpenFile(projectPath)) {
                var projectFile = _projectFileParser.Parse(stream);

                return projectFile.SourceFilenames.Select(f => _virtualPathProvider.Combine(basePath, f));
            }
        }

        private string GetProjectPath(ExtensionDescriptor descriptor) {
            string projectPath = _virtualPathProvider.Combine(descriptor.Location, descriptor.Id,
                                                       descriptor.Id + ".csproj");

            if (!_virtualPathProvider.FileExists(projectPath)) {
                return null;
            }

            return projectPath;
        }

        /// <summary>
        /// We should be able to support reloading multiple version of a compiled module from
        /// a ".csproj" file in the same AppDomain. However, we are currently running into a 
        /// limitation with NHibernate getting confused when a type name is present in
        /// multiple assemblies loaded in an AppDomain.  So, until a solution is found, any change 
        /// to a ".csproj" file of an active module requires an AppDomain restart.
        /// The purpose of this class is to keep track of all .csproj files monitored until
        /// an AppDomain restart.
        /// </summary>
        class ReloadWorkaround {
            private readonly List<IVolatileToken> _tokens = new List<IVolatileToken>();

            public void Monitor(IVolatileToken whenProjectFileChanges) {
                lock(_tokens) {
                    _tokens.Add(whenProjectFileChanges);
                }
            }

            public bool AppDomainRestartNeeded {
                get {
                    lock(_tokens) {
                        return _tokens.Any(t => t.IsCurrent == false);
                    }
                }
            }
        }
    }
}