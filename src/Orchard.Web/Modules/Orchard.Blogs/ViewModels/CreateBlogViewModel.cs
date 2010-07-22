using Orchard.Blogs.Models;
using Orchard.Mvc.ViewModels;

namespace Orchard.Blogs.ViewModels {
    public class CreateBlogViewModel : BaseViewModel {
        public ContentItemViewModel<BlogPart> Blog { get; set; }
        public bool PromoteToHomePage { get; set; }
    }
}