﻿using System;
using System.Collections.Generic;
using Autofac;
using Autofac.Builder;
using JetBrains.Annotations;
using Moq;
using NUnit.Framework;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Drivers;
using Orchard.ContentManagement.Handlers;
using Orchard.ContentManagement.Records;
using Orchard.Core.Common.Models;
using Orchard.Core.Common.Providers;
using Orchard.Core.Common.Records;
using Orchard.Core.Common.Services;
using Orchard.Data;
using Orchard.Security;
using Orchard.Tests.Packages;

namespace Orchard.Core.Tests.Common.Services {
    [TestFixture]
    public class RoutableServiceTests : DatabaseEnabledTestsBase {
        [SetUp]
        public override void Init() {
            base.Init();
            _routableService = _container.Resolve<IRoutableService>();
        }

        public override void Register(ContainerBuilder builder) {
            builder.Register<DefaultContentManager>().As<IContentManager>();
            builder.Register<ThingHandler>().As<IContentHandler>();
            builder.Register<RoutableService>().As<IRoutableService>();
        }

        private IRoutableService _routableService;

        [Test]
        public void InvalidCharactersShouldBeReplacedByADash() {
            var contentManager = _container.Resolve<IContentManager>();

            var thing = contentManager.Create<Thing>(ThingDriver.ContentType.Name, t => {
                t.As<RoutableAspect>().Record = new RoutableRecord();
                t.Title = "Please do not use any of the following characters in your slugs: \":\", \"/\", \"?\", \"#\", \"[\", \"]\", \"@\", \"!\", \"$\", \"&\", \"'\", \"(\", \")\", \"*\", \"+\", \",\", \";\", \"=\"";
            });

            _routableService.FillSlug(thing.As<RoutableAspect>());

            Assert.That(thing.Slug, Is.EqualTo("Please-do-not-use-any-of-the-following-characters-in-your-slugs-\"-\"-\"-\"-\"-\"-\"-\"-\"-\"-\"-\"-\"-\"-\"-\"-\"-\"-\"-\"-\"-\"-\"-\"-\"-\"-\"-\"-\"-\"-\"-\"-\"-\"-\"-\""));
        }

        [Test]
        public void VeryLongStringTruncatedTo1000Chars() {
            var contentManager = _container.Resolve<IContentManager>();

            var veryVeryLongTitle = "this is a very long slug...";
            for (var i = 0; i < 100; i++)
                veryVeryLongTitle += "aaaaaaaaaa";

            var thing = contentManager.Create<Thing>(ThingDriver.ContentType.Name, t => {
                t.As<RoutableAspect>().Record = new RoutableRecord();
                t.Title = veryVeryLongTitle;
            });

            _routableService.FillSlug(thing.As<RoutableAspect>());

            Assert.That(veryVeryLongTitle.Length, Is.AtLeast(1001));
            Assert.That(thing.Slug.Length, Is.EqualTo(1000));
        }

        protected override IEnumerable<Type> DatabaseTypes {
            get {
                return new[] {
                                 typeof(RoutableRecord), 
                                 typeof(ContentItemRecord), 
                                 typeof(ContentItemVersionRecord), 
                                 typeof(ContentTypeRecord),
                                 typeof(CommonRecord),
                                 typeof(CommonVersionRecord),
                             };
            }
        }

        [UsedImplicitly]
        public class ThingHandler : ContentHandler {
            public ThingHandler()
            {
                Filters.Add(new ActivatingFilter<Thing>(ThingDriver.ContentType.Name));
                Filters.Add(new ActivatingFilter<ContentPart<CommonVersionRecord>>(ThingDriver.ContentType.Name));
                Filters.Add(new ActivatingFilter<CommonAspect>(ThingDriver.ContentType.Name));
                Filters.Add(new ActivatingFilter<RoutableAspect>(ThingDriver.ContentType.Name));
            }
        }

        public class Thing : ContentPart {
            public int Id { get { return ContentItem.Id; } }

            public string Title {
                get { return this.As<RoutableAspect>().Title; }
                set { this.As<RoutableAspect>().Title = value; }
            }

            public string Slug {
                get { return this.As<RoutableAspect>().Slug; }
                set { this.As<RoutableAspect>().Slug = value; }
            }
        }
        
        public class ThingDriver : ContentItemDriver<Thing> {
            public readonly static ContentType ContentType = new ContentType {
                Name = "thing",
                DisplayName = "Thing"
            };
        }
    }
}