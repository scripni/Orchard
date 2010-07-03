﻿using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Compilation;

namespace Orchard.Environment.Extensions.Compilers {
    /// <summary>
    /// Compile a C# extension into an assembly given a directory location
    /// Note: Currently not used...
    /// </summary>
    public class CSharpExtensionDirectoryCompiler {
        private readonly IBuildManager _buildManager;

        public CSharpExtensionDirectoryCompiler(IBuildManager buildManager) {
            _buildManager = buildManager;
        }

        public CompilerResults CompileProject(string location) {
            var codeProvider = CodeDomProvider.CreateProvider("cs");

            var references = GetAssemblyReferenceNames();
            var options = new CompilerParameters(references.ToArray());

            var fileNames = GetSourceFileNames(location);
            var results = codeProvider.CompileAssemblyFromFile(options, fileNames.ToArray());
            return results;
        }

        private IEnumerable<string> GetAssemblyReferenceNames() {
            return _buildManager
                .GetReferencedAssemblies()
                .Select(x => x.Location)
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private IEnumerable<string> GetSourceFileNames(string path) {
            foreach (var file in Directory.GetFiles(path, "*.cs")) {
                yield return file;
            }

            foreach (var folder in Directory.GetDirectories(path)) {
                if (Path.GetFileName(folder).StartsWith("."))
                    continue;

                foreach (var file in GetSourceFileNames(folder)) {
                    yield return file;
                }
            }
        }
    }
}