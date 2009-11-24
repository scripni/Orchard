using Orchard.Core.Common.Models;
using Orchard.Data;
using Orchard.Models;
using Orchard.Models.Driver;

namespace Orchard.Blogs.Models {
    public class BlogPostProvider : ContentProvider {
        public BlogPostProvider(IRepository<BlogPostRecord> repository, IContentManager contentManager) {
            Filters.Add(new ActivatingFilter<BlogPost>("blogpost"));
            Filters.Add(new ActivatingFilter<CommonAspect>("blogpost"));
            Filters.Add(new ActivatingFilter<RoutableAspect>("blogpost"));
            Filters.Add(new ActivatingFilter<BodyAspect>("blogpost"));
            Filters.Add(new StorageFilter<BlogPostRecord>(repository));
            AddOnLoaded<BlogPost>((context, bp) => bp.Blog = contentManager.Get<Blog>(context.ContentItem.Id));
        }
    }
}