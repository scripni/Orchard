﻿using System;
using System.Collections.Generic;
using System.Linq;
using Autofac.Features.Metadata;
using Orchard.Caching;
using Orchard.Environment.Extensions;
using Orchard.Environment.Extensions.Models;
using Orchard.Utility;

namespace Orchard.DisplayManagement.Descriptors {

    public class DefaultShapeTableManager : IShapeTableManager {
        private readonly IEnumerable<Meta<IShapeTableProvider>> _bindingStrategies;
        private readonly IExtensionManager _extensionManager;
        private readonly ICacheManager _cacheManager;

        public DefaultShapeTableManager(
            IEnumerable<Meta<IShapeTableProvider>> bindingStrategies,
            IExtensionManager extensionManager,
            ICacheManager cacheManager) {
            _extensionManager = extensionManager;
            _cacheManager = cacheManager;
            _bindingStrategies = bindingStrategies;
        }

        public ShapeTable GetShapeTable(string themeName) {
            return _cacheManager.Get(themeName ?? "", x => {
                var builderFactory = new ShapeTableBuilderFactory();
                foreach (var bindingStrategy in _bindingStrategies) {
                    Feature strategyDefaultFeature = bindingStrategy.Metadata.ContainsKey("Feature") ?
                        (Feature) bindingStrategy.Metadata["Feature"] : 
                        null;

                    var builder = builderFactory.CreateTableBuilder(strategyDefaultFeature);
                    bindingStrategy.Value.Discover(builder);
                }

                var alterations = builderFactory.BuildAlterations()
                    .Where(alteration => IsModuleOrRequestedTheme(alteration, themeName))
                    .OrderByDependencies(AlterationHasDependency);

                var descriptors = alterations.GroupBy(alteration => alteration.ShapeType, StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.Aggregate(
                        new ShapeDescriptor { ShapeType = group.Key },
                        (descriptor, alteration) => {
                            alteration.Alter(descriptor);
                            return descriptor;
                        }));

                return new ShapeTable {
                    Descriptors = descriptors.ToDictionary(sd => sd.ShapeType, StringComparer.OrdinalIgnoreCase),
                    Bindings = descriptors.SelectMany(sd => sd.Bindings).ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase),
                };
            });
        }

        private static bool AlterationHasDependency(ShapeAlteration item, ShapeAlteration subject) {
            return ExtensionManager.HasDependency(item.Feature.Descriptor, subject.Feature.Descriptor);
        }

        private bool IsModuleOrRequestedTheme(ShapeAlteration alteration, string themeName) {
            if (alteration == null ||
                alteration.Feature == null ||
                alteration.Feature.Descriptor == null ||
                alteration.Feature.Descriptor.Extension == null) {
                return false;
            }

            var extensionType = alteration.Feature.Descriptor.Extension.ExtensionType;
            if (DefaultExtensionTypes.IsModule(extensionType)) {
                return true;
            }

            if (DefaultExtensionTypes.IsTheme(extensionType)) {
                // alterations from themes must be from the given theme or a base theme
                var featureName = alteration.Feature.Descriptor.Id;
                return featureName == themeName || IsBaseTheme(featureName, themeName);
            }

            return false;
        }

        private bool IsBaseTheme(string featureName, string themeName) {
            // determine if the given feature is a base theme of the given theme
            var availableFeatures = _extensionManager.AvailableFeatures();

            var themeFeature = availableFeatures.SingleOrDefault(fd => fd.Id == themeName);
            while(themeFeature != null) {
                var baseTheme = themeFeature.Extension.BaseTheme;
                if (String.IsNullOrEmpty(baseTheme)) {
                    return false;
                }
                if (featureName == baseTheme) {
                    return true;
                }
                themeFeature = availableFeatures.SingleOrDefault(fd => fd.Id == baseTheme);
            }
            return false;
        }

        class ShapeTableBuilderFactory {
            readonly IList<ShapeAlterationBuilder> _alterationBuilders = new List<ShapeAlterationBuilder>();

            public ShapeTableBuilder CreateTableBuilder(Feature feature) {
                return new ShapeTableBuilder(_alterationBuilders, feature);
            }

            public IEnumerable<ShapeAlteration> BuildAlterations() {
                return _alterationBuilders.Select(b => b.Build());
            }

        }



    }
}
