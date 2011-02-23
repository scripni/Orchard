﻿using System.Linq;
using Orchard.Environment.Configuration;
using Orchard.Environment.Descriptor;
using Orchard.Environment.Descriptor.Models;
using Orchard.Specs.Hosting.Orchard.Web;
using TechTalk.SpecFlow;

namespace Orchard.Specs.Bindings {
    [Binding]
    public class OrchardSiteFactory : BindingBase {
        [Given(@"I have installed Orchard")]
        public void GivenIHaveInstalledOrchard() {
            GivenIHaveInstalledOrchard("/");
        }

        [Given(@"I have installed Orchard at ""(.*)\""")]
        public void GivenIHaveInstalledOrchard(string virtualDirectory) {
            var webApp = Binding<WebAppHosting>();

            webApp.GivenIHaveACleanSiteWith(
                virtualDirectory,
                TableData(
                new { extension = "Module", names = "Orchard.Setup, Orchard.Pages, Orchard.Blogs, Orchard.Messaging, Orchard.Media, Orchard.Modules, Orchard.Packaging, Orchard.PublishLater, Orchard.Themes, Orchard.Scripting, Orchard.Widgets, Orchard.Users, Orchard.Lists, Orchard.ContentTypes, Orchard.Roles, Orchard.Comments, Orchard.jQuery, Orchard.Tags, TinyMce, Orchard.Packaging, Orchard.Recipes" },
                new { extension = "Core", names = "Common, Containers, Dashboard, Feeds, HomePage, Navigation, Contents, Routable, Scheduling, Settings, Shapes, XmlRpc" },
                new { extension = "Theme", names = "SafeMode, TheAdmin, TheThemeMachine" }));

            webApp.WhenIGoTo("Setup");

            webApp.WhenIFillIn(TableData(
                new { name = "SiteName", value = "My Site" },
                new { name = "AdminPassword", value = "6655321" },
                new { name = "ConfirmPassword", value = "6655321" }));

            webApp.WhenIHit("Finish Setup");
        }

        [Given(@"I have installed ""(.*)\""")]
        public void GivenIHaveInstalled(string name) {
            var webApp = Binding<WebAppHosting>();
            webApp.GivenIHaveModule(name);
            webApp.Host.Execute(MvcApplication.ReloadExtensions);

            GivenIHaveEnabled(name);
        }

        [Given(@"I have enabled ""(.*)\""")]
        public void GivenIHaveEnabled(string name) {
            var webApp = Binding<WebAppHosting>();
            webApp.Host.Execute(() => {
                using (var environment = MvcApplication.CreateStandaloneEnvironment("Default")) {
                    var descriptorManager = environment.Resolve<IShellDescriptorManager>();
                    var descriptor = descriptorManager.GetShellDescriptor();
                    descriptorManager.UpdateShellDescriptor(
                        descriptor.SerialNumber,
                        descriptor.Features.Concat(new[] { new ShellFeature { Name = name } }),
                        descriptor.Parameters);
                }
            });

        }

        [Given(@"I have tenant ""(.*)\"" on ""(.*)\"" as ""(.*)\""")]
        public void GivenIHaveTenantOnSiteAsName(string shellName, string hostName, string siteName) {
            var webApp = Binding<WebAppHosting>();
            webApp.Host.Execute(() => {
                var shellSettings = new ShellSettings {
                    Name = shellName,
                    RequestUrlHost = hostName,
                    State = new TenantState("Uninitialized"),
                };
                using (var environment = MvcApplication.CreateStandaloneEnvironment("Default")) {
                    environment.Resolve<IShellSettingsManager>().SaveSettings(shellSettings);
                }
            });

            webApp.WhenIGoToPathOnHost("Setup", hostName);

            webApp.WhenIFillIn(TableData(
                new { name = "SiteName", value = siteName },
                new { name = "AdminPassword", value = "6655321" }));

            webApp.WhenIHit("Finish Setup");
        }
    }
}
