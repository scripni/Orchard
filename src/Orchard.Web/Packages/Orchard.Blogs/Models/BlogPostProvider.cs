using System.Collections.Generic;
using System.Linq;
using System.Web.Routing;
using Orchard.Blogs.Services;
using Orchard.Core.Common.Models;
using Orchard.Data;
using Orchard.Models;
using Orchard.Models.Driver;
using Orchard.Models.ViewModels;

namespace Orchard.Blogs.Models {
    public class BlogPostProvider : ContentProvider {
        public override IEnumerable<ContentType> GetContentTypes() {
            return new[] { BlogPost.ContentType };
        }

        public BlogPostProvider(
            IRepository<BlogPostRecord> repository,
            IContentManager contentManager,
            IBlogPostService blogPostService) {

            Filters.Add(new ActivatingFilter<BlogPost>("blogpost"));
            Filters.Add(new ActivatingFilter<CommonAspect>("blogpost"));
            Filters.Add(new ActivatingFilter<RoutableAspect>("blogpost"));
            Filters.Add(new ActivatingFilter<BodyAspect>("blogpost"));
            Filters.Add(new StorageFilter<BlogPostRecord>(repository));
            Filters.Add(new ContentItemTemplates<BlogPost>("BlogPost", "Detail", "Summary"));

            OnLoaded<BlogPost>((context, bp) => bp.Blog = contentManager.Get<Blog>(bp.Record.Blog.Id));

            OnGetItemMetadata<BlogPost>((context, bp) => {
                context.Metadata.DisplayText = bp.Title;
                context.Metadata.DisplayRouteValues =
                    new RouteValueDictionary(
                        new {
                            area = "Orchard.Blogs",
                            controller = "BlogPost",
                            action = "Item",
                            blogSlug = bp.Blog.Slug,
                            postSlug = bp.Slug
                        });
                context.Metadata.EditorRouteValues =
                    new RouteValueDictionary(
                        new {
                            area = "Orchard.Blogs",
                            controller = "BlogPost",
                            action = "Edit",
                            blogSlug = bp.Blog.Slug,
                            postSlug = bp.Slug
                        });
            });

            OnGetDisplayViewModel<Blog>((context, blog) => {
                if (context.DisplayType != "Detail") {
                    return;
                }

                var posts = blogPostService.Get(blog);
                var viewModels = posts.Select(
                    bp => contentManager.GetDisplayViewModel(bp, null, "Summary"));
                context.AddDisplay(new TemplateViewModel(viewModels) { TemplateName = "BlogPostList", ZoneName = "body" });
            });
        }


    }
}