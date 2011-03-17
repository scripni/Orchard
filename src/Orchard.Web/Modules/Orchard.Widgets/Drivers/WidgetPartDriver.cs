﻿using System.Collections.Generic;
using JetBrains.Annotations;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Drivers;
using Orchard.Widgets.Models;
using Orchard.Widgets.Services;

namespace Orchard.Widgets.Drivers {

    [UsedImplicitly]
    public class WidgetPartDriver : ContentPartDriver<WidgetPart> {
        private readonly IWidgetsService _widgetsService;

        public WidgetPartDriver(IWidgetsService widgetsService) {
            _widgetsService = widgetsService;
        }

        protected override DriverResult Editor(WidgetPart widgetPart, dynamic shapeHelper) {
            widgetPart.AvailableZones = _widgetsService.GetZones();
            widgetPart.AvailableLayers = _widgetsService.GetLayers();

            var results = new List<DriverResult> {
                ContentShape("Parts_Widgets_WidgetPart",
                             () => shapeHelper.EditorTemplate(TemplateName: "Parts.Widgets.WidgetPart", Model: widgetPart, Prefix: Prefix))
            };

            if (widgetPart.Id > 0)
                results.Add(ContentShape("Widget_DeleteButton",
                    deleteButton => deleteButton));

            return Combined(results.ToArray());
        }

        protected override DriverResult Editor(WidgetPart widgetPart, IUpdateModel updater, dynamic shapeHelper) {
            updater.TryUpdateModel(widgetPart, Prefix, null, null);
            return Editor(widgetPart, shapeHelper);
        }

        protected override void Importing(WidgetPart part, ContentManagement.Handlers.ImportContentContext context) {
            var title = context.Attribute(part.PartDefinition.Name, "Title");
            if (title != null) {
                part.Title = title;
            }

            var position = context.Attribute(part.PartDefinition.Name, "Position");
            if (position != null) {
                part.Position = position;
            }

            var zone = context.Attribute(part.PartDefinition.Name, "Zone");
            if (zone != null) {
                part.Zone = zone;
            }
        }

        protected override void Exporting(WidgetPart part, ContentManagement.Handlers.ExportContentContext context) {
            context.Element(part.PartDefinition.Name).SetAttributeValue("Title", part.Title);
            context.Element(part.PartDefinition.Name).SetAttributeValue("Position", part.Position);
            context.Element(part.PartDefinition.Name).SetAttributeValue("Zone", part.Zone);
        }
    }
}