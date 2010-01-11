﻿using System.Collections.Generic;
using System.Linq;
using Orchard.Security.Permissions;

namespace Orchard.Blogs {
    public class Permissions : IPermissionProvider {
        public static readonly Permission ViewPost = new Permission { Description = "Viewing Blog Posts", Name = "ViewPosts" };
        public static readonly Permission CreatePost = new Permission { Description = "Creating Blog Posts", Name = "CreatePost" };
        public static readonly Permission CreateDraft = new Permission { Description = "Creating a Draft of a Blog Post", Name = "CreateDraft" };
        public static readonly Permission ModifyPost = new Permission { Description = "Mofifying a Blog Post", Name = "ModifyPost" };
        public static readonly Permission DeletePost = new Permission { Description = "Deleting a Blog Post", Name = "DeletePost" };
        public static readonly Permission PublishPost = new Permission { Description = "Publishing a Blog Post", Name = "PublishPost" };
        public static readonly Permission UnpublishPost = new Permission { Description = "Unpublishing a Blog Post", Name = "UnpublishPost" };
        public static readonly Permission SchedulePost = new Permission { Description = "Scheduling a Blog Post", Name = "SchedulePost" };
        public static readonly Permission CreateBlog = new Permission { Description = "Creating a Blog", Name = "CreateBlog" };
        public static readonly Permission ModifyBlog = new Permission { Description = "Mofifying a Blog", Name = "ModifyBlog" };
        public static readonly Permission DeleteBlog = new Permission { Description = "Deleting a Blog", Name = "DeleteBlog" };

        public string PackageName {
            get {
                return "Blogs";
            }
        }

        public IEnumerable<Permission> GetPermissions() {
            return new List<Permission> {
                ViewPost,
                CreatePost,
                CreateDraft,
                ModifyPost,
                DeletePost,
                PublishPost,
                UnpublishPost,
                SchedulePost,
                CreateBlog,
                ModifyBlog,
                DeleteBlog
            };
        }

        public IEnumerable<PermissionStereotype> GetDefaultStereotypes() {
            return Enumerable.Empty<PermissionStereotype>();
        }
    }
}


