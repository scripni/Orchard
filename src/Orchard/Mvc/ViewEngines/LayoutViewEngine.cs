﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;
using Orchard.Mvc.ViewModels;

namespace Orchard.Mvc.ViewEngines {
    public class LayoutViewEngine : IViewEngine {
        private readonly ViewEngineCollection _viewEngines;

        public LayoutViewEngine(ViewEngineCollection viewEngines) {
            _viewEngines = viewEngines;
        }

        public ViewEngineResult FindPartialView(ControllerContext controllerContext, string partialViewName, bool useCache) {
            return new ViewEngineResult(Enumerable.Empty<string>());
        }

        public ViewEngineResult FindView(
            ControllerContext controllerContext,
            string viewName,
            string masterName,
            bool useCache) {

            var skipLayoutViewEngine = false;
            if (string.IsNullOrEmpty(masterName) == false)
                skipLayoutViewEngine = true;
            if (!(controllerContext.Controller.ViewData.Model is BaseViewModel))
                skipLayoutViewEngine = true;
            if (_viewEngines == null || _viewEngines.Count == 0)
                skipLayoutViewEngine = true;
            if (skipLayoutViewEngine)
                return new ViewEngineResult(Enumerable.Empty<string>());


            var bodyView = _viewEngines.FindPartialView(controllerContext, viewName);
            var layoutView = _viewEngines.FindPartialView(controllerContext, "layout");
            var documentView = _viewEngines.FindPartialView(controllerContext, "document");

            if (bodyView.View == null ||
                layoutView.View == null ||
                documentView.View == null) {

                var missingTemplatesResult = new ViewEngineResult(
                    (bodyView.SearchedLocations ?? Enumerable.Empty<string>())
                        .Concat((layoutView.SearchedLocations ?? Enumerable.Empty<string>()))
                        .Concat((documentView.SearchedLocations ?? Enumerable.Empty<string>()))
                    );

                return missingTemplatesResult;
            }

            var view = new LayoutView(this, new[] {
                                                bodyView,
                                                layoutView,
                                                documentView,
                                            });

            return new ViewEngineResult(view, this);
        }

        public void ReleaseView(ControllerContext controllerContext, IView view) {
            var layoutView = (LayoutView)view;
            layoutView.ReleaseViews(controllerContext);
        }

        public IDisposable CreateScope(ViewContext context) {
            return new Scope(context) { LayoutViewEngine = this };
        }

        class Scope : IDisposable {
            private readonly ControllerContext _context;
            private readonly Scope _prior;

            public Scope(ControllerContext context) {
                _context = context;
                _prior = From(context);
                context.HttpContext.Items[typeof(Scope)] = this;
            }

            public LayoutViewEngine LayoutViewEngine { get; set; }

            public void Dispose() {
                _context.HttpContext.Items[typeof(Scope)] = _prior;
            }

            public static Scope From(ControllerContext context) {
                return (Scope)context.HttpContext.Items[typeof(Scope)];
            }
        }

        public static IViewEngine CreateShim() {
            return new Shim();
        }

        class Shim : IViewEngine {
            public ViewEngineResult FindPartialView(ControllerContext controllerContext, string partialViewName, bool useCache) {
                var scope = Scope.From(controllerContext);
                if (scope != null && scope.LayoutViewEngine != null) {
                    var result = scope.LayoutViewEngine._viewEngines.FindPartialView(controllerContext, partialViewName);
                    Monitor(result, partialViewName);
                    return result;
                }

                return new ViewEngineResult(Enumerable.Empty<string>());
            }


            public ViewEngineResult FindView(ControllerContext controllerContext, string viewName, string masterName, bool useCache) {
                var scope = Scope.From(controllerContext);
                if (scope != null && scope.LayoutViewEngine != null) {
                    var result = scope.LayoutViewEngine._viewEngines.FindView(controllerContext, viewName, masterName);
                    Monitor(result, viewName);
                    return result;
                }

                return new ViewEngineResult(Enumerable.Empty<string>());
            }


            private static void Monitor(ViewEngineResult result, string viewName) {
                if (result.View == null) {
                    Trace.WriteLine("Unable to find " + viewName);
                //    foreach (var search in result.SearchedLocations) {
                //        Trace.WriteLine("  location " + search);
                //    }
                }
            }

            public void ReleaseView(ControllerContext controllerContext, IView view) {
                throw new NotImplementedException();
            }
        }

    }


}
