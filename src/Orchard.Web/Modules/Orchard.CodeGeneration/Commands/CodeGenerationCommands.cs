﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.Hosting;
using Orchard.Commands;
using Orchard.Data.Migration.Generator;
using Orchard.CodeGeneration.Services;
using Orchard.Data.Migration.Schema;
using Orchard.Environment.Extensions;
using Orchard.Environment.Extensions.Models;
using Orchard.Localization;

namespace Orchard.CodeGeneration.Commands {

    public class CodeGenerationCommands : DefaultOrchardCommandHandler {

        private readonly IExtensionManager _extensionManager;
        private readonly ISchemaCommandGenerator _schemaCommandGenerator;

        private static readonly string[] _themeDirectories = new [] {
            "", "Content", "Styles", "Scripts", "Views", "Zones"
        };
        private static readonly string[] _moduleDirectories = new [] {
            "", "Properties", "Controllers", "Views", "Models", "Scripts"
        };

        private const string ModuleName = "CodeGeneration";
        private static readonly string _codeGenTemplatePath = HostingEnvironment.MapPath("~/Modules/Orchard." + ModuleName + "/CodeGenerationTemplates/");
        private static readonly string _orchardWebProj = HostingEnvironment.MapPath("~/Orchard.Web.csproj");
        private static readonly string _orchardThemesProj = HostingEnvironment.MapPath("~/Themes/Orchard.Themes.csproj");

        public CodeGenerationCommands(
            IExtensionManager extensionManager,
            ISchemaCommandGenerator schemaCommandGenerator) {
            _extensionManager = extensionManager;
            _schemaCommandGenerator = schemaCommandGenerator;
        }

        [OrchardSwitch]
        public bool IncludeInSolution { get; set; }

        [OrchardSwitch]
        public bool CreateProject { get; set; }

        [OrchardSwitch]
        public string BasedOn { get; set; }

        [CommandHelp("generate create datamigration <feature-name> \r\n\t" + "Create a new Data Migration class")]
        [CommandName("generate create datamigration")]
        public bool CreateDataMigration(string featureName) {
            Context.Output.WriteLine(T("Creating Data Migration for {0}", featureName));

            ExtensionDescriptor extensionDescriptor = _extensionManager.AvailableExtensions().FirstOrDefault(extension => extension.ExtensionType == "Module" &&
                                                                                                             extension.Features.Any(feature => String.Equals(feature.Name, featureName, StringComparison.OrdinalIgnoreCase)));

            if (extensionDescriptor == null) {
                Context.Output.WriteLine(T("Creating data migration failed: target Feature {0} could not be found.", featureName));
                return false;
            }

            string dataMigrationFolderPath = HostingEnvironment.MapPath("~/Modules/" + extensionDescriptor.Name + "/");
            string dataMigrationFilePath = dataMigrationFolderPath + "Migration.cs";
            string templatesPath = HostingEnvironment.MapPath("~/Modules/Orchard." + ModuleName + "/CodeGenerationTemplates/");
            string moduleCsProjPath = HostingEnvironment.MapPath(string.Format("~/Modules/{0}/{0}.csproj", extensionDescriptor.Name));
                    
            if (!Directory.Exists(dataMigrationFolderPath)) {
                Directory.CreateDirectory(dataMigrationFolderPath);
            }

            if (File.Exists(dataMigrationFilePath)) {
                Context.Output.WriteLine(T("Data migration already exists in target Module {0}.", extensionDescriptor.Name));
                return false;
            }

            List<SchemaCommand> commands = _schemaCommandGenerator.GetCreateFeatureCommands(featureName, false).ToList();
                    
            var stringWriter = new StringWriter();
            var interpreter = new CodeGenerationCommandInterpreter(stringWriter);

            foreach (var command in commands) {
                interpreter.Visit(command);
                stringWriter.WriteLine();
            }

            string dataMigrationText = File.ReadAllText(templatesPath + "DataMigration.txt");
            dataMigrationText = dataMigrationText.Replace("$$FeatureName$$", featureName);
            dataMigrationText = dataMigrationText.Replace("$$Commands$$", stringWriter.ToString());
            File.WriteAllText(dataMigrationFilePath, dataMigrationText);

            string projectFileText = File.ReadAllText(moduleCsProjPath);

            // The string searches in solution/project files can be made aware of comment lines.
            if ( projectFileText.Contains("<Compile Include") ) {
                string compileReference = string.Format("<Compile Include=\"{0}\" />\r\n    ", "DataMigrations\\" + extensionDescriptor.DisplayName + "DataMigration.cs");
                projectFileText = projectFileText.Insert(projectFileText.LastIndexOf("<Compile Include"), compileReference);
            }
            else {
                string itemGroupReference = string.Format("</ItemGroup>\r\n  <ItemGroup>\r\n    <Compile Include=\"{0}\" />\r\n  ", "DataMigrations\\" + extensionDescriptor.DisplayName + "DataMigration.cs");
                projectFileText = projectFileText.Insert(projectFileText.LastIndexOf("</ItemGroup>"), itemGroupReference);
            }

            File.WriteAllText(moduleCsProjPath, projectFileText);
            TouchSolution(Context.Output, T);
            Context.Output.WriteLine(T("Data migration created successfully in Module {0}", extensionDescriptor.Name));

            return true;
        }

        [CommandHelp("generate create module <module-name> [/IncludeInSolution:true|false]\r\n\t" + "Create a new Orchard module")]
        [CommandName("generate create module")]
        [OrchardSwitches("IncludeInSolution")]
        public bool CreateModule(string moduleName) {
            Context.Output.WriteLine(T("Creating Module {0}", moduleName));

            if ( _extensionManager.AvailableExtensions().Any(extension => String.Equals(moduleName, extension.DisplayName, StringComparison.OrdinalIgnoreCase)) ) {
                Context.Output.WriteLine(T("Creating Module {0} failed: a module of the same name already exists", moduleName));
                return false;
            }

            IntegrateModule(moduleName);
            Context.Output.WriteLine(T("Module {0} created successfully", moduleName));

            return true;
        }

        [CommandName("generate create theme")]
        [CommandHelp("generate create theme <theme-name> [/CreateProject:true|false][/IncludeInSolution:true|false][/BasedOn:<theme-name>]\r\n\tCreate a new Orchard theme")]
        [OrchardSwitches("IncludeInSolution,BasedOn,CreateProject")]
        public void CreateTheme(string themeName) {
            Context.Output.WriteLine(T("Creating Theme {0}", themeName));
            if (_extensionManager.AvailableExtensions().Any(extension => String.Equals(themeName, extension.Name, StringComparison.OrdinalIgnoreCase))) {
                Context.Output.WriteLine(T("Creating Theme {0} failed: an extention of the same name already exists", themeName));
            }
            else {
                if (!string.IsNullOrEmpty(BasedOn)) {
                    if (!_extensionManager.AvailableExtensions().Any(extension =>
                        string.Equals(extension.ExtensionType, "Theme", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(BasedOn, extension.Name, StringComparison.OrdinalIgnoreCase))) {
                        Context.Output.WriteLine(T("Creating Theme {0} failed: base theme named {1} was not found.", themeName, BasedOn));
                        return;
                    }
                }
                IntegrateTheme(themeName, BasedOn);
                Context.Output.WriteLine(T("Theme {0} created successfully", themeName));
            }
        }

        [CommandHelp("generate create controller <module-name> <controller-name>\r\n\t" + "Create a new Orchard controller in a module")]
        [CommandName("generate create controller")]
        public void CreateController(string moduleName, string controllerName) {
            Context.Output.WriteLine(T("Creating Controller {0} in Module {1}", controllerName, moduleName));

            ExtensionDescriptor extensionDescriptor = _extensionManager.AvailableExtensions().FirstOrDefault(extension => extension.ExtensionType == "Module" &&
                                                                                                             string.Equals(moduleName, extension.DisplayName, StringComparison.OrdinalIgnoreCase));

            if (extensionDescriptor == null) {
                Context.Output.WriteLine(T("Creating Controller {0} failed: target Module {1} could not be found.", controllerName, moduleName));
                return;
            }

            string moduleControllersPath = HostingEnvironment.MapPath("~/Modules/" + extensionDescriptor.Name + "/Controllers/");
            string controllerPath = moduleControllersPath + controllerName + ".cs";
            string moduleCsProjPath = HostingEnvironment.MapPath(string.Format("~/Modules/{0}/{0}.csproj", extensionDescriptor.Name));
            string templatesPath = HostingEnvironment.MapPath("~/Modules/Orchard." + ModuleName + "/CodeGenerationTemplates/");

            if (!Directory.Exists(moduleControllersPath)) {
                Directory.CreateDirectory(moduleControllersPath);
            }
            if (File.Exists(controllerPath)) {
                Context.Output.WriteLine(T("Controller {0} already exists in target Module {1}.", controllerName, moduleName));
                return;
            }

            string controllerText = File.ReadAllText(templatesPath + "Controller.txt");
            controllerText = controllerText.Replace("$$ModuleName$$", moduleName);
            controllerText = controllerText.Replace("$$ControllerName$$", controllerName);
            File.WriteAllText(controllerPath, controllerText);
            string projectFileText = File.ReadAllText(moduleCsProjPath);

            // The string searches in solution/project files can be made aware of comment lines.
            if (projectFileText.Contains("<Compile Include")) {
                string compileReference = string.Format("<Compile Include=\"{0}\" />\r\n    ", "Controllers\\" + controllerName + ".cs");
                projectFileText = projectFileText.Insert(projectFileText.LastIndexOf("<Compile Include"), compileReference);
            }
            else {
                string itemGroupReference = string.Format("</ItemGroup>\r\n  <ItemGroup>\r\n    <Compile Include=\"{0}\" />\r\n  ", "Controllers\\" + controllerName + ".cs");
                projectFileText = projectFileText.Insert(projectFileText.LastIndexOf("</ItemGroup>"), itemGroupReference);
            }

            File.WriteAllText(moduleCsProjPath, projectFileText);
            Context.Output.WriteLine(T("Controller {0} created successfully in Module {1}", controllerName, moduleName));
            TouchSolution(Context.Output, T);
        }

        private void IntegrateModule(string moduleName) {
            string projectGuid = Guid.NewGuid().ToString().ToUpper();

            CreateFilesFromTemplates(moduleName, projectGuid);
            // The string searches in solution/project files can be made aware of comment lines.
            if (IncludeInSolution) {
                AddToSolution(Context.Output, T, moduleName, projectGuid, "Modules");
            }
        }

        private void IntegrateTheme(string themeName, string baseTheme) {
            CreateThemeFromTemplates(Context.Output, T,
                themeName,
                baseTheme,
                CreateProject ? Guid.NewGuid().ToString().ToUpper() : null,
                IncludeInSolution);
        }

        private static void CreateFilesFromTemplates(string moduleName, string projectGuid) {
            string modulePath = HostingEnvironment.MapPath("~/Modules/" + moduleName + "/");
            string propertiesPath = modulePath + "Properties";
            var content = new HashSet<string>();
            var folders = new HashSet<string>();

            foreach(var folder in _moduleDirectories) {
                Directory.CreateDirectory(modulePath + folder);
                if (folder != "") {
                    folders.Add(modulePath + folder);
                }
            }

            File.WriteAllText(modulePath + "Views\\Web.config", File.ReadAllText(_codeGenTemplatePath + "ViewsWebConfig.txt"));
            content.Add(modulePath + "Views\\Web.config");

            string templateText = File.ReadAllText(_codeGenTemplatePath + "ModuleAssemblyInfo.txt");
            templateText = templateText.Replace("$$ModuleName$$", moduleName);
            templateText = templateText.Replace("$$ModuleTypeLibGuid$$", Guid.NewGuid().ToString());
            File.WriteAllText(propertiesPath + "\\AssemblyInfo.cs", templateText);
            content.Add(propertiesPath + "\\AssemblyInfo.cs");

            File.WriteAllText(modulePath + "Web.config", File.ReadAllText(_codeGenTemplatePath + "ModuleWebConfig.txt"));
            templateText = File.ReadAllText(_codeGenTemplatePath + "ModuleManifest.txt");
            templateText = templateText.Replace("$$ModuleName$$", moduleName);
            File.WriteAllText(modulePath + "Module.txt", templateText);
            content.Add(modulePath + "Module.txt");

            var itemGroup = CreateProjectItemGroup(modulePath, content, folders);

            File.WriteAllText(modulePath + moduleName + ".csproj", CreateCsProject(moduleName, projectGuid, itemGroup));
        }

        private static string CreateCsProject(string projectName, string projectGuid, string itemGroup) {
            string text = File.ReadAllText(_codeGenTemplatePath + "\\ModuleCsProj.txt");
            text = text.Replace("$$ModuleName$$", projectName);
            text = text.Replace("$$ModuleProjectGuid$$", projectGuid);
            text = text.Replace("$$FileIncludes$$", itemGroup ?? "");
            return text;
        }

        private static void CreateThemeFromTemplates(TextWriter output, Localizer T, string themeName, string baseTheme, string projectGuid, bool includeInSolution) {
            var themePath = HostingEnvironment.MapPath("~/Themes/" + themeName + "/");
            var createdFiles = new HashSet<string>();
            var createdFolders = new HashSet<string>();

            // create directories
            foreach (var folderName in _themeDirectories) {
                var folder = themePath + folderName;
                Directory.CreateDirectory(folder);
                if (folderName != "") {
                    createdFolders.Add(folder);
                }
            }

            var webConfig = themePath + "Views\\Web.config";
            File.WriteAllText(webConfig, File.ReadAllText(_codeGenTemplatePath + "\\ViewsWebConfig.txt"));
            createdFiles.Add(webConfig);

            var templateText = File.ReadAllText(_codeGenTemplatePath + "\\ThemeManifest.txt").Replace("$$ThemeName$$", themeName);
            if (string.IsNullOrEmpty(baseTheme)) {
                templateText = templateText.Replace("BaseTheme: $$BaseTheme$$\r\n", "");
            }
            else {
                templateText = templateText.Replace("$$BaseTheme$$", baseTheme);
            }

            File.WriteAllText(themePath + "Theme.txt", templateText);
            createdFiles.Add(themePath + "Theme.txt");

            // create new csproj for the theme
            if (projectGuid != null) {
                var itemGroup = CreateProjectItemGroup(themePath, createdFiles, createdFolders);
                string projectText = CreateCsProject(themeName, projectGuid, itemGroup);
                File.WriteAllText(themePath + "\\" + themeName + ".csproj", projectText);
            }

            if (includeInSolution) {
                if (projectGuid == null) {
                    // include in solution but dont create a project: just add the references to Orchard.Themes project
                    var itemGroup = CreateProjectItemGroup(HostingEnvironment.MapPath("~/Themes/"), createdFiles, createdFolders);
                    AddFilesToOrchardThemesProject(output, T, itemGroup);
                }
                else {
                    // create a project (already done) and add it to the solution
                    AddToSolution(output, T, themeName, projectGuid, "Themes");
                }
            }
        }


        private static void AddToSolution(TextWriter output, Localizer T, string projectName, string projectGuid, string containingFolder) {
            if (!string.IsNullOrEmpty(projectGuid)) {
                var solutionPath = Directory.GetParent(_orchardWebProj).Parent.FullName + "\\Orchard.sln";
                if (File.Exists(solutionPath)) {
                    var projectReference = string.Format("EndProject\r\nProject(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{0}\", \"Orchard.Web\\{2}\\{0}\\{0}.csproj\", \"{{{1}}}\"\r\n", projectName, projectGuid, containingFolder);
                    var projectConfiguationPlatforms = string.Format("GlobalSection(ProjectConfigurationPlatforms) = postSolution\r\n\t\t{{{0}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU\r\n\t\t{{{0}}}.Debug|Any CPU.Build.0 = Debug|Any CPU\r\n\t\t{{{0}}}.Release|Any CPU.ActiveCfg = Release|Any CPU\r\n\t\t{{{0}}}.Release|Any CPU.Build.0 = Release|Any CPU\r\n", projectGuid);
                    var solutionText = File.ReadAllText(solutionPath);
                    solutionText = solutionText.Insert(solutionText.LastIndexOf("EndProject\r\n"), projectReference).Replace("GlobalSection(ProjectConfigurationPlatforms) = postSolution\r\n", projectConfiguationPlatforms);
                    solutionText = solutionText.Insert(solutionText.LastIndexOf("EndGlobalSection"), "\t{" + projectGuid + "} = {E9C9F120-07BA-4DFB-B9C3-3AFB9D44C9D5}\r\n\t");
                    File.WriteAllText(solutionPath, solutionText);
                    TouchSolution(output, T);
                }
                else {
                    output.WriteLine(T("Warning: Solution file could not be found at {0}", solutionPath));
                }
            }
        }

        private static string CreateProjectItemGroup(string relativeFromPath, HashSet<string> content, HashSet<string> folders) {
            var contentInclude = "";
            if (relativeFromPath != null && !relativeFromPath.EndsWith("\\", StringComparison.OrdinalIgnoreCase)) {
                relativeFromPath += "\\";
            }
            else if (relativeFromPath == null) {
                relativeFromPath = "";
            }

            if (content != null && content.Count > 0) {
                contentInclude = string.Join("\r\n",
                                             from file in content
                                             select "    <Content Include=\"" + file.Replace(relativeFromPath, "") + "\" />");
            }
            if (folders != null && folders.Count > 0) {
                contentInclude += "\r\n" + string.Join("\r\n", from folder in folders
                                                               select "    <Folder Include=\"" + folder.Replace(relativeFromPath, "") + "\" />");
            }
            return string.Format(CultureInfo.InvariantCulture, "<ItemGroup>\r\n{0}\r\n  </ItemGroup>\r\n  ", contentInclude);
        }

        private static void AddFilesToOrchardThemesProject(TextWriter output, Localizer T, string itemGroup) {
            if (!File.Exists(_orchardThemesProj)) {
                output.WriteLine(T("Warning: Orchard.Themes project file could not be found at {0}", _orchardThemesProj));
            }
            else {
                var projectText = File.ReadAllText(_orchardThemesProj);

                // find where the first ItemGroup is after any References
                var refIndex = projectText.LastIndexOf("<Reference Include");
                if (refIndex != -1) {
                    var firstItemGroupIndex = projectText.IndexOf("<ItemGroup>", refIndex);
                    if (firstItemGroupIndex != -1) {
                        projectText = projectText.Insert(firstItemGroupIndex, itemGroup);
                        File.WriteAllText(_orchardThemesProj, projectText);
                        return;
                    }
                }
                output.WriteLine(T("Warning: Unable to modify Orchard.Themes project file at {0}", _orchardThemesProj));
            }
        }

        private static void TouchSolution(TextWriter output, Localizer T) {
            string rootWebProjectPath = HostingEnvironment.MapPath("~/Orchard.Web.csproj");
            string solutionPath = Directory.GetParent(rootWebProjectPath).Parent.FullName + "\\Orchard.sln";
            if (!File.Exists(solutionPath)) {
                output.WriteLine(T("Warning: Solution file could not be found at {0}", solutionPath));
                return;
            }

            try {
                File.SetLastWriteTime(solutionPath, DateTime.Now);
            }
            catch {
                output.WriteLine(T("An unexpected error occured while trying to refresh the Visual Studio solution. Please reload it."));
            }
        }
    }
}