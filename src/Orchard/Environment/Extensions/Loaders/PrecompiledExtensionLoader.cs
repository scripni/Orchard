using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Orchard.Caching;
using Orchard.Environment.Extensions.Models;
using Orchard.FileSystems.Dependencies;
using Orchard.FileSystems.VirtualPath;
using Orchard.Logging;

namespace Orchard.Environment.Extensions.Loaders {
    /// <summary>
    /// Load an extension by looking into the "bin" subdirectory of an
    /// extension directory.
    /// </summary>
    public class PrecompiledExtensionLoader : ExtensionLoaderBase {
        private readonly IHostEnvironment _hostEnvironment;
        private readonly IAssemblyProbingFolder _assemblyProbingFolder;
        private readonly IVirtualPathProvider _virtualPathProvider;
        private readonly IVirtualPathMonitor _virtualPathMonitor;

        public PrecompiledExtensionLoader(
            IHostEnvironment hostEnvironment,
            IDependenciesFolder dependenciesFolder,
            IAssemblyProbingFolder assemblyProbingFolder,
            IVirtualPathProvider virtualPathProvider,
            IVirtualPathMonitor virtualPathMonitor)
            : base(dependenciesFolder) {
            _hostEnvironment = hostEnvironment;
            _assemblyProbingFolder = assemblyProbingFolder;
            _virtualPathProvider = virtualPathProvider;
            _virtualPathMonitor = virtualPathMonitor;

            Logger = NullLogger.Instance;
        }

        public ILogger Logger { get; set; }

        public override int Order { get { return 30; } }

        public override string GetWebFormAssemblyDirective(DependencyDescriptor dependency) {
            return string.Format("<%@ Assembly Name=\"{0}\"%>", dependency.Name);
        }

        public override IEnumerable<string> GetWebFormVirtualDependencies(DependencyDescriptor dependency) {
            yield return _assemblyProbingFolder.GetAssemblyVirtualPath(dependency.Name);
        }

        public override void ExtensionRemoved(ExtensionLoadingContext ctx, DependencyDescriptor dependency) {
            if (_assemblyProbingFolder.AssemblyExists(dependency.Name)) {
                ctx.DeleteActions.Add(
                    () => {
                        Logger.Information("ExtensionRemoved: Deleting assembly \"{0}\" from probing directory", dependency.Name);
                        _assemblyProbingFolder.DeleteAssembly(dependency.Name);
                    });

                // We need to restart the appDomain if the assembly is loaded
                if (_hostEnvironment.IsAssemblyLoaded(dependency.Name)) {
                    Logger.Information("ExtensionRemoved: Module \"{0}\" is removed and its assembly is loaded, forcing AppDomain restart", dependency.Name);
                    ctx.RestartAppDomain = true;
                }
            }
        }

        public override void ExtensionActivated(ExtensionLoadingContext ctx, ExtensionDescriptor extension) {
            string sourceFileName = _virtualPathProvider.MapPath(GetAssemblyPath(extension));

            // Copy the assembly if it doesn't exist or if it is older than the source file.
            bool copyAssembly =
                !_assemblyProbingFolder.AssemblyExists(extension.Name) ||
                File.GetLastWriteTimeUtc(sourceFileName) > _assemblyProbingFolder.GetAssemblyDateTimeUtc(extension.Name);

            if (copyAssembly) {
                ctx.CopyActions.Add(() => _assemblyProbingFolder.StoreAssembly(extension.Name, sourceFileName));

                // We need to restart the appDomain if the assembly is loaded
                if (_hostEnvironment.IsAssemblyLoaded(extension.Name)) {
                    Logger.Information("ExtensionRemoved: Module \"{0}\" is activated with newer file and its assembly is loaded, forcing AppDomain restart", extension.Name);
                    ctx.RestartAppDomain = true;
                }
            }
        }

        public override void ExtensionDeactivated(ExtensionLoadingContext ctx, ExtensionDescriptor extension) {
            if (_assemblyProbingFolder.AssemblyExists(extension.Name)) {
                ctx.DeleteActions.Add(
                    () => {
                        Logger.Information("ExtensionDeactivated: Deleting assembly \"{0}\" from probing directory", extension.Name);
                        _assemblyProbingFolder.DeleteAssembly(extension.Name);
                    });

                // We need to restart the appDomain if the assembly is loaded
                if (_hostEnvironment.IsAssemblyLoaded(extension.Name)) {
                    Logger.Information("ExtensionDeactivated: Module \"{0}\" is deactivated and its assembly is loaded, forcing AppDomain restart", extension.Name);
                    ctx.RestartAppDomain = true;
                }
            }
        }

        public override void ReferenceActivated(ExtensionLoadingContext context, ExtensionReferenceProbeEntry referenceEntry) {
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

        public override void Monitor(ExtensionDescriptor descriptor, Action<IVolatileToken> monitor) {
            string assemblyPath = GetAssemblyPath(descriptor);
            if (assemblyPath != null) {
                Logger.Information("Monitoring virtual path \"{0}\"", assemblyPath);
                monitor(_virtualPathMonitor.WhenPathChanges(assemblyPath));
            }
        }

        public override IEnumerable<ExtensionReferenceProbeEntry> ProbeReferences(ExtensionDescriptor descriptor) {
            var assemblyPath = GetAssemblyPath(descriptor);
            if (assemblyPath == null)
                return Enumerable.Empty<ExtensionReferenceProbeEntry>();

            return _virtualPathProvider
                .ListFiles(_virtualPathProvider.GetDirectoryName(assemblyPath))
                .Where(s => StringComparer.OrdinalIgnoreCase.Equals(Path.GetExtension(s), ".dll"))
                .Where(s => !StringComparer.OrdinalIgnoreCase.Equals(Path.GetFileNameWithoutExtension(s), descriptor.Name))
                .Select(path => new ExtensionReferenceProbeEntry {
                    Descriptor = descriptor,
                    Loader = this,
                    Name = Path.GetFileNameWithoutExtension(path),
                    VirtualPath = path,
                    LastWriteTimeUtc = _virtualPathProvider.GetFileLastWriteTimeUtc(path)
                } );
        }

        public override bool IsCompatibleWithModuleReferences(ExtensionDescriptor extension, IEnumerable<ExtensionProbeEntry> references) {
            // A pre-compiled module is _not_ compatible with a dynamically loaded module
            // because a pre-compiled module usually references a pre-compiled assembly binary
            // which will have a different identity (i.e. name) from the dynamic module.
            bool result = references.All(r => r.Loader.GetType() != typeof (DynamicExtensionLoader));
            if (!result) {
                Logger.Information("Extension \"{0}\" will not be loaded as pre-compiled extension because one or more referenced extension is dynamically compiled", extension.Name);
            }
            return result;
        }

        public override ExtensionProbeEntry Probe(ExtensionDescriptor descriptor) {
            var assemblyPath = GetAssemblyPath(descriptor);
            if (assemblyPath == null)
                return null;

            return new ExtensionProbeEntry {
                Descriptor = descriptor,
                LastWriteTimeUtc = _virtualPathProvider.GetFileLastWriteTimeUtc(assemblyPath),
                Loader = this,
                VirtualPath = assemblyPath
            };
        }

        public override Assembly LoadReference(DependencyReferenceDescriptor reference) {
            return _assemblyProbingFolder.LoadAssembly(reference.Name);
        }

        protected override ExtensionEntry LoadWorker(ExtensionDescriptor descriptor) {
            var assembly = _assemblyProbingFolder.LoadAssembly(descriptor.Name);
            if (assembly == null)
                return null;

            //Logger.Information("Loading extension \"{0}\"", descriptor.Name);

            return new ExtensionEntry {
                Descriptor = descriptor,
                Assembly = assembly,
                ExportedTypes = assembly.GetExportedTypes()
            };
        }

        public string GetAssemblyPath(ExtensionDescriptor descriptor) {
            var assemblyPath = _virtualPathProvider.Combine(descriptor.Location, descriptor.Name, "bin",
                                                            descriptor.Name + ".dll");
            if (!_virtualPathProvider.FileExists(assemblyPath))
                return null;

            return assemblyPath;
        }
    }
}