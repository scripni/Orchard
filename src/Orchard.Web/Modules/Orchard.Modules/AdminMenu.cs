﻿using Orchard.Localization;
using Orchard.Security;
using Orchard.UI.Navigation;

namespace Orchard.Modules {
    public class AdminMenu : INavigationProvider {
        public Localizer T { get; set; }

        public string MenuName { get { return "admin"; } }

        public void GetNavigation(NavigationBuilder builder) {
            builder.Add(T("Modules"), "20", menu => menu
                .Add(T("Features"), "0", item => item.Action("Features", "Admin", new { area = "Orchard.Modules" })
                    .Permission(Permissions.ManageFeatures).LocalNav())
                .Add(T("Installed"), "1", item => item.Action("Index", "Admin", new { area = "Orchard.Modules" })
                    .Permission(StandardPermissions.SiteOwner).LocalNav()));
        }
    }
}