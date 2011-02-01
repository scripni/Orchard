using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Orchard.Caching;
using Orchard.Environment.Extensions.Models;
using Orchard.FileSystems.WebSite;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.Utility.Extensions;

namespace Orchard.Environment.Extensions.Folders {
    public class ExtensionFolders : IExtensionFolders {
        private readonly IEnumerable<string> _paths;
        private readonly string _manifestName;
        private readonly string _extensionType;
        private readonly bool _manifestIsOptional;
        private readonly ICacheManager _cacheManager;
        private readonly IWebSiteFolder _webSiteFolder;

        protected ExtensionFolders(
            IEnumerable<string> paths,
            string manifestName,
            bool manifestIsOptional,
            ICacheManager cacheManager,
            IWebSiteFolder webSiteFolder) {
            _paths = paths;
            _manifestName = manifestName;
            _extensionType = manifestName == "Theme.txt" ? DefaultExtensionTypes.Theme : DefaultExtensionTypes.Module;
            _manifestIsOptional = manifestIsOptional;
            _cacheManager = cacheManager;
            _webSiteFolder = webSiteFolder;
            Logger = NullLogger.Instance;
            T = NullLocalizer.Instance;
        }

        Localizer T { get; set; }
        ILogger Logger { get; set; }

        public IEnumerable<ExtensionDescriptor> AvailableExtensions() {
            var list = new List<ExtensionDescriptor>();
            foreach (var locationPath in _paths) {
                var path = locationPath;
                var subList = _cacheManager.Get(locationPath, ctx => {
                    ctx.Monitor(_webSiteFolder.WhenPathChanges(ctx.Key));
                    var subfolderPaths = _webSiteFolder.ListDirectories(ctx.Key);
                    var localList = new List<ExtensionDescriptor>();
                    foreach (var subfolderPath in subfolderPaths) {
                        var extensionId = Path.GetFileName(subfolderPath.TrimEnd('/', '\\'));
                        var manifestPath = Path.Combine(subfolderPath, _manifestName);
                        try {
                            var descriptor = GetExtensionDescriptor(path, extensionId, manifestPath);

                            if (descriptor == null)
                                continue;

                            if (descriptor.Path != null && !descriptor.Path.IsValidUrlSegment()) {
                                Logger.Error("The module '{0}' could not be loaded because it has an invalid Path ({1}). It was ignored. The Path if specified must be a valid URL segment. The best bet is to stick with letters and numbers with no spaces.",
                                    extensionId,
                                    descriptor.Path);
                                continue;
                            }

                            if (descriptor.Path == null) {
                                descriptor.Path = descriptor.Name.IsValidUrlSegment()
                                    ? descriptor.Name
                                    : descriptor.Id;
                            }

                            localList.Add(descriptor);
                        }
                        catch (Exception ex) {
                            // Ignore invalid module manifests
                            Logger.Error(ex, "The module '{0}' could not be loaded. It was ignored.", extensionId);
                        }
                    }
                    return localList;
                });
                list.AddRange(subList);
            }

            return list;
        }

        public static ExtensionDescriptor GetDescriptorForExtension(string locationPath, string extensionId, string extensionType, string manifestText) {
            Dictionary<string, string> manifest = ParseManifest(manifestText);
            var extensionDescriptor = new ExtensionDescriptor {
                Location = locationPath,
                Id = extensionId,
                ExtensionType = extensionType,
                Name = GetValue(manifest, "Name") ?? extensionId,
                Path = GetValue(manifest, "Path"),
                Description = GetValue(manifest, "Description"),
                Version = GetValue(manifest, "Version"),
                OrchardVersion = GetValue(manifest, "OrchardVersion"),
                Author = GetValue(manifest, "Author"),
                WebSite = GetValue(manifest, "Website"),
                Tags = GetValue(manifest, "Tags"),
                AntiForgery = GetValue(manifest, "AntiForgery"),
                Zones = GetValue(manifest, "Zones"),
                BaseTheme = GetValue(manifest, "BaseTheme"),
            };
            extensionDescriptor.Features = GetFeaturesForExtension(GetValue(manifest, "Features"), extensionDescriptor);

            return extensionDescriptor;
        }

        private ExtensionDescriptor GetExtensionDescriptor(string locationPath, string extensionId, string manifestPath) {
            return _cacheManager.Get(manifestPath, context => {
                context.Monitor(_webSiteFolder.WhenPathChanges(manifestPath));
                var manifestText = _webSiteFolder.ReadFile(manifestPath);
                if (manifestText == null) {
                    if (_manifestIsOptional) {
                        manifestText = string.Format("Id: {0}", extensionId);
                    }
                    else {
                        return null;
                    }
                }

                return GetDescriptorForExtension(locationPath, extensionId, manifestText);
            });
        }

        private ExtensionDescriptor GetDescriptorForExtension(string locationPath, string extensionId, string manifestText) {
            return GetDescriptorForExtension(locationPath, extensionId, _extensionType, manifestText);
        }

        private static Dictionary<string, string> ParseManifest(string manifestText) {
            var manifest = new Dictionary<string, string>();

            using (StringReader reader = new StringReader(manifestText)) {
                string line;
                while ((line = reader.ReadLine()) != null) {
                    string[] field = line.Split(new[] { ":" }, 2, StringSplitOptions.None);
                    int fieldLength = field.Length;
                    if (fieldLength != 2)
                        continue;
                    for (int i = 0; i < fieldLength; i++) {
                        field[i] = field[i].Trim();
                    }
                    switch (field[0]) {
                        case "Name":
                            manifest.Add("Name", field[1]);
                            break;
                        case "Path":
                            manifest.Add("Path", field[1]);
                            break;
                        case "Description":
                            manifest.Add("Description", field[1]);
                            break;
                        case "Version":
                            manifest.Add("Version", field[1]);
                            break;
                        case "OrchardVersion":
                            manifest.Add("OrchardVersion", field[1]);
                            break;
                        case "Author":
                            manifest.Add("Author", field[1]);
                            break;
                        case "Website":
                            manifest.Add("Website", field[1]);
                            break;
                        case "Tags":
                            manifest.Add("Tags", field[1]);
                            break;
                        case "AntiForgery":
                            manifest.Add("AntiForgery", field[1]);
                            break;
                        case "Zones":
                            manifest.Add("Zones", field[1]);
                            break;
                        case "BaseTheme":
                            manifest.Add("BaseTheme", field[1]);
                            break;
                        case "Features":
                            manifest.Add("Features", reader.ReadToEnd());
                            break;
                    }
                }
            }

            return manifest;
        }

        private static IEnumerable<FeatureDescriptor> GetFeaturesForExtension(string featuresText, ExtensionDescriptor extensionDescriptor) {
            var featureDescriptors = new List<FeatureDescriptor>();
            if (featuresText != null) {
                FeatureDescriptor featureDescriptor = null;
                using (StringReader reader = new StringReader(featuresText)) {
                    string line;
                    while ((line = reader.ReadLine()) != null) {
                        if (IsFeatureDeclaration(line)) {
                            if (featureDescriptor != null) {
                                featureDescriptors.Add(featureDescriptor);
                                featureDescriptor = null;
                            }
                            featureDescriptor = new FeatureDescriptor {
                                Extension = extensionDescriptor
                            };
                            string[] featureDeclaration = line.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
                            featureDescriptor.Id = featureDeclaration[0].Trim();
                            if (featureDescriptor.Id == extensionDescriptor.Id) {
                                featureDescriptor.Name = extensionDescriptor.Name;
                            }
                        }
                        else if (IsFeatureFieldDeclaration(line)) {
                            if (featureDescriptor != null) {
                                string[] featureField = line.Split(new[] { ":" }, 2, StringSplitOptions.None);
                                int featureFieldLength = featureField.Length;
                                if (featureFieldLength != 2)
                                    continue;
                                for (int i = 0; i < featureFieldLength; i++) {
                                    featureField[i] = featureField[i].Trim();
                                }
                                switch (featureField[0]) {
                                    case "Name":
                                        featureDescriptor.Name = featureField[1];
                                        break;
                                    case "Description":
                                        featureDescriptor.Description = featureField[1];
                                        break;
                                    case "Category":
                                        featureDescriptor.Category = featureField[1];
                                        break;
                                    case "Dependencies":
                                        featureDescriptor.Dependencies = ParseFeatureDependenciesEntry(featureField[1]);
                                        break;
                                }
                            }
                            else {
                                string message = string.Format("The line {0} in manifest for extension {1} was ignored", line, extensionDescriptor.Id);
                                throw new ArgumentException(message);
                            }
                        }
                        else {
                            string message = string.Format("The line {0} in manifest for extension {1} was ignored", line, extensionDescriptor.Id);
                            throw new ArgumentException(message);
                        }
                    }
                    if (featureDescriptor != null)
                        featureDescriptors.Add(featureDescriptor);
                }
            }

            if (!featureDescriptors.Any(fd => fd.Id == extensionDescriptor.Id)) {
                featureDescriptors.Add(new FeatureDescriptor {
                    Id = extensionDescriptor.Id,
                    Name = extensionDescriptor.Name,
                    Dependencies = new string[0],
                    Extension = extensionDescriptor
                });
            }

            return featureDescriptors;
        }

        private static bool IsFeatureFieldDeclaration(string line) {
            if (line.StartsWith("\t\t") ||
                line.StartsWith("\t    ") ||
                line.StartsWith("    ") ||
                line.StartsWith("    \t"))
                return true;

            return false;
        }

        private static bool IsFeatureDeclaration(string line) {
            int lineLength = line.Length;
            if (line.StartsWith("\t") && lineLength >= 2) {
                return !Char.IsWhiteSpace(line[1]);
            }
            if (line.StartsWith("    ") && lineLength >= 5)
                return !Char.IsWhiteSpace(line[4]);

            return false;
        }

        private static string[] ParseFeatureDependenciesEntry(string dependenciesEntry) {
            var dependencies = new List<string>();
            foreach (var s in dependenciesEntry.Split(',')) {
                dependencies.Add(s.Trim());
            }
            return dependencies.ToArray();
        }

        private static string GetValue(IDictionary<string, string> fields, string key) {
            string value;
            return fields.TryGetValue(key, out value) ? value : null;
        }
    }
}