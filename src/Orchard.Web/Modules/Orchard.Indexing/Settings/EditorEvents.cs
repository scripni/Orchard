﻿using System.Collections.Generic;
using Orchard.ContentManagement;
using Orchard.ContentManagement.MetaData;
using Orchard.ContentManagement.MetaData.Builders;
using Orchard.ContentManagement.MetaData.Models;
using Orchard.ContentManagement.ViewModels;

namespace Orchard.Indexing.Settings {
    public class EditorEvents : ContentDefinitionEditorEventsBase {
        public override IEnumerable<TemplateViewModel> TypeEditor(ContentTypeDefinition definition) {
            var model = definition.Settings.GetModel<TypeIndexing>();
            yield return DefinitionTemplate(model);
        }

        public override IEnumerable<TemplateViewModel> TypeEditorUpdate(ContentTypeDefinitionBuilder builder, IUpdateModel updateModel) {
            var model = new TypeIndexing();
            updateModel.TryUpdateModel(model, "TypeIndexing", null, null);
            builder.WithSetting("TypeIndexing.Included", model.Included ? true.ToString() : null);
            yield return DefinitionTemplate(model);
        }

        public override IEnumerable<TemplateViewModel> PartFieldEditor(ContentPartFieldDefinition definition) {
            var model = definition.Settings.GetModel<FieldIndexing>();
            yield return DefinitionTemplate(model);
        }

        public override IEnumerable<TemplateViewModel> PartFieldEditorUpdate(ContentPartFieldDefinitionBuilder builder, IUpdateModel updateModel) {
            var model = new FieldIndexing();
            updateModel.TryUpdateModel(model, "FieldIndexing", null, null);
            builder.WithSetting("FieldIndexing.Included", model.Included ? true.ToString() : null);
            yield return DefinitionTemplate(model);
        }
    }
}