using System;
using System.Linq;
using Orchard.Environment.Extensions.Models;
using Orchard.FileSystems.Dependencies;
using Orchard.Logging;

namespace Orchard.Environment.Extensions.Loaders {
    public class AreaExtensionLoader : ExtensionLoaderBase {
        private readonly string _hostAssemblyName = "Orchard.Web";
        private readonly IAssemblyLoader _assemblyLoader;

        public AreaExtensionLoader(IDependenciesFolder dependenciesFolder, IAssemblyLoader assemblyLoader)
            : base(dependenciesFolder) {
            _assemblyLoader = assemblyLoader;

            Logger = NullLogger.Instance;
        }

        public ILogger Logger { get; set; }

        public override int Order { get { return 50; } }

        public override ExtensionProbeEntry Probe(ExtensionDescriptor descriptor) {
            if (descriptor.Location == "~/Areas") {
                return new ExtensionProbeEntry {
                    Descriptor = descriptor,
                    Loader = this,
                    LastWriteTimeUtc = DateTime.MinValue,
                    VirtualPath = "~/Areas/" + descriptor.Name,
                };
            }
            return null;
        }

        protected override ExtensionEntry LoadWorker(ExtensionDescriptor descriptor) {
            //Logger.Information("Loading extension \"{0}\"", descriptor.Name);

            var assembly = _assemblyLoader.Load(_hostAssemblyName);
            if (assembly == null) {
                Logger.Warning("Support for 'Areas' modules disabled because assembly '{0}' could not be loaded", _hostAssemblyName);
                return null;
            }

            return new ExtensionEntry {
                Descriptor = descriptor,
                Assembly = assembly,
                ExportedTypes = assembly.GetExportedTypes().Where(x => IsTypeFromModule(x, descriptor))
            };
        }

        private bool IsTypeFromModule(Type type, ExtensionDescriptor descriptor) {
            return (type.Namespace + ".").StartsWith(_hostAssemblyName + ".Areas." + descriptor.Name + ".");
        }
    }
}