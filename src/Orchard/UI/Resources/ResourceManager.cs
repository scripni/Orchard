using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Autofac.Features.Metadata;
using Orchard.Environment.Extensions.Models;

namespace Orchard.UI.Resources {
    public class ResourceManager : IResourceManager {
        private readonly Dictionary<Tuple<String, String>, RequireSettings> _required = new Dictionary<Tuple<String, String>, RequireSettings>();
        private readonly List<LinkEntry> _links = new List<LinkEntry>();
        private readonly Dictionary<string, MetaEntry> _metas = new Dictionary<string, MetaEntry> {
            { "generator", new MetaEntry { Content = "Orchard", Name = "generator" } }
        };
        private readonly Dictionary<string, IList<ResourceRequiredContext>> _builtResources = new Dictionary<string, IList<ResourceRequiredContext>>(StringComparer.OrdinalIgnoreCase);
        private readonly IEnumerable<Meta<IResourceManifestProvider>> _providers;
        private ResourceManifest _dynamicManifest;
        private List<String> _headScripts;
        private List<String> _footScripts;
        private IEnumerable<IResourceManifest> _manifests;

        private static string ToAppRelativePath(string resourcePath) {
            if (!String.IsNullOrEmpty(resourcePath) && !Uri.IsWellFormedUriString(resourcePath, UriKind.Absolute)) {
                resourcePath = VirtualPathUtility.ToAppRelative(resourcePath);
            }
            return resourcePath;
        }

        private static string FixPath(string resourcePath, string relativeFromPath) {
            if (!String.IsNullOrEmpty(resourcePath) && !VirtualPathUtility.IsAbsolute(resourcePath) && !Uri.IsWellFormedUriString(resourcePath, UriKind.Absolute)) {
                // appears to be a relative path (e.g. 'foo.js' or '../foo.js', not "/foo.js" or "http://..")
                if (String.IsNullOrEmpty(relativeFromPath)) {
                    throw new InvalidOperationException("ResourcePath cannot be relative unless a base relative path is also provided.");
                }
                resourcePath = VirtualPathUtility.ToAbsolute(VirtualPathUtility.Combine(relativeFromPath, resourcePath));
            }
            return resourcePath;
        }

        private static TagBuilder GetTagBuilder(ResourceDefinition resource, string url) {
            var tagBuilder = new TagBuilder(resource.TagName);
            tagBuilder.MergeAttributes(resource.TagBuilder.Attributes);
            if (!String.IsNullOrEmpty(resource.FilePathAttributeName)) {
                if (!String.IsNullOrEmpty(url)) {
                    if (VirtualPathUtility.IsAppRelative(url)) {
                        url = VirtualPathUtility.ToAbsolute(url);
                    }
                    tagBuilder.MergeAttribute(resource.FilePathAttributeName, url, true);
                }
            }
            return tagBuilder;
        }

        public static void WriteResource(TextWriter writer, ResourceDefinition resource, string url, string condition) {
            if (!string.IsNullOrEmpty(condition)) {
                writer.WriteLine("<!--[if " + condition + "]>");
            }
            writer.WriteLine(GetTagBuilder(resource, url).ToString(resource.TagRenderMode));
            if (!string.IsNullOrEmpty(condition)) {
                writer.WriteLine("<![endif]-->");
            }
        }

        public ResourceManager(IEnumerable<Meta<IResourceManifestProvider>> resourceProviders) {
            _providers = resourceProviders;
        }

        public IEnumerable<IResourceManifest> ResourceProviders {
            get {
                if (_manifests == null) {
                    var builder = new ResourceManifestBuilder();
                    foreach (var provider in _providers) {
                        builder.Feature = provider.Metadata.ContainsKey("Feature") ?
                            (Feature) provider.Metadata["Feature"] :
                            null;
                        provider.Value.BuildManifests(builder);
                    }
                    _manifests = builder.ResourceManifests;
                }
                return _manifests;
            }
        }

        public virtual ResourceManifest DynamicResources {
            get {
                return _dynamicManifest ?? (_dynamicManifest = new ResourceManifest());
            }
        }

        public virtual RequireSettings Require(string resourceType, string resourceName) {
            if (resourceType == null) {
                throw new ArgumentNullException("resourceType");
            }
            if (resourceName == null) {
                throw new ArgumentNullException("resourceName");
            }
            RequireSettings settings;
            var key = new Tuple<string, string>(resourceType, resourceName);
            if (!_required.TryGetValue(key, out settings)) {
                settings = new RequireSettings {Type = resourceType, Name = resourceName};
                _required[key] = settings;
            }
            _builtResources[resourceType] = null;
            return settings;
        }

        public virtual RequireSettings Include(string resourceType, string resourcePath, string resourceDebugPath) {
            return Include(resourceType, resourcePath, null);
        }

        public virtual RequireSettings Include(string resourceType, string resourcePath, string resourceDebugPath, string relativeFromPath) {
            if (resourceType == null) {
                throw new ArgumentNullException("resourceType");
            }
            if (resourcePath == null) {
                throw new ArgumentNullException("resourcePath");
            }

            if (VirtualPathUtility.IsAppRelative(resourcePath)) {
                // ~/ ==> convert to absolute path (e.g. /orchard/..)
                resourcePath = VirtualPathUtility.ToAbsolute(resourcePath);
            }
            resourcePath = FixPath(resourcePath, relativeFromPath);
            resourceDebugPath = FixPath(resourceDebugPath, relativeFromPath);
            return Require(resourceType, ToAppRelativePath(resourcePath)).Define(d => d.SetUrl(resourcePath, resourceDebugPath));
        }

        public virtual void RegisterHeadScript(string script) {
            if (_headScripts == null) {
                _headScripts = new List<string>();
            }
            _headScripts.Add(script);
        }

        public virtual void RegisterFootScript(string script) {
            if (_footScripts == null) {
                _footScripts = new List<string>();
            }
            _footScripts.Add(script);
        }

        public virtual void NotRequired(string resourceType, string resourceName) {
            if (resourceType == null) {
                throw new ArgumentNullException("resourceType");
            }
            if (resourceName == null) {
                throw new ArgumentNullException("resourceName");
            }
            var key = new Tuple<string, string>(resourceType, resourceName);
            _builtResources[resourceType] = null;
            _required.Remove(key);
        }

        public virtual ResourceDefinition FindResource(RequireSettings settings) {
            // find the resource with the given type and name
            // that has at least the given version number. If multiple,
            // return the resource with the greatest version number.
            // If not found and an inlineDefinition is given, define the resource on the fly
            // using the action.
            var name = settings.Name ?? "";
            var type = settings.Type;
            var resource = (from p in ResourceProviders
                            from r in p.GetResources(type)
                            where name.Equals(r.Key, StringComparison.OrdinalIgnoreCase)
                            orderby r.Value.Version descending
                            select r.Value).FirstOrDefault();
            if (resource == null && _dynamicManifest != null) {
                resource = (from r in _dynamicManifest.GetResources(type)
                            where name.Equals(r.Key, StringComparison.OrdinalIgnoreCase)
                            orderby r.Value.Version descending
                            select r.Value).FirstOrDefault();
            }
            if (resource == null && settings.InlineDefinition != null) {
                // defining it on the fly
                resource = DynamicResources.DefineResource(type, name)
                    .SetBasePath(settings.BasePath);
                settings.InlineDefinition(resource);
            }
            return resource;
        }

        public virtual IEnumerable<RequireSettings> GetRequiredResources(string type) {
            return from r in _required
                   where r.Key.Item1 == type
                   select r.Value;
        }

        public virtual IList<LinkEntry> GetRegisteredLinks() {
            return _links.AsReadOnly();
        }

        public virtual IList<MetaEntry> GetRegisteredMetas() {
            return _metas.Values.ToList().AsReadOnly();
        }

        public virtual IList<String> GetRegisteredHeadScripts() {
            return _headScripts == null ? null : _headScripts.AsReadOnly();
        }

        public virtual IList<String> GetRegisteredFootScripts() {
            return _footScripts == null ? null : _footScripts.AsReadOnly();
        }

        public virtual IList<ResourceRequiredContext> BuildRequiredResources(string resourceType) {
            IList<ResourceRequiredContext> requiredResources;
            if (_builtResources.TryGetValue(resourceType, out requiredResources) && requiredResources != null) {
                return requiredResources;
            }
            var allResources = new OrderedDictionary();
            foreach (var settings in GetRequiredResources(resourceType)) {
                var resource = FindResource(settings);
                if (resource == null) {
                    throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, "A '{1}' named '{0}' could not be found.", settings.Name, settings.Type));
                }
                ExpandDependencies(resource, settings, allResources);
            }
            requiredResources = (from DictionaryEntry entry in allResources
                                 select new ResourceRequiredContext {Resource = (ResourceDefinition) entry.Key, Settings = (RequireSettings) entry.Value}).ToList();
            _builtResources[resourceType] = requiredResources;
            return requiredResources;
        }

        protected virtual void ExpandDependencies(ResourceDefinition resource, RequireSettings settings, OrderedDictionary allResources) {
            if (resource == null) {
                return;
            }
            if (allResources.Contains(resource)) {
                settings = ((RequireSettings) allResources[resource]).Combine(settings);
            }
            settings.Type = resource.Type;
            settings.Name = resource.Name;
            if (resource.Dependencies != null) {
                var dependencies = from d in resource.Dependencies
                                   select FindResource(new RequireSettings { Type = resource.Type, Name = d });
                foreach (var dependency in dependencies) {
                    if (dependency == null) {
                        continue;
                    }
                    ExpandDependencies(dependency, settings, allResources);
                }
            }
            allResources[resource] = settings;
        }

        public void RegisterLink(LinkEntry link) {
            _links.Add(link);
        }

        public void SetMeta(MetaEntry meta) {
            if (meta == null || String.IsNullOrEmpty(meta.Name)) {
                return;
            }
            _metas[meta.Name] = meta;
        }

        public void AppendMeta(MetaEntry meta, string contentSeparator) {
            if (meta == null || String.IsNullOrEmpty(meta.Name)) {
                return;
            }
            MetaEntry existingMeta;
            if (_metas.TryGetValue(meta.Name, out existingMeta)) {
                meta = MetaEntry.Combine(existingMeta, meta, contentSeparator);
            }
            _metas[meta.Name] = meta;
        }

    }
}
