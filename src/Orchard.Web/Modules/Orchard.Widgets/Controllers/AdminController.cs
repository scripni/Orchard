﻿using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Linq;
using Orchard.ContentManagement;
using Orchard.Core.Contents.Controllers;
using Orchard.DisplayManagement;
using Orchard.Environment.Extensions.Models;
using Orchard.FileSystems.VirtualPath;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.Mvc.Extensions;
using Orchard.Themes.Services;
using Orchard.UI.Admin;
using Orchard.UI.Notify;
using Orchard.Utility.Extensions;
using Orchard.Widgets.Models;
using Orchard.Widgets.Services;

namespace Orchard.Widgets.Controllers {

    [ValidateInput(false), Admin]
    public class AdminController : Controller, IUpdateModel {

        private const string NotAuthorizedManageWidgetsLabel = "Not authorized to manage widgets";

        private readonly IWidgetsService _widgetsService;
        private readonly ISiteThemeService _siteThemeService;
        private readonly IVirtualPathProvider _virtualPathProvider;

        public AdminController(
            IOrchardServices services,
            IWidgetsService widgetsService,
            IShapeFactory shapeFactory,
            ISiteThemeService siteThemeService,
            IVirtualPathProvider virtualPathProvider) {

            Services = services;
            _widgetsService = widgetsService;
            _siteThemeService = siteThemeService;
            _virtualPathProvider = virtualPathProvider;

            T = NullLocalizer.Instance;
            Logger = NullLogger.Instance;
            Shape = shapeFactory;
        }

        private IOrchardServices Services { get; set; }
        public Localizer T { get; set; }
        public ILogger Logger { get; set; }
        dynamic Shape { get; set; }

        public ActionResult Index(int? layerId) {
            IEnumerable<LayerPart> layers = _widgetsService.GetLayers();

            if (layers.Count() == 0) {
                Services.Notifier.Error(T("There are no widget layers defined. A layer will need to be added in order to add widgets to any part of the site."));
            }

            LayerPart currentLayer = layerId == null
                ? layers.FirstOrDefault()
                : layers.FirstOrDefault(layer => layer.Id == layerId);

            if (currentLayer == null && layerId != null) { // Incorrect layer id passed
                Services.Notifier.Error(T("Layer not found: {0}", layerId));
                return RedirectToAction("Index");
            }

            ExtensionDescriptor currentTheme = _siteThemeService.GetSiteTheme();
            IEnumerable<string> allZones = _widgetsService.GetZones();
            IEnumerable<string> currentThemesZones = _widgetsService.GetZones(currentTheme);

            string zonePreviewImagePath = string.Format("{0}/{1}/ThemeZonePreview.png", currentTheme.Location, currentTheme.Id);
            string zonePreviewImage = _virtualPathProvider.FileExists(zonePreviewImagePath) ? zonePreviewImagePath : null;

            dynamic viewModel = Shape.ViewModel()
                .CurrentTheme(currentTheme)
                .CurrentLayer(currentLayer)
                .Layers(layers)
                .Widgets(_widgetsService.GetWidgets())
                .Zones(currentThemesZones)
                .OrphanZones(allZones.Except(currentThemesZones))
                .OrphanWidgets(_widgetsService.GetOrphanedWidgets())
                .ZonePreviewImage(zonePreviewImage);

            // Casting to avoid invalid (under medium trust) reflection over the protected View method and force a static invocation.
            return View((object)viewModel);
        }

        [HttpPost, ActionName("Index")]
        public ActionResult IndexWidgetPOST(int? layerId, int? moveUp, int? moveDown, int? moveHere, int? moveOut, string returnUrl) {
            if (moveOut.HasValue)
                return DeleteWidget(moveOut.Value, returnUrl);

            if (!Services.Authorizer.Authorize(Permissions.ManageWidgets, T(NotAuthorizedManageWidgetsLabel)))
                return new HttpUnauthorizedResult();

            try {
                if (moveUp.HasValue)
                    _widgetsService.MoveWidgetUp(moveUp.Value);
                if (moveDown.HasValue)
                    _widgetsService.MoveWidgetDown(moveDown.Value);
                if (moveHere.HasValue)
                    _widgetsService.MoveWidgetToLayer(moveHere.Value, layerId);
            }
            catch (Exception exception) {
                this.Error(exception, T("Moving widget failed: {0}", exception.Message), Logger, Services.Notifier);
            }

            return this.RedirectLocal(returnUrl, () => RedirectToAction("Index"));
        }


        public ActionResult ChooseWidget(int layerId, string zone, string returnUrl) {
            if (!Services.Authorizer.Authorize(Permissions.ManageWidgets, T(NotAuthorizedManageWidgetsLabel)))
                return new HttpUnauthorizedResult();

            if (string.IsNullOrWhiteSpace(zone)) {
                Services.Notifier.Error(T("Need a zone specified for widget placement."));
                return RedirectToAction("Index");
            }

            IEnumerable<LayerPart> layers = _widgetsService.GetLayers();

            if (layers.Count() == 0) {
                Services.Notifier.Error(T("Layer not found: {0}", layerId));
                return RedirectToAction("Index");
            }

            LayerPart currentLayer = layers.FirstOrDefault(layer => layer.Id == layerId);
            if (currentLayer == null) { // Incorrect layer id passed
                Services.Notifier.Error(T("Layer not found: {0}", layerId));
                return RedirectToAction("Index");
            }

            dynamic viewModel = Shape.ViewModel()
                .CurrentLayer(currentLayer)
                .Zone(zone)
                .WidgetTypes(_widgetsService.GetWidgetTypes())
                .ReturnUrl(returnUrl);

            // Casting to avoid invalid (under medium trust) reflection over the protected View method and force a static invocation.
            return View((object)viewModel);
        }

        public ActionResult AddWidget(int layerId, string widgetType, string zone, string returnUrl) {
            if (!Services.Authorizer.Authorize(Permissions.ManageWidgets, T(NotAuthorizedManageWidgetsLabel)))
                return new HttpUnauthorizedResult();

            try {
                WidgetPart widgetPart = Services.ContentManager.New<WidgetPart>(widgetType);
                if (widgetPart == null)
                    return HttpNotFound();

                int widgetPosition = _widgetsService.GetWidgets().Where(widget => widget.Zone == widgetPart.Zone).Count() + 1;
                widgetPart.Position = widgetPosition.ToString();
                widgetPart.Zone = zone;
                widgetPart.LayerPart = _widgetsService.GetLayer(layerId);
                dynamic model = Services.ContentManager.BuildEditor(widgetPart);
                // Casting to avoid invalid (under medium trust) reflection over the protected View method and force a static invocation.
                return View((object)model);
            }
            catch (Exception exception) {
                this.Error(exception, T("Creating widget failed: {0}", exception.Message), Logger, Services.Notifier);
                return this.RedirectLocal(returnUrl, () => RedirectToAction("Index"));
            }
        }

        [HttpPost, ActionName("AddWidget")]
        public ActionResult AddWidgetPOST(int layerId, string widgetType, string returnUrl) {
            if (!Services.Authorizer.Authorize(Permissions.ManageWidgets, T(NotAuthorizedManageWidgetsLabel)))
                return new HttpUnauthorizedResult();

            try {
                WidgetPart widgetPart = _widgetsService.CreateWidget(layerId, widgetType, "", "", "");
                if (widgetPart == null)
                    return HttpNotFound();

                var model = Services.ContentManager.UpdateEditor(widgetPart, this);
                if (!ModelState.IsValid) {
                    Services.TransactionManager.Cancel();
                    // Casting to avoid invalid (under medium trust) reflection over the protected View method and force a static invocation.
                    return View((object)model);
                }

                Services.Notifier.Information(T("Your {0} has been added.", widgetPart.TypeDefinition.DisplayName));
            } catch (Exception exception) {
                this.Error(exception, T("Creating widget failed: {0}", exception.Message), Logger, Services.Notifier);
            }

            return this.RedirectLocal(returnUrl, () => RedirectToAction("Index"));
        }

        public ActionResult AddLayer(string name, string description, string layerRule) { // <- hints for a new layer
            if (!Services.Authorizer.Authorize(Permissions.ManageWidgets, T(NotAuthorizedManageWidgetsLabel)))
                return new HttpUnauthorizedResult();

            try {
                LayerPart layerPart = Services.ContentManager.New<LayerPart>("Layer");
                if (layerPart == null)
                    return HttpNotFound();

                dynamic model = Services.ContentManager.BuildEditor(layerPart);

                // only messing with the hints if they're given
                if (!string.IsNullOrWhiteSpace(name))
                    model.Name = name;
                if (!string.IsNullOrWhiteSpace(description))
                    model.Description = description;
                if (!string.IsNullOrWhiteSpace(layerRule))
                    model.LayerRule = layerRule;

                // Casting to avoid invalid (under medium trust) reflection over the protected View method and force a static invocation.
                return View((object)model);
            } catch (Exception exception) {
                this.Error(exception, T("Creating layer failed: {0}", exception.Message), Logger, Services.Notifier);
                return RedirectToAction("Index");
            }
        }

        [HttpPost, ActionName("AddLayer")]
        public ActionResult AddLayerPOST() {
            if (!Services.Authorizer.Authorize(Permissions.ManageWidgets, T(NotAuthorizedManageWidgetsLabel)))
                return new HttpUnauthorizedResult();

            try {
                LayerPart layerPart = _widgetsService.CreateLayer("", "", "");
                if (layerPart == null)
                    return HttpNotFound();

                var model = Services.ContentManager.UpdateEditor(layerPart, this);

                if (!ModelState.IsValid) {
                    Services.TransactionManager.Cancel();
                    // Casting to avoid invalid (under medium trust) reflection over the protected View method and force a static invocation.
                    return View((object)model);
                }

                Services.Notifier.Information(T("Your {0} has been created.", layerPart.TypeDefinition.DisplayName));
                return RedirectToAction("Index", "Admin", new { layerId = layerPart.Id });
            } catch (Exception exception) {
                this.Error(exception, T("Creating layer failed: {0}", exception.Message), Logger, Services.Notifier);
                return RedirectToAction("Index");
            }
        }

        public ActionResult EditLayer(int id) {
            if (!Services.Authorizer.Authorize(Permissions.ManageWidgets, T(NotAuthorizedManageWidgetsLabel)))
                return new HttpUnauthorizedResult();

            try {
                LayerPart layerPart = _widgetsService.GetLayer(id);
                if (layerPart == null)
                    return HttpNotFound();

                dynamic model = Services.ContentManager.BuildEditor(layerPart);
                // Casting to avoid invalid (under medium trust) reflection over the protected View method and force a static invocation.
                return View((object)model);
            } catch (Exception exception) {
                this.Error(exception, T("Editing layer failed: {0}", exception.Message), Logger, Services.Notifier);

                return RedirectToAction("Index", "Admin", new { id });
            }
        }

        [HttpPost, ActionName("EditLayer")]
        [FormValueRequired("submit.Save")]
        public ActionResult EditLayerSavePOST(int id) {
            if (!Services.Authorizer.Authorize(Permissions.ManageWidgets, T(NotAuthorizedManageWidgetsLabel)))
                return new HttpUnauthorizedResult();

            try {
                LayerPart layerPart = _widgetsService.GetLayer(id);
                if (layerPart == null)
                    return HttpNotFound();

                var model = Services.ContentManager.UpdateEditor(layerPart, this);

                if (!ModelState.IsValid) {
                    Services.TransactionManager.Cancel();
                    // Casting to avoid invalid (under medium trust) reflection over the protected View method and force a static invocation.
                    return View((object)model);
                }

                Services.Notifier.Information(T("Your {0} has been saved.", layerPart.TypeDefinition.DisplayName));
            } catch (Exception exception) {
                this.Error(exception, T("Editing layer failed: {0}", exception.Message), Logger, Services.Notifier);
            }

            return RedirectToAction("Index");
        }

        [HttpPost, ActionName("EditLayer")]
        [FormValueRequired("submit.Delete")]
        public ActionResult EditLayerDeletePOST(int id, string returnUrl) {
            if (!Services.Authorizer.Authorize(Permissions.ManageWidgets, T(NotAuthorizedManageWidgetsLabel)))
                return new HttpUnauthorizedResult();

            try {
                _widgetsService.DeleteLayer(id);
                Services.Notifier.Information(T("Layer was successfully deleted"));
            } catch (Exception exception) {
                this.Error(exception, T("Removing Layer failed: {0}", exception.Message), Logger, Services.Notifier);
            }

            return this.RedirectLocal(returnUrl, () => RedirectToAction("Index"));
        }

        public ActionResult EditWidget(int id) {
            if (!Services.Authorizer.Authorize(Permissions.ManageWidgets, T(NotAuthorizedManageWidgetsLabel)))
                return new HttpUnauthorizedResult();

            WidgetPart widgetPart = null;
            try {
                widgetPart = _widgetsService.GetWidget(id);
                if (widgetPart == null) {
                    Services.Notifier.Error(T("Widget not found: {0}", id));
                    return RedirectToAction("Index");
                }

                dynamic model = Services.ContentManager.BuildEditor(widgetPart);
                // Casting to avoid invalid (under medium trust) reflection over the protected View method and force a static invocation.
                return View((object)model);
            }
            catch (Exception exception) {
                this.Error(exception, T("Editing widget failed: {0}", exception.Message), Logger, Services.Notifier);

                if (widgetPart != null)
                    return RedirectToAction("Index", "Admin", new { id = widgetPart.LayerPart.Id });

                return RedirectToAction("Index");
            }
        }

        [HttpPost, ActionName("EditWidget")]
        [FormValueRequired("submit.Save")]
        public ActionResult EditWidgetSavePOST(int id, int layerId, string returnUrl) {
            if (!Services.Authorizer.Authorize(Permissions.ManageWidgets, T(NotAuthorizedManageWidgetsLabel)))
                return new HttpUnauthorizedResult();

            WidgetPart widgetPart = null;
            try {
                widgetPart = _widgetsService.GetWidget(id);
                if (widgetPart == null)
                    return HttpNotFound();

                widgetPart.LayerPart = _widgetsService.GetLayer(layerId);
                var model = Services.ContentManager.UpdateEditor(widgetPart, this);
                if (!ModelState.IsValid) {
                    Services.TransactionManager.Cancel();
                    // Casting to avoid invalid (under medium trust) reflection over the protected View method and force a static invocation.
                    return View((object)model);
                }

                Services.Notifier.Information(T("Your {0} has been saved.", widgetPart.TypeDefinition.DisplayName));
            } catch (Exception exception) {
                this.Error(exception, T("Editing widget failed: {0}", exception.Message), Logger, Services.Notifier);
            }

            return this.RedirectLocal(returnUrl, () => RedirectToAction("Index"));
        }

        [HttpPost, ActionName("EditWidget")]
        [FormValueRequired("submit.Delete")]
        public ActionResult EditWidgetDeletePOST(int id, string returnUrl) {
            return DeleteWidget(id, returnUrl);
        }
        private ActionResult DeleteWidget(int id, string returnUrl) {
            if (!Services.Authorizer.Authorize(Permissions.ManageWidgets, T(NotAuthorizedManageWidgetsLabel)))
                return new HttpUnauthorizedResult();

            WidgetPart widgetPart = null;
            try {
                widgetPart = _widgetsService.GetWidget(id);
                if (widgetPart == null)
                    return HttpNotFound();

                _widgetsService.DeleteWidget(widgetPart.Id);
                Services.Notifier.Information(T("Widget was successfully deleted"));
            }
            catch (Exception exception) {
                this.Error(exception, T("Removing Widget failed: {0}", exception.Message), Logger, Services.Notifier);
            }

            return this.RedirectLocal(returnUrl, () => RedirectToAction("Index"));
        }

        bool IUpdateModel.TryUpdateModel<TModel>(TModel model, string prefix, string[] includeProperties, string[] excludeProperties) {
            return base.TryUpdateModel(model, prefix, includeProperties, excludeProperties);
        }

        void IUpdateModel.AddModelError(string key, LocalizedString errorMessage) {
            ModelState.AddModelError(key, errorMessage.ToString());
        }
    }
}