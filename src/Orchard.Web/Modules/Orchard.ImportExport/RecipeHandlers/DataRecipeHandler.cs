﻿using System;
using Orchard.ContentManagement;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.Recipes.Models;
using Orchard.Recipes.Services;

namespace Orchard.ImportExport.RecipeHandlers {
    public class DataRecipeHandler : IRecipeHandler {
        private readonly IOrchardServices _orchardServices;

        public DataRecipeHandler(IOrchardServices orchardServices) {
            _orchardServices = orchardServices;
            Logger = NullLogger.Instance;
            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }
        ILogger Logger { get; set; }

        // <Data />
        // Import Data
        public void ExecuteRecipeStep(RecipeContext recipeContext) {
            if (!String.Equals(recipeContext.RecipeStep.Name, "Data", StringComparison.OrdinalIgnoreCase)) {
                return;
            }

            // First pass to resolve content items from content identities for all content items, new and old.
            var importContentSession = new ImportContentSession(_orchardServices.ContentManager);
            foreach (var element in recipeContext.RecipeStep.Step.Elements()) {
                var elementId = element.Attribute("Id");
                if (elementId == null)
                    continue;

                var identity = elementId.Value;

                var item = importContentSession.Get(identity);
                if (item == null) {
                    item = _orchardServices.ContentManager.New(element.Name.LocalName);
                    _orchardServices.ContentManager.Create(item);
                    importContentSession.Store(identity, item);
                }
            }

            // Second pass to import the content items.
            foreach (var element in recipeContext.RecipeStep.Step.Elements()) {
                _orchardServices.ContentManager.Import(element, importContentSession);
            }

            recipeContext.Executed = true;
        }
    }
}
