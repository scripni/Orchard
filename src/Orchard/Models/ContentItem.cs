using System;
using System.Collections.Generic;
using System.Linq;
using Orchard.Models.Records;

namespace Orchard.Models {
    public class ContentItem : IContent {
        public ContentItem() {
            _parts = new List<ContentPart>();
        }


        private readonly IList<ContentPart> _parts;
        ContentItem IContent.ContentItem { get { return this; } }

        public int Id { get; set; }
        public string ContentType { get; set; }
        public ContentItemRecord Record { get; set; }

        public IEnumerable<ContentPart> Parts { get { return _parts; } }

        public IContentManager ContentManager { get; set; }

        public bool Has(Type partType) {
            return partType==typeof(ContentItem) || _parts.Any(part => partType.IsAssignableFrom(part.GetType()));
        }

        public IContent Get(Type partType) {
            if (partType == typeof(ContentItem))
                return this;
            return _parts.FirstOrDefault(part => partType.IsAssignableFrom(part.GetType()));
        }

        public void Weld(ContentPart part) {
            part.ContentItem = this;
            _parts.Add(part);
        }
    }
}