﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using NUnit.Framework;
using Orchard.ContentManagement.MetaData;
using Orchard.ContentManagement.MetaData.Builders;
using Orchard.ContentManagement.MetaData.Services;

namespace Orchard.Tests.ContentManagement.MetaData.Services {
    [TestFixture]
    public class ContentDefinitionReaderTests {
        private IContentDefinitionReader _reader;

        [SetUp]
        public void Init() {
            _reader = new ContentDefinitionReader(new SettingsFormatter());
        }

        [Test]
        public void ReadingElementSetsName() {
            var builder = new ContentTypeDefinitionBuilder();
            _reader.Merge(new XElement("foo"), builder);
            var type = builder.Build();
            Assert.That(type.Name, Is.EqualTo("foo"));
        }


        [Test]
        public void AttributesAreAppliedAsSettings() {
            var builder = new ContentTypeDefinitionBuilder();
            _reader.Merge(new XElement("foo", new XAttribute("x", "1")), builder);
            var type = builder.Build();
            Assert.That(type.Settings["x"], Is.EqualTo("1"));
        }

        [Test]
        public void ChildElementsAreAddedAsPartsWithSettings() {
            var builder = new ContentTypeDefinitionBuilder();
            _reader.Merge(new XElement("foo", new XElement("bar", new XAttribute("y", "2"))), builder);
            var type = builder.Build();
            Assert.That(type.Parts.Single().PartDefinition.Name, Is.EqualTo("bar"));
            Assert.That(type.Parts.Single().Settings["y"], Is.EqualTo("2"));
        }

        [Test, Ignore("Parts can be removed by name")]
        public void PartsCanBeRemovedByNameWhenImporting() {
            Assert.Fail();
        }
    }
}
