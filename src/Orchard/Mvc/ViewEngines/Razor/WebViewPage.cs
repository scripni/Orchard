﻿using System;
using System.Web;
using System.Web.Mvc;
using System.Web.WebPages;
using Autofac;
using Orchard.DisplayManagement;
using Orchard.Localization;
using Orchard.Mvc.Html;
using Orchard.Mvc.Spooling;
using Orchard.Security;
using Orchard.Security.Permissions;
using Orchard.UI.Resources;

namespace Orchard.Mvc.ViewEngines.Razor {

    public abstract class WebViewPage<TModel> : System.Web.Mvc.WebViewPage<TModel>, IOrchardViewPage {
        private ScriptRegister _scriptRegister;
        private ResourceRegister _stylesheetRegister;
        private object _display;
        private object _new;
        private Localizer _localizer = NullLocalizer.Instance;
        private WorkContext _workContext;

        public Localizer T { get { return _localizer; } }
        public dynamic Display { get { return _display; } }
        public WorkContext WorkContext { get { return _workContext; } }

        public dynamic New { get { return _new; } }
        public IDisplayHelperFactory DisplayHelperFactory { get; set; }
        public IShapeHelperFactory ShapeHelperFactory { get; set; }

        public IAuthorizer Authorizer { get; set; }

        public ScriptRegister Script {
            get {
                return _scriptRegister ??
                    (_scriptRegister = new WebViewScriptRegister(this, Html.ViewDataContainer, Html.Resolve<IResourceManager>()));
            }
        }

        public ResourceRegister Style {
            get {
                return _stylesheetRegister ??
                    (_stylesheetRegister = new ResourceRegister(Html.ViewDataContainer, Html.Resolve<IResourceManager>(), "stylesheet"));
            }
        }

        public virtual void RegisterLink(LinkEntry link) {
            Html.Resolve<IResourceManager>().RegisterLink(link);
        }

        public void SetMeta(string name, string content) {
            SetMeta(new MetaEntry { Name = name, Content = content });
        }

        public virtual void SetMeta(MetaEntry meta) {
            Html.Resolve<IResourceManager>().SetMeta(meta);
        }

        public void AppendMeta(string name, string content, string contentSeparator) {
            AppendMeta(new MetaEntry { Name = name, Content = content }, contentSeparator);
        }

        public virtual void AppendMeta(MetaEntry meta, string contentSeparator) {
            Html.Resolve<IResourceManager>().AppendMeta(meta, contentSeparator);
        }

        public override void InitHelpers() {
            base.InitHelpers();

            _workContext = ViewContext.GetWorkContext();
            _workContext.Resolve<IComponentContext>().InjectUnsetProperties(this);

            _localizer = LocalizationUtilities.Resolve(ViewContext, VirtualPath);
            _display = DisplayHelperFactory.CreateHelper(ViewContext, this);
            _new = ShapeHelperFactory.CreateHelper();
        }

        public bool AuthorizedFor(Permission permission) {
            return Authorizer.Authorize(permission);
        }

        public IHtmlString DisplayChildren(dynamic shape) {
            var writer = new HtmlStringWriter();
            foreach (var item in shape) {
                writer.Write(Display(item));
            }
            return writer;
        }

        public IDisposable Capture(Action<IHtmlString> callback) {
            return new CaptureScope(this, callback);
        }

        class CaptureScope : IDisposable {
            readonly WebPageBase _viewPage;
            readonly Action<IHtmlString> _callback;

            public CaptureScope(WebPageBase viewPage, Action<IHtmlString> callback) {
                _viewPage = viewPage;
                _callback = callback;
                _viewPage.OutputStack.Push(new HtmlStringWriter());
            }

            void IDisposable.Dispose() {
                var writer = (HtmlStringWriter)_viewPage.OutputStack.Pop();
                _callback(writer);
            }
        }

        class WebViewScriptRegister : ScriptRegister {
            private readonly WebPageBase _viewPage;

            public WebViewScriptRegister(WebPageBase viewPage, IViewDataContainer container, IResourceManager resourceManager)
                : base(container, resourceManager) {
                _viewPage = viewPage;
            }

            public override IDisposable Head() {
                return new CaptureScope(_viewPage, s => ResourceManager.RegisterHeadScript(s.ToString()));
            }

            public override IDisposable Foot() {
                return new CaptureScope(_viewPage, s => ResourceManager.RegisterFootScript(s.ToString()));
            }
        }
    }

    public abstract class WebViewPage : WebViewPage<dynamic> {
    }
}
