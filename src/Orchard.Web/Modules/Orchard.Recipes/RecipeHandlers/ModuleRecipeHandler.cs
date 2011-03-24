﻿using System;
using System.Linq;
using System.Web.Hosting;
using Orchard.Data.Migration;
using Orchard.Environment.Extensions;
using Orchard.Environment.Extensions.Models;
using Orchard.Environment.Features;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.Packaging.Models;
using Orchard.Packaging.Services;
using Orchard.Recipes.Models;
using Orchard.Recipes.Services;

namespace Orchard.Recipes.RecipeHandlers {
    public class ModuleRecipeHandler : IRecipeHandler {
        private readonly IPackagingSourceManager _packagingSourceManager;
        private readonly IPackageManager _packageManager;
        private readonly IExtensionManager _extensionManager;
        private readonly IFeatureManager _featureManager;
        private readonly IDataMigrationManager _dataMigrationManager;

        public ModuleRecipeHandler(
            IPackagingSourceManager packagingSourceManager, 
            IPackageManager packageManager, 
            IExtensionManager extensionManager,
            IFeatureManager featureManager,
            IDataMigrationManager dataMigrationManager) {
            _packagingSourceManager = packagingSourceManager;
            _packageManager = packageManager;
            _extensionManager = extensionManager;
            _featureManager = featureManager;
            _dataMigrationManager = dataMigrationManager;

            Logger = NullLogger.Instance;
            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }
        ILogger Logger { get; set; }

        // <Module name="module1" [repository="somerepo"] version="1.1" />
        // install modules from feed.
        public void ExecuteRecipeStep(RecipeContext recipeContext) {
            if (!String.Equals(recipeContext.RecipeStep.Name, "Module", StringComparison.OrdinalIgnoreCase)) {
                return;
            }
            string name = null, version = null, repository = null;

            foreach (var attribute in recipeContext.RecipeStep.Step.Attributes()) {
                if (String.Equals(attribute.Name.LocalName, "name", StringComparison.OrdinalIgnoreCase)) {
                    name = attribute.Value;
                }
                else if (String.Equals(attribute.Name.LocalName, "version", StringComparison.OrdinalIgnoreCase)) {
                    version = attribute.Value;
                }
                else if (String.Equals(attribute.Name.LocalName, "repository", StringComparison.OrdinalIgnoreCase)) {
                    repository = attribute.Value;
                }
                else {
                    throw new InvalidOperationException(string.Format("Unrecognized attribute {0} encountered in step Module.", attribute.Name.LocalName));
                }
            }

            if (name == null) {
                throw new InvalidOperationException("Name is required in a Module declaration in a recipe file.");
            }

            // download and install module from the orchard feed or a custom feed if repository is specified.
            bool enforceVersion = version != null;
            bool installed = false;

            var packagingSource = _packagingSourceManager.GetSources().FirstOrDefault();
            if (repository != null) {
                enforceVersion = false;
                packagingSource = new PackagingSource {FeedTitle = repository, FeedUrl = repository};
            }

            var packagingEntry = _packagingSourceManager.GetExtensionList(packagingSource,
                packages => packages.Where(package =>
                    package.PackageType.Equals(DefaultExtensionTypes.Module) &&
                    package.Id.Equals(name, StringComparison.OrdinalIgnoreCase) &&
                    (!enforceVersion || package.Version.Equals(version, StringComparison.OrdinalIgnoreCase))))
                .FirstOrDefault();

            if (packagingEntry != null) {
                _packageManager.Install(packagingEntry.PackageId, packagingEntry.Version, packagingSource.FeedUrl, HostingEnvironment.MapPath("~/")); 
                foreach (var features in
                    from extensionDescriptor in _extensionManager.AvailableExtensions()
                    where extensionDescriptor.Name.Equals(packagingEntry.Title, StringComparison.OrdinalIgnoreCase)
                    select extensionDescriptor.Features.Select(f => f.Name).ToArray()) {

                    _featureManager.EnableFeatures(features);
                    _dataMigrationManager.Update(features);

                    installed = true;
                }
            }

            if (!installed) {
                throw new InvalidOperationException(string.Format("Module {0} was not found in the specified location.", name));
            }

            recipeContext.Executed = true;
        }
    }
}