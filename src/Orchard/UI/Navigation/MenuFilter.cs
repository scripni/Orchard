﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using Orchard.DisplayManagement;
using Orchard.Mvc.Filters;
using Orchard.UI.Admin;

namespace Orchard.UI.Navigation {
    public class MenuFilter : FilterProvider, IResultFilter {
        private readonly INavigationManager _navigationManager;
        private readonly IWorkContextAccessor _workContextAccessor;
        private readonly dynamic _shapeFactory;

        public MenuFilter(INavigationManager navigationManager,
            IWorkContextAccessor workContextAccessor,
            IShapeFactory shapeFactory) {

            _navigationManager = navigationManager;
            _workContextAccessor = workContextAccessor;
            _shapeFactory = shapeFactory;
        }

        public void OnResultExecuting(ResultExecutingContext filterContext) {
            // should only run on a full view rendering result
            if (!(filterContext.Result is ViewResult)) {
                return;
            }

            WorkContext workContext = _workContextAccessor.GetContext(filterContext);

            string menuName = "main";
            if (AdminFilter.IsApplied(filterContext.RequestContext)) {
                menuName = "admin";
            }

            IEnumerable<MenuItem> menuItems = _navigationManager.BuildMenu(menuName);

            // Set the currently selected path
            Stack<MenuItem> selectedPath = SetSelectedPath(menuItems, filterContext.RouteData);

            // Populate main nav
            dynamic menuShape = _shapeFactory.Menu().MenuName(menuName);
            PopulateMenu(_shapeFactory, menuShape, menuShape, menuItems);
            workContext.Layout.Navigation.Add(menuShape);

            // Populate local nav
            dynamic localMenuShape = _shapeFactory.LocalMenu().MenuName(string.Format("local_{0}", menuName));
            PopulateLocalMenu(_shapeFactory, localMenuShape, localMenuShape, selectedPath);
            workContext.Layout.LocalNavigation.Add(localMenuShape);
        }

        /// <summary>
        /// Populates the menu shapes.
        /// </summary>
        /// <param name="shapeFactory">The shape factory.</param>
        /// <param name="parentShape">The menu parent shape.</param>
        /// <param name="menu">The menu shape.</param>
        /// <param name="menuItems">The current level to populate.</param>
        protected void PopulateMenu(dynamic shapeFactory, dynamic parentShape, dynamic menu, IEnumerable<MenuItem> menuItems) {
            foreach (MenuItem menuItem in menuItems) {
                dynamic menuItemShape = BuildMenuItemShape(shapeFactory, parentShape, menu, menuItem);

                if (menuItem.Items != null && menuItem.Items.Any()) {
                    PopulateMenu(shapeFactory, menuItemShape, menu, menuItem.Items);
                }

                parentShape.Add(menuItemShape, menuItem.Position);
            }
        }

        /// <summary>
        /// Populates the local menu starting from the first non local task parent.
        /// </summary>
        /// <param name="shapeFactory">The shape factory.</param>
        /// <param name="parentShape">The menu parent shape.</param>
        /// <param name="menu">The menu shape.</param>
        /// <param name="selectedPath">The selection path.</param>
        protected void PopulateLocalMenu(dynamic shapeFactory, dynamic parentShape, dynamic menu, Stack<MenuItem> selectedPath) {
            MenuItem parentMenuItem = FindParentLocalTask(selectedPath);

            // find childs tabs and expand them
            if (parentMenuItem != null && parentMenuItem.Items != null && parentMenuItem.Items.Any()) {
                PopulateLocalMenu(shapeFactory, parentShape, menu, parentMenuItem.Items);
            }
        }

        /// <summary>
        /// Populates the local menu shapes.
        /// </summary>
        /// <param name="shapeFactory">The shape factory.</param>
        /// <param name="parentShape">The menu parent shape.</param>
        /// <param name="menu">The menu shape.</param>
        /// <param name="menuItems">The current level to populate.</param>
        protected void PopulateLocalMenu(dynamic shapeFactory, dynamic parentShape, dynamic menu, IEnumerable<MenuItem> menuItems) {
            foreach (MenuItem menuItem in menuItems) {
                dynamic menuItemShape = BuildLocalMenuItemShape(shapeFactory, parentShape, menu, menuItem);

                if (menuItem.Items != null && menuItem.Items.Any()) {
                    PopulateLocalMenu(shapeFactory, menuItemShape, menu, menuItem.Items);
                }

                parentShape.Add(menuItemShape, menuItem.Position);
            }
        }

        /// <summary>
        /// Identifies the currently selected path, starting from the selected node.
        /// </summary>
        /// <param name="menuItems">All the menuitems in the navigation menu.</param>
        /// <param name="currentRouteData">The current route data.</param>
        /// <returns>A stack with the selection path being the last node the currently selected one.</returns>
        protected static Stack<MenuItem> SetSelectedPath(IEnumerable<MenuItem> menuItems, RouteData currentRouteData) {
            foreach(MenuItem menuItem in menuItems) {
                if (RouteMatches(menuItem, currentRouteData)) {
                    menuItem.Selected = true;

                    Stack<MenuItem> selectedPath = new Stack<MenuItem>();
                    selectedPath.Push(menuItem);
                    return selectedPath;
                }

                if (menuItem.Items != null && menuItem.Items.Any()) {
                    Stack<MenuItem> selectedPath = SetSelectedPath(menuItem.Items, currentRouteData);
                    if (selectedPath != null) {
                        menuItem.Selected = true;
                        selectedPath.Push(menuItem);
                        return selectedPath;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Find the first level in the selection path, starting from the bottom, that is not a local task.
        /// </summary>
        /// <param name="selectedPath">The selection path stack. The bottom node is the currently selected one.</param>
        /// <returns>The first node, starting from the bottom, that is not a local task. Otherwise, null.</returns>
        protected static MenuItem FindParentLocalTask(Stack<MenuItem> selectedPath) {
            if (selectedPath != null) {
                MenuItem parentMenuItem = selectedPath.Pop();
                if (parentMenuItem != null) {
                    while (selectedPath.Count > 0) {
                        MenuItem currentMenuItem = selectedPath.Pop();
                        if (currentMenuItem.LocalNav) {
                            return parentMenuItem;
                        }

                        parentMenuItem = currentMenuItem;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Determines if a menu item corresponds to a given route.
        /// </summary>
        /// <param name="menuItem">The menu item.</param>
        /// <param name="currentRouteData">The route data.</param>
        /// <returns>True if the menu item's action corresponds to the route data; false otherwise.</returns>
        protected static bool RouteMatches(MenuItem menuItem, RouteData currentRouteData) {
            return menuItem.RouteValues != null &&
                   string.Equals((string) menuItem.RouteValues["area"], (string) currentRouteData.Values["area"], StringComparison.OrdinalIgnoreCase) &&
                   string.Equals((string) menuItem.RouteValues["controller"], (string) currentRouteData.Values["controller"], StringComparison.OrdinalIgnoreCase) &&
                   string.Equals((string) menuItem.RouteValues["action"], (string) currentRouteData.Values["action"], StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Builds a menu item shape.
        /// </summary>
        /// <param name="shapeFactory">The shape factory.</param>
        /// <param name="parentShape">The parent shape.</param>
        /// <param name="menu">The menu shape.</param>
        /// <param name="menuItem">The menu item to build the shape for.</param>
        /// <returns>The menu item shape.</returns>
        protected dynamic BuildMenuItemShape(dynamic shapeFactory, dynamic parentShape, dynamic menu, MenuItem menuItem) {
            return shapeFactory.MenuItem()
                .Text(menuItem.Text)
                .Href(menuItem.Href)
                .LinkToFirstChild(menuItem.LinkToFirstChild)
                .LocalNav(menuItem.LocalNav)
                .Selected(menuItem.Selected)
                .RouteValues(menuItem.RouteValues)
                .Item(menuItem)
                .Menu(menu)
                .Parent(parentShape);
        }

        /// <summary>
        /// Builds a local menu item shape.
        /// </summary>
        /// <param name="shapeFactory">The shape factory.</param>
        /// <param name="parentShape">The parent shape.</param>
        /// <param name="menu">The menu shape.</param>
        /// <param name="menuItem">The menu item to build the shape for.</param>
        /// <returns>The menu item shape.</returns>
        protected dynamic BuildLocalMenuItemShape(dynamic shapeFactory, dynamic parentShape, dynamic menu, MenuItem menuItem) {
            return shapeFactory.LocalMenuItem()
                .Text(menuItem.Text)
                .Href(menuItem.Href)
                .LinkToFirstChild(menuItem.LinkToFirstChild)
                .LocalNav(menuItem.LocalNav)
                .Selected(menuItem.Selected)
                .RouteValues(menuItem.RouteValues)
                .Item(menuItem)
                .Menu(menu)
                .Parent(parentShape);
        }

        public void OnResultExecuted(ResultExecutedContext filterContext) { }
    }
}