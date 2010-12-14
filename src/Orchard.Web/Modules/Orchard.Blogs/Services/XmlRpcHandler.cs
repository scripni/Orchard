﻿using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.Routing;
using System.Xml.Linq;
using JetBrains.Annotations;
using Orchard.Blogs.Models;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Aspects;
using Orchard.Core.Common.Models;
using Orchard.Core.Routable.Models;
using Orchard.Core.Routable.Services;
using Orchard.Core.XmlRpc;
using Orchard.Core.XmlRpc.Models;
using Orchard.Environment.Extensions;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.Mvc.Extensions;
using Orchard.Security;
using Orchard.Blogs.Extensions;

namespace Orchard.Blogs.Services {
    [UsedImplicitly]
    [OrchardFeature("Orchard.Blogs.RemotePublishing")]
    public class XmlRpcHandler : IXmlRpcHandler {
        private readonly IBlogService _blogService;
        private readonly IBlogPostService _blogPostService;
        private readonly IContentManager _contentManager;
        private readonly IAuthorizationService _authorizationService;
        private readonly IMembershipService _membershipService;
        private readonly IRoutableService _routableService;
        private readonly RouteCollection _routeCollection;

        public XmlRpcHandler(IBlogService blogService, IBlogPostService blogPostService, IContentManager contentManager,
            IAuthorizationService authorizationService, IMembershipService membershipService, IRoutableService routableService,
            RouteCollection routeCollection) {
            _blogService = blogService;
            _blogPostService = blogPostService;
            _contentManager = contentManager;
            _authorizationService = authorizationService;
            _membershipService = membershipService;
            _routableService = routableService;
            _routeCollection = routeCollection;
            Logger = NullLogger.Instance;
            T = NullLocalizer.Instance;
        }

        public ILogger Logger { get; set; }
        public Localizer T { get; set; }

        public void SetCapabilities(XElement options) {
            const string manifestUri = "http://schemas.microsoft.com/wlw/manifest/weblog";
            options.SetElementValue(XName.Get("supportsSlug", manifestUri), "Yes");
        }

        public void Process(XmlRpcContext context) {
            var urlHelper = new UrlHelper(context.ControllerContext.RequestContext, _routeCollection);

            if (context.Request.MethodName == "blogger.getUsersBlogs") {
                var result = MetaWeblogGetUserBlogs(urlHelper,
                    Convert.ToString(context.Request.Params[0].Value),
                    Convert.ToString(context.Request.Params[1].Value),
                    Convert.ToString(context.Request.Params[2].Value));

                context.Response = new XRpcMethodResponse().Add(result);
            }

            if (context.Request.MethodName == "metaWeblog.getRecentPosts") {
                var result = MetaWeblogGetRecentPosts(urlHelper,
                    Convert.ToString(context.Request.Params[0].Value),
                    Convert.ToString(context.Request.Params[1].Value),
                    Convert.ToString(context.Request.Params[2].Value),
                    Convert.ToInt32(context.Request.Params[3].Value));

                context.Response = new XRpcMethodResponse().Add(result);
            }

            if (context.Request.MethodName == "metaWeblog.newPost") {
                var result = MetaWeblogNewPost(
                    Convert.ToString(context.Request.Params[0].Value),
                    Convert.ToString(context.Request.Params[1].Value),
                    Convert.ToString(context.Request.Params[2].Value),
                    (XRpcStruct)context.Request.Params[3].Value,
                    Convert.ToBoolean(context.Request.Params[4].Value),
                    context._drivers);

                context.Response = new XRpcMethodResponse().Add(result);
            }

            if (context.Request.MethodName == "metaWeblog.getPost") {
                var result = MetaWeblogGetPost(
                    urlHelper,
                    Convert.ToInt32(context.Request.Params[0].Value),
                    Convert.ToString(context.Request.Params[1].Value),
                    Convert.ToString(context.Request.Params[2].Value),
                    context._drivers);
                context.Response = new XRpcMethodResponse().Add(result);
            }

            if (context.Request.MethodName == "metaWeblog.editPost") {
                var result = MetaWeblogEditPost(
                    Convert.ToInt32(context.Request.Params[0].Value),
                    Convert.ToString(context.Request.Params[1].Value),
                    Convert.ToString(context.Request.Params[2].Value),
                    (XRpcStruct)context.Request.Params[3].Value,
                    Convert.ToBoolean(context.Request.Params[4].Value),
                    context._drivers);
                context.Response = new XRpcMethodResponse().Add(result);
            }

            if (context.Request.MethodName == "blogger.deletePost") {
                var result = MetaWeblogDeletePost(
                    Convert.ToString(context.Request.Params[0].Value),
                    Convert.ToString(context.Request.Params[1].Value),
                    Convert.ToString(context.Request.Params[2].Value),
                    Convert.ToString(context.Request.Params[3].Value),
                    Convert.ToBoolean(context.Request.Params[4].Value),
                    context._drivers);
                context.Response = new XRpcMethodResponse().Add(result);
            }
        }

        private XRpcArray MetaWeblogGetUserBlogs(UrlHelper urlHelper,
            string appkey,
            string userName,
            string password) {

            IUser user = ValidateUser(userName, password);

            // User needs to at least have permission to edit its own blog posts to access the service
            _authorizationService.CheckAccess(Permissions.EditOwnBlogPost, user, null);

            XRpcArray array = new XRpcArray();
            foreach (BlogPart blog in _blogService.Get()) {
                BlogPart blogPart = blog;
                array.Add(new XRpcStruct()
                                .Set("url", urlHelper.AbsoluteAction(() => urlHelper.Blog(blogPart)))
                                .Set("blogid", blog.Id)
                                .Set("blogName", blog.Name));
            }

            return array;
        }

        private XRpcArray MetaWeblogGetRecentPosts(
            UrlHelper urlHelper,
            string blogId,
            string userName,
            string password,
            int numberOfPosts) {

            IUser user = ValidateUser(userName, password);

            // User needs to at least have permission to edit its own blog posts to access the service
            _authorizationService.CheckAccess(Permissions.EditOwnBlogPost, user, null);

            BlogPart blog = _contentManager.Get<BlogPart>(Convert.ToInt32(blogId));
            if (blog == null) {
                throw new ArgumentException();
            }

            var array = new XRpcArray();
            foreach (var blogPost in _blogPostService.Get(blog, 0, numberOfPosts, VersionOptions.Latest)) {
                array.Add(CreateBlogStruct(blogPost, urlHelper));
            }
            return array;
        }

        private int MetaWeblogNewPost(
            string blogId,
            string userName,
            string password,
            XRpcStruct content,
            bool publish,
            IEnumerable<IXmlRpcDriver> drivers) {

            IUser user = ValidateUser(userName, password);

            // User needs permission to edit or publish its own blog posts
            _authorizationService.CheckAccess(publish ? Permissions.PublishOwnBlogPost : Permissions.EditOwnBlogPost, user, null);

            BlogPart blog = _contentManager.Get<BlogPart>(Convert.ToInt32(blogId));
            if (blog == null)
                throw new ArgumentException();

            var title = content.Optional<string>("title");
            var description = content.Optional<string>("description");
            var slug = content.Optional<string>("wp_slug");

            var blogPost = _contentManager.New<BlogPostPart>("BlogPost");

            // BodyPart
            if (blogPost.Is<BodyPart>()) {
                blogPost.As<BodyPart>().Text = description;
            }

            //CommonPart
            if (blogPost.Is<ICommonPart>()) {
                blogPost.As<ICommonPart>().Owner = user;
                blogPost.As<ICommonPart>().Container = blog;
            }

            //RoutePart
            if (blogPost.Is<RoutePart>()) {
                blogPost.As<RoutePart>().Title = title;
                blogPost.As<RoutePart>().Slug = slug;
                _routableService.FillSlugFromTitle(blogPost.As<RoutePart>());
                blogPost.As<RoutePart>().Path = blogPost.As<RoutePart>().GetPathWithSlug(blogPost.As<RoutePart>().Slug);
            }

            _contentManager.Create(blogPost.ContentItem, VersionOptions.Draft);

            if (publish)
                _blogPostService.Publish(blogPost);

            foreach (var driver in drivers)
                driver.Process(blogPost.Id);

            return blogPost.Id;
        }

        private XRpcStruct MetaWeblogGetPost(
            UrlHelper urlHelper,
            int postId,
            string userName,
            string password,
            IEnumerable<IXmlRpcDriver> drivers) {

            IUser user = ValidateUser(userName, password);
            var blogPost = _blogPostService.Get(postId, VersionOptions.Latest);
            if (blogPost == null)
                throw new ArgumentException();

            _authorizationService.CheckAccess(Permissions.EditOthersBlogPost, user, blogPost);

            var postStruct = CreateBlogStruct(blogPost, urlHelper);

            foreach (var driver in drivers)
                driver.Process(postStruct);

            return postStruct;
        }

        private bool MetaWeblogEditPost(int postId, string userName, string password, XRpcStruct content, bool publish, IEnumerable<IXmlRpcDriver> drivers) {
            IUser user = ValidateUser(userName, password);
            var blogPost = _blogPostService.Get(postId, VersionOptions.DraftRequired);
            if (blogPost == null)
                throw new ArgumentException();

            _authorizationService.CheckAccess(publish ? Permissions.PublishOthersBlogPost : Permissions.EditOthersBlogPost, user, blogPost);

            var title = content.Optional<string>("title");
            var description = content.Optional<string>("description");
            var slug = content.Optional<string>("wp_slug");

            blogPost.Title = title;
            blogPost.Slug = slug;
            blogPost.Text = description;

            if (publish)
                _blogPostService.Publish(blogPost);

            foreach (var driver in drivers)
                driver.Process(blogPost.Id);

            return true;
        }

        private bool MetaWeblogDeletePost(string appkey, string postId, string userName, string password, bool publish, IEnumerable<IXmlRpcDriver> drivers) {
            IUser user = ValidateUser(userName, password);
            var blogPost = _blogPostService.Get(Convert.ToInt32(postId), VersionOptions.Latest);
            if (blogPost == null)
                throw new ArgumentException();

            _authorizationService.CheckAccess(Permissions.DeleteOthersBlogPost, user, blogPost);

            foreach (var driver in drivers)
                driver.Process(blogPost.Id);

            _blogPostService.Delete(blogPost);
            return true;
        }

        private IUser ValidateUser(string userName, string password) {
            IUser user = _membershipService.ValidateUser(userName, password);
            if (user == null) {
                throw new OrchardCoreException(T("The username or e-mail or password provided is incorrect."));
            }

            return user;
        }

        private static XRpcStruct CreateBlogStruct(BlogPostPart blogPostPart, UrlHelper urlHelper) {
            var url = urlHelper.AbsoluteAction(() => urlHelper.BlogPost(blogPostPart));
            return new XRpcStruct()
                .Set("postid", blogPostPart.Id)
                .Set("dateCreated", blogPostPart.PublishedUtc)
                .Set("title", blogPostPart.Title)
                .Set("wp_slug", blogPostPart.Slug)
                .Set("description", blogPostPart.Text)
                .Set("link", url)
                .Set("permaLink", url);
        }
    }
}
