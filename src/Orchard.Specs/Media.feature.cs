// ------------------------------------------------------------------------------
//  <auto-generated>
//      This code was generated by SpecFlow (http://www.specflow.org/).
//      SpecFlow Version:1.3.2.0
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
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("TechTalk.SpecFlow", "1.3.2.0")]
    [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [NUnit.Framework.TestFixtureAttribute()]
    [NUnit.Framework.DescriptionAttribute("Media management")]
    public partial class MediaManagementFeature
    {
        
        private static TechTalk.SpecFlow.ITestRunner testRunner;
        
#line 1 "Media.feature"
#line hidden
        
        [NUnit.Framework.TestFixtureSetUpAttribute()]
        public virtual void FeatureSetup()
        {
            testRunner = TechTalk.SpecFlow.TestRunnerManager.GetTestRunner();
            TechTalk.SpecFlow.FeatureInfo featureInfo = new TechTalk.SpecFlow.FeatureInfo(new System.Globalization.CultureInfo("en-US"), "Media management", "In order to reference images and such from content\r\nAs an author\r\nI want to uploa" +
                    "d and manage files in a media folder", ((string[])(null)));
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
        [NUnit.Framework.DescriptionAttribute("Media admin is available")]
        public virtual void MediaAdminIsAvailable()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Media admin is available", ((string[])(null)));
#line 6
this.ScenarioSetup(scenarioInfo);
#line 7
testRunner.Given("I have installed Orchard");
#line 8
testRunner.And("I have installed \"Orchard.Media\"");
#line 9
testRunner.When("I go to \"admin/media\"");
#line 10
testRunner.Then("I should see \"Manage Media Folders\"");
#line 11
testRunner.And("the status should be 200 \"OK\"");
#line hidden
            testRunner.CollectScenarioErrors();
        }
        
        [NUnit.Framework.TestAttribute()]
        [NUnit.Framework.DescriptionAttribute("Creating a folder")]
        public virtual void CreatingAFolder()
        {
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Creating a folder", ((string[])(null)));
#line 13
this.ScenarioSetup(scenarioInfo);
#line 14
testRunner.Given("I have installed Orchard");
#line 15
testRunner.And("I have installed \"Orchard.Media\"");
#line 16
testRunner.When("I go to \"admin/media/create\"");
#line hidden
            TechTalk.SpecFlow.Table table1 = new TechTalk.SpecFlow.Table(new string[] {
                        "name",
                        "value"});
            table1.AddRow(new string[] {
                        "Name",
                        "Hello World"});
#line 17
testRunner.And("I fill in", ((string)(null)), table1);
#line 20
testRunner.And("I hit \"Save\"");
#line 21
testRunner.And("I am redirected");
#line 22
testRunner.Then("I should see \"Manage Media Folders\"");
#line 23
testRunner.And("I should see \"Hello World\"");
#line 24
testRunner.And("the status should be 200 \"OK\"");
#line hidden
            testRunner.CollectScenarioErrors();
        }
    }
}
#endregion
