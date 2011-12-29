using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Orchard.Blogs.Extensions;
using Orchard.Blogs.Routing;
using Orchard.Blogs.Services;
using Orchard.Core.Feeds;
using Orchard.DisplayManagement;
using Orchard.Logging;
using Orchard.Mvc;
using Orchard.Services;
using Orchard.Themes;
using Orchard.UI.Navigation;
using Orchard.Settings;
using Orchard.ContentManagement;
using Orchard.Blogs.Models;

namespace Orchard.Blogs.Controllers {

    [Themed]
    public class BlogController : Controller {
        private readonly IOrchardServices _services;
        private readonly IBlogService _blogService;
        private readonly IBlogPostService _blogPostService;
        private readonly IFeedManager _feedManager;
        private readonly IWorkContextAccessor _workContextAccessor;
        private readonly ISiteService _siteService;

        public BlogController(
            IOrchardServices services, 
            IBlogService blogService,
            IBlogPostService blogPostService,
            IFeedManager feedManager, 
            IShapeFactory shapeFactory,
            IWorkContextAccessor workContextAccessor,
            ISiteService siteService) {
            _services = services;
            _blogService = blogService;
            _blogPostService = blogPostService;
            _feedManager = feedManager;
            _workContextAccessor = workContextAccessor;
            _siteService = siteService;
            Logger = NullLogger.Instance;
            Shape = shapeFactory;
        }

        dynamic Shape { get; set; }
        protected ILogger Logger { get; set; }

        public ActionResult List() {
            var blogs = _blogService.Get().Select(b => _services.ContentManager.BuildDisplay(b, "Summary"));

            var list = Shape.List();
            list.AddRange(blogs);

            dynamic viewModel = Shape.ViewModel()
                .ContentItems(list);

            // Casting to avoid invalid (under medium trust) reflection over the protected View method and force a static invocation.
            return View((object)viewModel);
        }

        public ActionResult Item(int blogId, PagerParameters pagerParameters) {
            // TODO: (PH:Autoroute) Should use Containers so we can lose this action and rely on ContainerPartDriver instead
            Pager pager = new Pager(_siteService.GetSiteSettings(), pagerParameters);

            var blogPart = _blogService.Get(blogId, VersionOptions.Published).As<BlogPart>();
            if (blogPart == null)
                return HttpNotFound();

            _feedManager.Register(blogPart);
            var blogPosts = _blogPostService.Get(blogPart, pager.GetStartIndex(), pager.PageSize)
                .Select(b => _services.ContentManager.BuildDisplay(b, "Summary"));
            dynamic blog = _services.ContentManager.BuildDisplay(blogPart);

            var list = Shape.List();
            list.AddRange(blogPosts);
            blog.Content.Add(Shape.Parts_Blogs_BlogPost_List(ContentItems: list), "5");

            var totalItemCount = _blogPostService.PostCount(blogPart);
            blog.Content.Add(Shape.Pager(pager).TotalItemCount(totalItemCount), "Content:after");

            return new ShapeResult(this, blog);
        }
    }
}