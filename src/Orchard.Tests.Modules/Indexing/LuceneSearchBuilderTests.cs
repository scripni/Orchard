﻿using System;
using System.IO;
using System.Linq;
using Autofac;
using Lucene.Services;
using NUnit.Framework;
using Orchard.Environment.Configuration;
using Orchard.FileSystems.AppData;
using Orchard.Indexing;
using Orchard.Indexing.Services;
using Orchard.Tests.FileSystems.AppData;

namespace Orchard.Tests.Modules.Indexing {
    public class LuceneSearchBuilderTests {
        private IContainer _container;
        private IIndexProvider _provider;
        private IAppDataFolder _appDataFolder;
        private ShellSettings _shellSettings;
        private readonly string _basePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        
        [TestFixtureTearDown]
        public void Clean() {
            if (Directory.Exists(_basePath)) {
                Directory.Delete(_basePath, true);
            }
        }

        [SetUp]
        public void Setup() {
            if (Directory.Exists(_basePath)) {
                Directory.Delete(_basePath, true);
            }
            Directory.CreateDirectory(_basePath);

            _appDataFolder = AppDataFolderTests.CreateAppDataFolder(_basePath);

            var builder = new ContainerBuilder();
            builder.RegisterType<LuceneIndexProvider>().As<IIndexProvider>();
            builder.RegisterInstance(_appDataFolder).As<IAppDataFolder>();

            // setting up a ShellSettings instance
            _shellSettings = new ShellSettings { Name = "My Site" };
            builder.RegisterInstance(_shellSettings).As<ShellSettings>();

            _container = builder.Build();
            _provider = _container.Resolve<IIndexProvider>();
        }

        private ISearchBuilder _searchBuilder { get { return _provider.CreateSearchBuilder("default"); } }

        [Test]
        public void SearchTermsShouldBeFoundInMultipleFields() {
            _provider.CreateIndex("default");
            _provider.Store("default", 
                _provider.New(42)
                    .Add("title", "title1 title2 title3").Analyze()
                    .Add("date", new DateTime(2010, 05, 28, 14, 13, 56, 123))
                );

            Assert.IsNotNull(_provider.CreateSearchBuilder("default").Get(42));

            Assert.IsNotNull(_provider.CreateSearchBuilder("default").WithField("title", "title1").Search().FirstOrDefault());
            Assert.IsNotNull(_provider.CreateSearchBuilder("default").WithField("title", "title2").Search().FirstOrDefault());
            Assert.IsNotNull(_provider.CreateSearchBuilder("default").WithField("title", "title3").Search().FirstOrDefault());
            Assert.IsNull(_provider.CreateSearchBuilder("default").WithField("title", "title4").Search().FirstOrDefault());

        }

        [Test]
        public void ShouldSearchById() {
            _provider.CreateIndex("default");
            _provider.Store("default", _provider.New(1));
            _provider.Store("default", _provider.New(2));
            _provider.Store("default", _provider.New(3));


            Assert.That(_searchBuilder.Get(1).ContentItemId, Is.EqualTo(1));
            Assert.That(_searchBuilder.Get(2).ContentItemId, Is.EqualTo(2));
            Assert.That(_searchBuilder.Get(3).ContentItemId, Is.EqualTo(3));
        }

        [Test]
        public void ShouldSearchWithField() {
            _provider.CreateIndex("default");
            _provider.Store("default", _provider.New(1).Add("title", "cat"));
            _provider.Store("default", _provider.New(2).Add("title", "dog"));
            _provider.Store("default", _provider.New(3).Add("title", "cat"));


            Assert.That(_searchBuilder.WithField("title", "cat").Search().Count(), Is.EqualTo(2));
            Assert.That(_searchBuilder.WithField("title", "cat").Search().Any(hit => new[] { 1, 3 }.Contains(hit.ContentItemId)), Is.True);

        }

        [Test]
        public void ShouldCountResultsOnly() {
            _provider.CreateIndex("default");
            _provider.Store("default", _provider.New(1).Add("title", "cat"));
            _provider.Store("default", _provider.New(2).Add("title", "dog"));
            _provider.Store("default", _provider.New(3).Add("title", "cat"));

            Assert.That(_searchBuilder.WithField("title", "dog").Count(), Is.EqualTo(1));
            Assert.That(_searchBuilder.WithField("title", "cat").Count(), Is.EqualTo(2));
        }

        [Test]
        public void ShouldFilterByDate() {
            _provider.CreateIndex("default");
            _provider.Store("default", _provider.New(1).Add("date", new DateTime(2010, 05, 28, 12, 30, 15)));
            _provider.Store("default", _provider.New(2).Add("date", new DateTime(2010, 05, 28, 12, 30, 30)));
            _provider.Store("default", _provider.New(3).Add("date", new DateTime(2010, 05, 28, 12, 30, 45)));

            Assert.That(_searchBuilder.WithinRange("date", new DateTime(2010, 05, 28, 12, 30, 15), DateTime.MaxValue).Count(), Is.EqualTo(3));
            Assert.That(_searchBuilder.WithinRange("date", DateTime.MinValue, new DateTime(2010, 05, 28, 12, 30, 45)).Count(), Is.EqualTo(3));
            Assert.That(_searchBuilder.WithinRange("date", new DateTime(2010, 05, 28, 12, 30, 15), new DateTime(2010, 05, 28, 12, 30, 45)).Count(), Is.EqualTo(3));
            Assert.That(_searchBuilder.WithinRange("date", new DateTime(2010, 05, 28, 12, 30, 16), new DateTime(2010, 05, 28, 12, 30, 44)).Count(), Is.EqualTo(1));
            Assert.That(_searchBuilder.WithinRange("date", new DateTime(2010, 05, 28, 12, 30, 46), DateTime.MaxValue).Count(), Is.EqualTo(0));
            Assert.That(_searchBuilder.WithinRange("date", DateTime.MinValue, new DateTime(2010, 05, 28, 12, 30, 1)).Count(), Is.EqualTo(0));
        }

        [Test]
        public void ShouldSliceResults() {
            _provider.CreateIndex("default");
            _provider.Store("default", _provider.New(1));
            _provider.Store("default", _provider.New(22));
            _provider.Store("default", _provider.New(333));
            _provider.Store("default", _provider.New(4444));
            _provider.Store("default", _provider.New(55555));

            
            Assert.That(_searchBuilder.Count(), Is.EqualTo(5));
            Assert.That(_searchBuilder.Slice(0, 3).Count(), Is.EqualTo(3));
            Assert.That(_searchBuilder.Slice(1, 3).Count(), Is.EqualTo(3));
            Assert.That(_searchBuilder.Slice(3, 3).Count(), Is.EqualTo(2));

            // Count() and Search() should return the same results
            Assert.That(_searchBuilder.Search().Count(), Is.EqualTo(5));
            Assert.That(_searchBuilder.Slice(0, 3).Search().Count(), Is.EqualTo(3));
            Assert.That(_searchBuilder.Slice(1, 3).Search().Count(), Is.EqualTo(3));
            Assert.That(_searchBuilder.Slice(3, 3).Search().Count(), Is.EqualTo(2));
        }

        [Test]
        public void ShouldSortByRelevance() {
            _provider.CreateIndex("default");
            _provider.Store("default", _provider.New(1).Add("body", "michael is in the kitchen").Analyze());
            _provider.Store("default", _provider.New(2).Add("body", "michael as a cousin named michel").Analyze());
            _provider.Store("default", _provider.New(3).Add("body", "speak inside the mic").Analyze());
            _provider.Store("default", _provider.New(4).Add("body", "a dog is pursuing a cat").Analyze());
            _provider.Store("default", _provider.New(5).Add("body", "the elephant can't catch up the dog").Analyze());

            var michael = _searchBuilder.WithField("body", "michael").Search().ToList();
            Assert.That(michael.Count(), Is.EqualTo(2));
            Assert.That(michael[0].Score >= michael[1].Score, Is.True);

            // Sorting on score is always descending
            michael = _searchBuilder.WithField("body", "michael").Ascending().Search().ToList();
            Assert.That(michael.Count(), Is.EqualTo(2));
            Assert.That(michael[0].Score >= michael[1].Score, Is.True);
        }

        [Test]
        public void ShouldSortByDate() {
            _provider.CreateIndex("default");
            _provider.Store("default", _provider.New(1).Add("date", new DateTime(2010, 05, 28, 12, 30, 15)).Store());
            _provider.Store("default", _provider.New(2).Add("date", new DateTime(2010, 05, 28, 12, 30, 30)).Store());
            _provider.Store("default", _provider.New(3).Add("date", new DateTime(2010, 05, 28, 12, 30, 45)).Store());

            var date = _searchBuilder.SortBy("date").Search().ToList();
            Assert.That(date.Count(), Is.EqualTo(3));
            Assert.That(date[0].GetDateTime("date") > date[1].GetDateTime("date"), Is.True);
            Assert.That(date[1].GetDateTime("date") > date[2].GetDateTime("date"), Is.True);

            date = _searchBuilder.SortBy("date").Ascending().Search().ToList();
            Assert.That(date.Count(), Is.EqualTo(3));
            Assert.That(date[0].GetDateTime("date") < date[1].GetDateTime("date"), Is.True);
            Assert.That(date[1].GetDateTime("date") < date[2].GetDateTime("date"), Is.True);
        }

        [Test]
        public void ShouldEscapeSpecialChars() {
            _provider.CreateIndex("default");
            _provider.Store("default", _provider.New(1).Add("body", "Orchard has been developped in C#").Analyze());
            _provider.Store("default", _provider.New(2).Add("body", "Windows has been developped in C++").Analyze());

            var cs = _searchBuilder.Parse("body", "C#").Search().ToList();
            Assert.That(cs.Count(), Is.EqualTo(2));

            var cpp = _searchBuilder.Parse("body", "C++").Search().ToList();
            Assert.That(cpp.Count(), Is.EqualTo(2));

        }

        [Test]
        public void ShouldHandleMandatoryFields() {
            _provider.CreateIndex("default");
            _provider.Store("default", _provider.New(1).Add("body", "Orchard has been developped in C#").Analyze());
            _provider.Store("default", _provider.New(2).Add("body", "Windows has been developped in C++").Analyze());

            Assert.That(_searchBuilder.WithField("body", "develop").Search().ToList().Count(), Is.EqualTo(2));
            Assert.That(_searchBuilder.WithField("body", "develop").WithField("body", "Orchard").Search().ToList().Count(), Is.EqualTo(2));
            Assert.That(_searchBuilder.WithField("body", "develop").WithField("body", "Orchard").Mandatory().Search().ToList().Count(), Is.EqualTo(1));
            Assert.That(_searchBuilder.WithField("body", "develop").WithField("body", "Orchard").Mandatory().Search().First().ContentItemId, Is.EqualTo(1));
        }

        [Test]
        public void ShouldHandleForbiddenFields() {
            _provider.CreateIndex("default");
            _provider.Store("default", _provider.New(1).Add("body", "Orchard has been developped in C#").Analyze());
            _provider.Store("default", _provider.New(2).Add("body", "Windows has been developped in C++").Analyze());

            Assert.That(_searchBuilder.WithField("body", "developped").Search().ToList().Count(), Is.EqualTo(2));
            Assert.That(_searchBuilder.WithField("body", "developped").WithField("body", "Orchard").Search().ToList().Count(), Is.EqualTo(2));
            Assert.That(_searchBuilder.WithField("body", "developped").WithField("body", "Orchard").Forbidden().Search().ToList().Count(), Is.EqualTo(1));
            Assert.That(_searchBuilder.WithField("body", "developped").WithField("body", "Orchard").Forbidden().Search().First().ContentItemId, Is.EqualTo(2));
        }

        [Test]
        public void ShouldHandleWeight() {
            _provider.CreateIndex("default");
            _provider.Store("default", _provider.New(1).Add("body", "Orchard has been developped in C#").Analyze());
            _provider.Store("default", _provider.New(2).Add("body", "Windows has been developped in C++").Analyze());

            Assert.That(_searchBuilder.WithField("body", "developped").WithField("body", "Orchard").Weighted(2).Search().First().ContentItemId, Is.EqualTo(1));
        }

        [Test]
        public void ShouldParseLuceneQueries() {
            _provider.CreateIndex("default");
            _provider.Store("default", _provider.New(1).Add("body", "Bradley is in the kitchen.").Analyze().Add("title", "Beer and takos").Analyze());
            _provider.Store("default", _provider.New(2).Add("body", "Renaud is also in the kitchen.").Analyze().Add("title", "A love affair").Analyze());
            _provider.Store("default", _provider.New(3).Add("body", "Bertrand is a little bit jealous.").Analyze().Add("title", "Soap opera").Analyze());

            Assert.That(_searchBuilder.Parse(new[] { "body" }, "kitchen").Count(), Is.EqualTo(2));
            Assert.That(_searchBuilder.Parse(new[] { "body" }, "kitchen bertrand").Count(), Is.EqualTo(3));
            Assert.That(_searchBuilder.Parse(new[] { "body" }, "kitchen +bertrand").Count(), Is.EqualTo(1));
            Assert.That(_searchBuilder.Parse(new[] { "body" }, "+kitchen +bertrand").Count(), Is.EqualTo(0));
            Assert.That(_searchBuilder.Parse(new[] { "body" }, "kit").Count(), Is.EqualTo(0));
            Assert.That(_searchBuilder.Parse(new[] { "body" }, "kit*").Count(), Is.EqualTo(2));
            Assert.That(_searchBuilder.Parse(new[] { "body", "title" }, "bradley love^3 soap").Count(), Is.EqualTo(3));
            Assert.That(_searchBuilder.Parse(new[] { "body", "title" }, "bradley love^3 soap").Search().First().ContentItemId, Is.EqualTo(2));
        }

        [Test]
        public void ShouldFilterIntValues() {
            _provider.CreateIndex("default");
            _provider.Store("default", _provider.New(1).Add("field", 1));
            _provider.Store("default", _provider.New(2).Add("field", 22));
            _provider.Store("default", _provider.New(3).Add("field", 333));

            Assert.That(_searchBuilder.WithField("field", 1).ExactMatch().Count(), Is.EqualTo(1));
            Assert.That(_searchBuilder.WithField("field", 22).ExactMatch().Count(), Is.EqualTo(1));
            Assert.That(_searchBuilder.WithField("field", 333).ExactMatch().Count(), Is.EqualTo(1));

            Assert.That(_searchBuilder.WithField("field", 0).ExactMatch().Count(), Is.EqualTo(0));
            Assert.That(_searchBuilder.WithField("field", 2).ExactMatch().Count(), Is.EqualTo(0));
            Assert.That(_searchBuilder.WithField("field", 3).ExactMatch().Count(), Is.EqualTo(0));
        }

        [Test]
        public void ShouldFilterStoredIntValues() {
            _provider.CreateIndex("default");
            _provider.Store("default", _provider.New(1).Add("field", 1).Store());
            _provider.Store("default", _provider.New(2).Add("field", 22).Store());
            _provider.Store("default", _provider.New(3).Add("field", 333).Store());

            Assert.That(_searchBuilder.WithField("field", 1).ExactMatch().Count(), Is.EqualTo(1));
            Assert.That(_searchBuilder.WithField("field", 22).ExactMatch().Count(), Is.EqualTo(1));
            Assert.That(_searchBuilder.WithField("field", 333).ExactMatch().Count(), Is.EqualTo(1));

            Assert.That(_searchBuilder.WithField("field", 0).ExactMatch().Count(), Is.EqualTo(0));
            Assert.That(_searchBuilder.WithField("field", 2).ExactMatch().Count(), Is.EqualTo(0));
            Assert.That(_searchBuilder.WithField("field", 3).ExactMatch().Count(), Is.EqualTo(0));
        }

        [Test]
        public void ShouldProvideAvailableFields() {
            _provider.CreateIndex("default");
            _provider.Store("default", _provider.New(1).Add("a", "Home").Analyze());
            _provider.Store("default", _provider.New(2).Add("b", DateTime.Now).Store());
            _provider.Store("default", _provider.New(3).Add("c", 333));

            Assert.That(_provider.GetFields("default").Count(), Is.EqualTo(4));
            Assert.That(_provider.GetFields("default").OrderBy(s => s).ToArray(), Is.EqualTo(new [] { "a", "b", "c", "id"}));
        }

        [Test]
        public void FiltersShouldNotAlterResults() {
            _provider.CreateIndex("default");
            _provider.Store("default", _provider.New(1).Add("body", "Orchard has been developped by Mirosoft in C#").Analyze().Add("culture", 1033));
            _provider.Store("default", _provider.New(2).Add("body", "Windows a été développé par Mirosoft en C++").Analyze().Add("culture", 1036));
            _provider.Store("default", _provider.New(3).Add("title", "Home").Analyze().Add("culture", 1033));

            Assert.That(_searchBuilder.WithField("body", "Mirosoft").Count(), Is.EqualTo(2));
            Assert.That(_searchBuilder.WithField("body", "Mirosoft").WithField("culture", 1033).Count(), Is.EqualTo(3));
            Assert.That(_searchBuilder.WithField("body", "Mirosoft").WithField("culture", 1033).AsFilter().Count(), Is.EqualTo(1));
            
            Assert.That(_searchBuilder.WithField("body", "Orchard").WithField("culture", 1036).Count(), Is.EqualTo(2));
            Assert.That(_searchBuilder.WithField("body", "Orchard").WithField("culture", 1036).AsFilter().Count(), Is.EqualTo(0));

            Assert.That(_searchBuilder.WithField("culture", 1033).Count(), Is.EqualTo(2));
            Assert.That(_searchBuilder.WithField("culture", 1033).AsFilter().Count(), Is.EqualTo(2));
            
            Assert.That(_searchBuilder.WithField("body", "blabla").WithField("culture", 1033).Count(), Is.EqualTo(2));
            Assert.That(_searchBuilder.WithField("body", "blabla").WithField("culture", 1033).AsFilter().Count(), Is.EqualTo(0));

            Assert.That(_searchBuilder.Parse("title", "home").Count(), Is.EqualTo(1));
            Assert.That(_searchBuilder.Parse("title", "home").WithField("culture", 1033).AsFilter().Count(), Is.EqualTo(1));

        }
    }
}
