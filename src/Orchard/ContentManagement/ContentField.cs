﻿using System;
using Orchard.ContentManagement.FieldStorage;
using Orchard.ContentManagement.MetaData.Models;

namespace Orchard.ContentManagement {
    public class ContentField {
        public string Name { get { return PartFieldDefinition.Name; } }

        public ContentPartDefinition.Field PartFieldDefinition { get; set; }
        public ContentFieldDefinition FieldDefinition { get { return PartFieldDefinition.FieldDefinition; } }

        public IFieldStorage Storage { protected get; set; }
    }
}
