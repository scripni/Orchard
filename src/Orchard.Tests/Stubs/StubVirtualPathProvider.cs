﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Orchard.FileSystems.VirtualPath;
using Orchard.Services;

namespace Orchard.Tests.Stubs {
    public class StubVirtualPathProvider : IVirtualPathProvider {
        private readonly StubFileSystem _fileSystem;

        public StubVirtualPathProvider(StubFileSystem fileSystem) {
            _fileSystem = fileSystem;
        }

        public StubFileSystem FileSystem {
            get { return _fileSystem; }
        }

        private string ToFileSystemPath(string path) {
            if (path.StartsWith("~/"))
                return path.Substring(2);
            if (path.StartsWith("/"))
                return path.Substring(1);
            return path;
        }

        public string Combine(params string[] paths) {
            return Path.Combine(paths).Replace(Path.DirectorySeparatorChar, '/');
        }

        public string ToAppRelative(string virtualPath) {
            return "~/" + ToFileSystemPath(virtualPath);
        }

        public string MapPath(string virtualPath) {
            throw new NotImplementedException("Mapping to a physical file is not supported in Unit Test with this stub.");
        }

        public bool FileExists(string virtualPath) {
            return _fileSystem.GetFileEntry(ToFileSystemPath(virtualPath)) != null;
        }

        public Stream OpenFile(string virtualPath) {
            return _fileSystem.OpenFile(ToFileSystemPath(virtualPath));
        }

        public StreamWriter CreateText(string virtualPath) {
            return new StreamWriter(_fileSystem.CreateFile(ToFileSystemPath(virtualPath)));
        }

        public Stream CreateFile(string virtualPath) {
            return _fileSystem.CreateFile(ToFileSystemPath(virtualPath));
        }

        public DateTime GetFileLastWriteTimeUtc(string virtualPath) {
            return _fileSystem.GetFileEntry(ToFileSystemPath(virtualPath)).LastWriteTimeUtc;
        }

        public bool DirectoryExists(string virtualPath) {
            return _fileSystem.GetDirectoryEntry(ToFileSystemPath(virtualPath)) != null;
        }

        public void CreateDirectory(string virtualPath) {
            _fileSystem.CreateDirectoryEntry(ToFileSystemPath(virtualPath));
        }

        public string GetDirectoryName(string virtualPath) {
            return Path.GetDirectoryName(virtualPath);
        }

        public IEnumerable<string> ListFiles(string path) {
            return _fileSystem.GetDirectoryEntry(ToFileSystemPath(path))
                .Files
                .Select(f => Combine(path, f.Name));
        }

        public IEnumerable<string> ListDirectories(string path) {
            return _fileSystem.GetDirectoryEntry(ToFileSystemPath(path))
                .Directories
                .Select(f => Combine(path, f.Name));
        }
    }
}