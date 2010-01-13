using System.Collections.Generic;
using System.Linq;
using Orchard.Settings;

namespace Orchard.UI.PageTitle {
    public class PageTitleBuilder : IPageTitleBuilder {
        private readonly ISiteService _siteService;
        private readonly List<string> _titleParts;
        private readonly string _titleSeparator;

        public PageTitleBuilder(ISiteService siteService) {
            _siteService = siteService;
            _titleParts = new List<string>(5);
            _titleSeparator = _siteService.GetSiteSettings().PageTitleSeparator;
        }

        public void AddTitleParts(params string[] titleParts) {
            if (titleParts != null)
                foreach (string titlePart in titleParts)
                    if (!string.IsNullOrEmpty(titlePart))
                        _titleParts.Add(titlePart);
        }

        public void AppendTitleParts(params string[] titleParts) {
            if (titleParts != null)
                foreach (string titlePart in titleParts)
                    if (!string.IsNullOrEmpty(titlePart))
                        _titleParts.Insert(0, titlePart);
        }

        public string GenerateTitle() {
            return string.Join(_titleSeparator, _titleParts.AsEnumerable().Reverse().ToArray());
        }
    }
}