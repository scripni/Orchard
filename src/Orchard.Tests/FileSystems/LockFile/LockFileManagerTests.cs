﻿using System.IO;
using System.Linq;
using NUnit.Framework;
using Orchard.FileSystems.AppData;
using Orchard.FileSystems.LockFile;
using Orchard.Tests.Stubs;

namespace Orchard.Tests.FileSystems.LockFile {
    [TestFixture]
    public class LockFileManagerTests {
        private string _tempFolder;
        private IAppDataFolder _appDataFolder;
        private ILockFileManager _lockFileManager;
        private StubClock _clock;

        public class StubAppDataFolderRoot : IAppDataFolderRoot {
            public string RootPath { get; set; }
            public string RootFolder { get; set; }
        }

        public static IAppDataFolder CreateAppDataFolder(string tempFolder) {
            var folderRoot = new StubAppDataFolderRoot {RootPath = "~/App_Data", RootFolder = tempFolder};
            var monitor = new StubVirtualPathMonitor();
            return new AppDataFolder(folderRoot, monitor);
        }

        [SetUp]
        public void Init() {
            _tempFolder = Path.GetTempFileName();
            File.Delete(_tempFolder);
            _appDataFolder = CreateAppDataFolder(_tempFolder);

            _clock = new StubClock();
            _lockFileManager = new DefaultLockFileManager(_appDataFolder, _clock);
        }

        [TearDown]
        public void Term() {
            Directory.Delete(_tempFolder, true);
        }

        [Test]
        public void LockShouldBeGrantedWhenDoesNotExist() {
            ILockFile lockFile = null;
            var granted = _lockFileManager.TryAcquireLock("foo.txt.lock", ref lockFile);

            Assert.That(granted, Is.True);
            Assert.That(lockFile, Is.Not.Null);
            Assert.That(_lockFileManager.IsLocked("foo.txt.lock"), Is.True);
            Assert.That(_appDataFolder.ListFiles("").Count(), Is.EqualTo(1));
        }

        [Test]
        public void ExistingLockFileShouldPreventGrants() {
            ILockFile lockFile = null;
            _lockFileManager.TryAcquireLock("foo.txt.lock", ref lockFile);
            
            Assert.That(_lockFileManager.TryAcquireLock("foo.txt.lock", ref lockFile), Is.False);
            Assert.That(_lockFileManager.IsLocked("foo.txt.lock"), Is.True);
            Assert.That(_appDataFolder.ListFiles("").Count(), Is.EqualTo(1));
        }

        [Test]
        public void ReleasingALockShouldAllowGranting() {
            ILockFile lockFile = null;
            _lockFileManager.TryAcquireLock("foo.txt.lock", ref lockFile);

            using (lockFile) {
                Assert.That(_lockFileManager.IsLocked("foo.txt.lock"), Is.True);
                Assert.That(_appDataFolder.ListFiles("").Count(), Is.EqualTo(1));
            }

            Assert.That(_lockFileManager.IsLocked("foo.txt.lock"), Is.False);
            Assert.That(_appDataFolder.ListFiles("").Count(), Is.EqualTo(0));
        }

        [Test]
        public void ReleasingAReleasedLockShouldWork() {
            ILockFile lockFile = null;
            _lockFileManager.TryAcquireLock("foo.txt.lock", ref lockFile);
            
            Assert.That(_lockFileManager.IsLocked("foo.txt.lock"), Is.True);
            Assert.That(_appDataFolder.ListFiles("").Count(), Is.EqualTo(1));
            
            lockFile.Release();
            Assert.That(_lockFileManager.IsLocked("foo.txt.lock"), Is.False);
            Assert.That(_appDataFolder.ListFiles("").Count(), Is.EqualTo(0));
            
            lockFile.Release();
            Assert.That(_lockFileManager.IsLocked("foo.txt.lock"), Is.False);
            Assert.That(_appDataFolder.ListFiles("").Count(), Is.EqualTo(0));
        }

        [Test]
        public void ExpiredLockShouldBeAvailable() {
            ILockFile lockFile = null;
            _lockFileManager.TryAcquireLock("foo.txt.lock", ref lockFile);

            _clock.Advance(DefaultLockFileManager.Expiration);
            Assert.That(_lockFileManager.IsLocked("foo.txt.lock"), Is.False);
            Assert.That(_appDataFolder.ListFiles("").Count(), Is.EqualTo(1));
        }

        [Test]
        public void ShouldGrantExpiredLock() {
            ILockFile lockFile = null;
            _lockFileManager.TryAcquireLock("foo.txt.lock", ref lockFile);

            _clock.Advance(DefaultLockFileManager.Expiration);
            var granted = _lockFileManager.TryAcquireLock("foo.txt.lock", ref lockFile);

            Assert.That(granted, Is.True);
            Assert.That(_appDataFolder.ListFiles("").Count(), Is.EqualTo(1));
        }
    }
}
