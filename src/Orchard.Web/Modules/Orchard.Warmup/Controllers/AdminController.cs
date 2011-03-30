using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using Orchard.ContentManagement;
using Orchard.Core.Contents.Controllers;
using Orchard.Environment.Warmup;
using Orchard.FileSystems.AppData;
using Orchard.Localization;
using Orchard.Security;
using Orchard.Warmup.Models;
using Orchard.UI.Notify;
using Orchard.Warmup.Services;

namespace Orchard.Warmup.Controllers {
    public  class AdminController : Controller, IUpdateModel {
        private readonly IWarmupScheduler _warmupScheduler;
        private readonly IAppDataFolder _appDataFolder;

        public AdminController(
            IOrchardServices services, 
            IWarmupScheduler warmupScheduler,
            IAppDataFolder appDataFolder) {
            _warmupScheduler = warmupScheduler;
            _appDataFolder = appDataFolder;
            Services = services;

            T = NullLocalizer.Instance;
        }

        public IOrchardServices Services { get; set; }
        public Localizer T { get; set; }

        public ActionResult Index() {
            if (!Services.Authorizer.Authorize(StandardPermissions.SiteOwner, T("Not authorized to manage settings")))
                return new HttpUnauthorizedResult();

            var warmupPart = Services.WorkContext.CurrentSite.As<WarmupSettingsPart>();
            return View(warmupPart);
        }

        [FormValueRequired("submit")]
        [HttpPost, ActionName("Index")]
        public ActionResult IndexPost() {
            if (!Services.Authorizer.Authorize(StandardPermissions.SiteOwner, T("Not authorized to manage settings")))
                return new HttpUnauthorizedResult();

            var warmupPart = Services.WorkContext.CurrentSite.As<WarmupSettingsPart>();

            if(TryUpdateModel(warmupPart)) {
                using (var urlReader = new StringReader(warmupPart.Urls)) {
                    string relativeUrl;
                    while (null != (relativeUrl = urlReader.ReadLine())) {
                        if(!Uri.IsWellFormedUriString(relativeUrl, UriKind.Relative) || !(relativeUrl.StartsWith("/"))) {
                            AddModelError("Urls", T("{0} is an invalid warmup url.", relativeUrl));
                        }
                    }
                }
            }

            if (warmupPart.Scheduled) {
                if (warmupPart.Delay <= 0) {
                    AddModelError("Delay", T("Delay must be greater than zero."));
                }
            }

            Services.Notifier.Information(T("Warmup updated successfully."));
            return View(warmupPart);
        }

        [FormValueRequired("submit.Generate")]
        [HttpPost, ActionName("Index")]
        public ActionResult IndexPostGenerate() {
            var result = IndexPost();
            
            if (ModelState.IsValid) {
                _warmupScheduler.Schedule(true);
                Services.Notifier.Information(T("Static pages are currently being generated."));
            }

            return result;
        }

        bool IUpdateModel.TryUpdateModel<TModel>(TModel model, string prefix, string[] includeProperties, string[] excludeProperties) {
            return TryUpdateModel(model, prefix, includeProperties, excludeProperties);
        }

        void IUpdateModel.AddModelError(string key, LocalizedString errorMessage) {
            ModelState.AddModelError(key, errorMessage.ToString());
        }

        public void AddModelError(string key, LocalizedString errorMessage) {
            ModelState.AddModelError(key, errorMessage.ToString());
        }
    }
}