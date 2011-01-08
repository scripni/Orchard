// ------------------------------------------------------------------------------
//  <auto-generated>
//      This code was generated by SpecFlow (http://www.specflow.org/).
//      SpecFlow Version:1.5.0.0
//      Runtime Version:4.0.30319.1
// 
//      Changes to this file may cause incorrect behavior and will be lost if
//      the code is regenerated.
//  </auto-generated>
// ------------------------------------------------------------------------------
#region Designer generated code
namespace Orchard.Specs
{
    using TechTalk.SpecFlow;
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("TechTalk.SpecFlow", "1.5.0.0")]
    [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [NUnit.Framework.TestFixtureAttribute()]
    [NUnit.Framework.DescriptionAttribute("Setup")]
    public partial class SetupFeature
    {
        
        private static TechTalk.SpecFlow.ITestRunner testRunner;
        
#line 1 "Setup.feature"
#line hidden
        
        [NUnit.Framework.TestFixtureSetUpAttribute()]
        public virtual void FeatureSetup()
        {
            testRunner = TechTalk.SpecFlow.TestRunnerManager.GetTestRunner();
            TechTalk.SpecFlow.FeatureInfo featureInfo = new TechTalk.SpecFlow.FeatureInfo(new System.Globalization.CultureInfo("en-US"), "Setup", "In order to install orchard\r\nAs a new user\r\nI want to setup a new site from the d" +
                    "efault screen", GenerationTargetLanguage.CSharp, ((string[])(null)));
            testRunner.OnFeatureStart(featureInfo);
        }
        
        [NUnit.Framework.TestFixtureTearDownAttribute()]
        public virtual void FeatureTearDown()
        {
            testRunner.OnFeatureEnd();
            testRunner = null;
        }
        
        public virtual void ScenarioSetup(TechTalk.SpecFlow.ScenarioInfo scenarioInfo)
        {
            testRunner.OnScenarioStart(scenarioInfo);
        }
        
        [NUnit.Framework.TearDownAttribute()]
        public virtual void ScenarioTearDown()
        {
            testRunner.OnScenarioEnd();
        }
        
        [NUnit.Framework.TestAttribute()]
        [NUnit.Framework.DescriptionAttribute("Root request shows setup form")]
        public virtual void RootRequestShowsSetupForm()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Root request shows setup form", ((string[])(null)));
#line 6
this.ScenarioSetup(scenarioInfo);
#line hidden
            TechTalk.SpecFlow.Table table1 = new TechTalk.SpecFlow.Table(new string[] {
                        "extension",
                        "names"});
            table1.AddRow(new string[] {
                        "Module",
                        "Orchard.Setup, Orchard.Pages, Orchard.Users, Orchard.Roles, Orchard.Messaging, Or" +
                            "chard.Comments, Orchard.PublishLater, Orchard.Themes, Orchard.jQuery, TinyMce"});
            table1.AddRow(new string[] {
                        "Core",
                        "Common, Contents, Dashboard, Feeds, HomePage, Navigation, Routable, Scheduling, S" +
                            "ettings, Shapes, XmlRpc"});
            table1.AddRow(new string[] {
                        "Theme",
                        "SafeMode"});
#line 7
    testRunner.Given("I have a clean site with", ((string)(null)), table1);
#line 12
    testRunner.When("I go to \"/\"");
#line 13
    testRunner.Then("I should see \"Welcome to Orchard\"");
#line 14
        testRunner.And("I should see \"Finish Setup\"");
#line 15
        testRunner.And("the status should be 200 \"OK\"");
#line hidden
            testRunner.CollectScenarioErrors();
        }
        
        [NUnit.Framework.TestAttribute()]
        [NUnit.Framework.DescriptionAttribute("Setup folder also shows setup form")]
        public virtual void SetupFolderAlsoShowsSetupForm()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Setup folder also shows setup form", ((string[])(null)));
#line 17
this.ScenarioSetup(scenarioInfo);
#line hidden
            TechTalk.SpecFlow.Table table2 = new TechTalk.SpecFlow.Table(new string[] {
                        "extension",
                        "names"});
            table2.AddRow(new string[] {
                        "Module",
                        "Orchard.Setup, Orchard.Pages, Orchard.Users, Orchard.Roles, Orchard.Messaging, Or" +
                            "chard.Comments, Orchard.PublishLater, Orchard.Themes, Orchard.jQuery, TinyMce"});
            table2.AddRow(new string[] {
                        "Core",
                        "Common, Contents, Dashboard, Feeds, HomePage, Navigation, Routable, Scheduling, S" +
                            "ettings, Shapes, XmlRpc"});
            table2.AddRow(new string[] {
                        "Theme",
                        "SafeMode"});
#line 18
    testRunner.Given("I have a clean site with", ((string)(null)), table2);
#line 23
    testRunner.When("I go to \"/Setup\"");
#line 24
    testRunner.Then("I should see \"Welcome to Orchard\"");
#line 25
        testRunner.And("I should see \"Finish Setup\"");
#line 26
        testRunner.And("the status should be 200 \"OK\"");
#line hidden
            testRunner.CollectScenarioErrors();
        }
        
        [NUnit.Framework.TestAttribute()]
        [NUnit.Framework.DescriptionAttribute("Some of the initial form values are required")]
        public virtual void SomeOfTheInitialFormValuesAreRequired()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Some of the initial form values are required", ((string[])(null)));
#line 28
this.ScenarioSetup(scenarioInfo);
#line hidden
            TechTalk.SpecFlow.Table table3 = new TechTalk.SpecFlow.Table(new string[] {
                        "extension",
                        "names"});
            table3.AddRow(new string[] {
                        "Module",
                        "Orchard.Setup, Orchard.Pages, Orchard.Users, Orchard.Roles, Orchard.Messaging, Or" +
                            "chard.Comments, Orchard.PublishLater, Orchard.Themes, Orchard.jQuery, TinyMce"});
            table3.AddRow(new string[] {
                        "Core",
                        "Common, Contents, Dashboard, Feeds, HomePage, Navigation, Routable, Scheduling, S" +
                            "ettings, Shapes, XmlRpc"});
            table3.AddRow(new string[] {
                        "Theme",
                        "SafeMode"});
#line 29
    testRunner.Given("I have a clean site with", ((string)(null)), table3);
#line 34
    testRunner.When("I go to \"/Setup\"");
#line 35
        testRunner.And("I hit \"Finish Setup\"");
#line 36
    testRunner.Then("I should see \"<input autofocus=\"autofocus\" class=\"input-validation-error\" id=\"Sit" +
                    "eName\" name=\"SiteName\" type=\"text\" value=\"\" />\"");
#line 37
        testRunner.And("I should see \"<input class=\"input-validation-error\" id=\"AdminPassword\" name=\"Admi" +
                    "nPassword\" type=\"password\" />\"");
#line hidden
            testRunner.CollectScenarioErrors();
        }
        
        [NUnit.Framework.TestAttribute()]
        [NUnit.Framework.DescriptionAttribute("Calling setup on a brand new install")]
        public virtual void CallingSetupOnABrandNewInstall()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Calling setup on a brand new install", ((string[])(null)));
#line 39
this.ScenarioSetup(scenarioInfo);
#line hidden
            TechTalk.SpecFlow.Table table4 = new TechTalk.SpecFlow.Table(new string[] {
                        "extension",
                        "names"});
            table4.AddRow(new string[] {
                        "Module",
                        "Orchard.Setup, Orchard.Pages, Orchard.Users, Orchard.Roles, Orchard.Messaging, Or" +
                            "chard.Scripting, Orchard.Comments, Orchard.PublishLater, Orchard.Themes, Orchard" +
                            ".Modules, Orchard.Widgets, Orchard.jQuery, TinyMce"});
            table4.AddRow(new string[] {
                        "Core",
                        "Common, Contents, Dashboard, Feeds, HomePage, Navigation, Routable, Scheduling, S" +
                            "ettings, Shapes, XmlRpc"});
            table4.AddRow(new string[] {
                        "Theme",
                        "SafeMode, TheThemeMachine"});
#line 40
    testRunner.Given("I have a clean site with", ((string)(null)), table4);
#line 45
        testRunner.And("I am on \"/Setup\"");
#line hidden
            TechTalk.SpecFlow.Table table5 = new TechTalk.SpecFlow.Table(new string[] {
                        "name",
                        "value"});
            table5.AddRow(new string[] {
                        "SiteName",
                        "My Site"});
            table5.AddRow(new string[] {
                        "AdminPassword",
                        "6655321"});
            table5.AddRow(new string[] {
                        "ConfirmPassword",
                        "6655321"});
#line 46
    testRunner.When("I fill in", ((string)(null)), table5);
#line 51
        testRunner.And("I hit \"Finish Setup\"");
#line 52
        testRunner.And("I go to \"/\"");
#line 53
    testRunner.Then("I should see \"My Site\"");
#line 54
        testRunner.And("I should see \"Welcome, <strong><a href=\"/Users/Account/ChangePassword\">admin</a><" +
                    "/strong>!\"");
#line hidden
            testRunner.CollectScenarioErrors();
        }
    }
}
#endregion
