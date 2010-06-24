﻿using System.Xml;
using System.Xml.Linq;
using Orchard.ContentManagement.MetaData.Builders;
using Orchard.ContentManagement.MetaData.Models;

namespace Orchard.ContentManagement.MetaData.Services {
    public class ContentDefinitionReader : IContentDefinitionReader {
        private readonly IMapper<XElement, SettingsDictionary> _settingsReader;

        public ContentDefinitionReader(IMapper<XElement, SettingsDictionary> settingsReader) {
            _settingsReader = settingsReader;
        }

        public void Merge(XElement source, ContentTypeDefinitionBuilder builder) {
            builder.Named(XmlConvert.DecodeName(source.Name.LocalName));
            foreach (var setting in _settingsReader.Map(source)) {
                builder.WithSetting(setting.Key, setting.Value);
            }
            foreach (var iter in source.Elements()) {
                var partElement = iter;
                builder.WithPart(
                    XmlConvert.DecodeName(partElement.Name.LocalName),
                    partBuilder => {
                        foreach (var setting in _settingsReader.Map(partElement)) {
                            partBuilder.WithSetting(setting.Key, setting.Value);
                        }
                    });
            }
        }

        public void Merge(XElement source, ContentPartDefinitionBuilder builder) {
            builder.Named(XmlConvert.DecodeName(source.Name.LocalName));
            foreach (var setting in _settingsReader.Map(source)) {
                builder.WithSetting(setting.Key, setting.Value);
            }

            foreach (var iter in source.Elements()) {
                var fieldElement = iter;
                var fieldParameters = XmlConvert.DecodeName(fieldElement.Name.LocalName).Split('.');
                builder.WithField(
                    fieldParameters[0],
                    fieldBuilder => {
                        fieldBuilder.OfType(fieldParameters[1]);
                        foreach (var setting in _settingsReader.Map(fieldElement)) {
                            fieldBuilder.WithSetting(setting.Key, setting.Value);
                        }
                    });
            }
        }
    }
}