using Orchard.Blogs.Models;
using Orchard.ContentManagement.ViewModels;
using Orchard.Mvc.ViewModels;

namespace Orchard.Blogs.ViewModels {
    public class BlogPostEditViewModel : AdminViewModel {
        public ItemEditorModel<BlogPost> BlogPost { get; set; }
    }
}