﻿using System.Web.Mvc;
using Orchard.ContentManagement;
using Orchard.DisplayManagement;
using Orchard.Localization;
using Orchard.Themes;

namespace Orchard.Core.Contents.Controllers {
    [Themed]
    public class ItemController : Controller {
        private readonly IContentManager _contentManager;

        public ItemController(IContentManager contentManager, IShapeFactory shapeFactory, IOrchardServices services) {
            _contentManager = contentManager;
            Shape = shapeFactory;
            Services = services;
            T = NullLocalizer.Instance;
        }

        dynamic Shape { get; set; }
        public IOrchardServices Services { get; private set; }
        public Localizer T { get; set; }
 
        // /Contents/Item/Display/72
        public ActionResult Display(int id) {
            var contentItem = _contentManager.Get(id, VersionOptions.Published);
            dynamic model = _contentManager.BuildDisplay(contentItem);
            // Casting to avoid invalid (under medium trust) reflection over the protected View method and force a static invocation.
            return View((object)model);
        }

        // /Contents/Item/Preview/72
        // /Contents/Item/Preview/72?version=5
        public ActionResult Preview(int id, int? version) {
            var versionOptions = VersionOptions.Latest;
            if (version != null)
                versionOptions = VersionOptions.Number((int)version);

            var contentItem = _contentManager.Get(id, versionOptions);

            if (!Services.Authorizer.Authorize(Permissions.EditContent, contentItem, T("Cannot edit content")))
                return new HttpUnauthorizedResult();

            dynamic model = _contentManager.BuildDisplay(contentItem);
            // Casting to avoid invalid (under medium trust) reflection over the protected View method and force a static invocation.
            return View("Display", (object)model);
        }
    }
}