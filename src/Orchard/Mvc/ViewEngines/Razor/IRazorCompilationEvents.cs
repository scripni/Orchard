﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Razor.Generator;
using System.Web.WebPages.Razor;
using Orchard.Environment;
using Orchard.Environment.Extensions.Loaders;
using Orchard.FileSystems.Dependencies;

namespace Orchard.Mvc.ViewEngines.Razor {
    public interface IRazorCompilationEvents {
        void CodeGenerationStarted(RazorBuildProvider provider);
        void CodeGenerationCompleted(RazorBuildProvider provider, CodeGenerationCompleteEventArgs e);
    }

    /// <summary>
    /// The purpose of this class is to notify the Razor View Engine of Module and Theme
    /// dependencies when compiling Views, so that the Razor Views build provider will add proper 
    /// assembly references when calling the compiler.
    /// For example, if Module A depends on Module Bar and some other 3rd party DLL "Foo.dll",
    /// we will notify the Razor View Engine of the following dependencies:
    /// * BuildManager.GetCompiledAssembly("~/Modules/Bar/Bar.csproj");
    /// * Assembly.Load("Foo");
    /// </summary>
    public class DefaultRazorCompilationEvents : IRazorCompilationEvents {
        private readonly IDependenciesFolder _dependenciesFolder;
        private readonly IBuildManager _buildManager;
        private readonly IEnumerable<IExtensionLoader> _loaders;
        private readonly IAssemblyLoader _assemblyLoader;

        public DefaultRazorCompilationEvents(
            IDependenciesFolder dependenciesFolder,
            IBuildManager buildManager,
            IEnumerable<IExtensionLoader> loaders,
            IAssemblyLoader assemblyLoader) {

            _dependenciesFolder = dependenciesFolder;
            _buildManager = buildManager;
            _loaders = loaders;
            _assemblyLoader = assemblyLoader;
        }

        public void CodeGenerationStarted(RazorBuildProvider provider) {
            DependencyDescriptor moduleDependencyDescriptor = GetModuleDependencyDescriptor(provider.VirtualPath);

            IEnumerable<DependencyDescriptor> dependencyDescriptors = _dependenciesFolder.LoadDescriptors();
            List<DependencyDescriptor> filteredDependencyDescriptors;
            if (moduleDependencyDescriptor != null) {
                // Add module
                filteredDependencyDescriptors = new List<DependencyDescriptor> { moduleDependencyDescriptor };

                // Add module's references
                filteredDependencyDescriptors.AddRange(moduleDependencyDescriptor.References
                    .SelectMany(reference => dependencyDescriptors
                        .Where(dependency => dependency.Name == reference.Name)));
            }
            else {
                // Fall back for themes
                filteredDependencyDescriptors = dependencyDescriptors.ToList();
            }

            var entries = filteredDependencyDescriptors
                .SelectMany(descriptor => _loaders
                                              .Where(loader => descriptor.LoaderName == loader.Name)
                                              .Select(loader => new {
                                                  loader,
                                                  descriptor,
                                                  directive = loader.GetWebFormAssemblyDirective(descriptor),
                                                  dependencies = loader.GetWebFormVirtualDependencies(descriptor)
                                              }));

            foreach (var entry in entries) {
                if (entry.directive != null) {
                    if (entry.directive.StartsWith("<%@ Assembly Name=\"")) {
                        var assembly = _assemblyLoader.Load(entry.descriptor.Name);
                        if (assembly != null)
                            provider.AssemblyBuilder.AddAssemblyReference(assembly);
                    }
                    else if (entry.directive.StartsWith("<%@ Assembly Src=\"")) {
                        // Returned assembly may be null if the .csproj file doesn't containt any .cs file, for example
                        var assembly = _buildManager.GetCompiledAssembly(entry.descriptor.VirtualPath);
                        if (assembly != null)
                            provider.AssemblyBuilder.AddAssemblyReference(assembly);
                    }
                }
                foreach (var virtualDependency in entry.dependencies) {
                    provider.AddVirtualPathDependency(virtualDependency);
                }
            }

            foreach (var virtualDependency in _dependenciesFolder.GetViewCompilationDependencies()) {
                provider.AddVirtualPathDependency(virtualDependency);
            }
        }

        private DependencyDescriptor GetModuleDependencyDescriptor(string virtualPath) {
            var appRelativePath = VirtualPathUtility.ToAppRelative(virtualPath);
            var prefix = PrefixMatch(appRelativePath, new [] { "~/Modules/", "~/Core/"});
            if (prefix == null)
                return null;

            var moduleName = ModuleMatch(appRelativePath, prefix);
            if (moduleName == null)
                return null;

            return _dependenciesFolder.GetDescriptor(moduleName);
        }

        private static string ModuleMatch(string virtualPath, string prefix) {
            var index = virtualPath.IndexOf('/', prefix.Length, virtualPath.Length - prefix.Length);
            if (index < 0)
                return null;

            var moduleName = virtualPath.Substring(prefix.Length, index - prefix.Length);
            return (string.IsNullOrEmpty(moduleName) ? null : moduleName);
        }

        private static string PrefixMatch(string virtualPath, params string[] prefixes) {
            return prefixes
                .FirstOrDefault(p => virtualPath.StartsWith(p, StringComparison.OrdinalIgnoreCase));
        }

        public void CodeGenerationCompleted(RazorBuildProvider provider, CodeGenerationCompleteEventArgs e) {
        }
    }
}