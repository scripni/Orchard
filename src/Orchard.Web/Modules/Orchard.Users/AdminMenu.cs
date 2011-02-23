﻿using Orchard.Localization;
using Orchard.Security;
using Orchard.UI.Navigation;

namespace Orchard.Users {
    public class AdminMenu : INavigationProvider {
        public Localizer T { get; set; }
        public string MenuName { get { return "admin"; } }

        public void GetNavigation(NavigationBuilder builder) {
            builder.AddImageSet("users")
                .Add(T("Users"), "40",
                    menu => menu.Add(T("Users"), "1.0", item => item.Action("Index", "Admin", new { area = "Orchard.Users" })
                        .LocalNav().Default().Permission(StandardPermissions.SiteOwner)));
        }
    }
}
