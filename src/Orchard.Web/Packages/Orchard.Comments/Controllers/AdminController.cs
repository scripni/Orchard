using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using Orchard.Comments.Models;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.Models;
using Orchard.Settings;
using Orchard.UI.Notify;
using Orchard.Security;
using Orchard.Comments.ViewModels;
using Orchard.Comments.Services;

namespace Orchard.Comments.Controllers {
    [ValidateInput(false)]
    public class AdminController : Controller {
        private readonly ICommentService _commentService;
        private readonly IAuthorizer _authorizer;
        private readonly INotifier _notifier;

        public AdminController(ICommentService commentService, INotifier notifier, IAuthorizer authorizer) {
            _commentService = commentService;
            _authorizer = authorizer;
            _notifier = notifier;
            Logger = NullLogger.Instance;
            T = NullLocalizer.Instance;
        }

        public IUser CurrentUser { get; set; }
        public ISite CurrentSite { get; set; }

        public ILogger Logger { get; set; }
        public Localizer T { get; set; }

        public ActionResult Index(CommentIndexOptions options) {
            // Default options
            if (options == null)
                options = new CommentIndexOptions();

            // Filtering
            IEnumerable<Comment> comments;
            try {
                switch (options.Filter) {
                    case CommentIndexFilter.All:
                        comments = _commentService.GetComments();
                        break;
                    case CommentIndexFilter.Approved:
                        comments = _commentService.GetComments(CommentStatus.Approved);
                        break;
                    case CommentIndexFilter.Spam:
                        comments = _commentService.GetComments(CommentStatus.Spam);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                var entries = comments.Select(comment => CreateCommentEntry(comment)).ToList();
                var model = new CommentsIndexViewModel {Comments = entries, Options = options};
                return View(model);
            }
            catch (Exception exception) {
                _notifier.Error(T("Listing comments failed: " + exception.Message));
                return Index(options);
            }
        }

        [HttpPost]
        [FormValueRequired("submit.BulkEdit")]
        public ActionResult Index(FormCollection input) {
            var viewModel = new CommentsIndexViewModel { Comments = new List<CommentEntry>(), Options = new CommentIndexOptions() };
            UpdateModel(viewModel, input.ToValueProvider());

            try {
                IEnumerable<CommentEntry> checkedEntries = viewModel.Comments.Where(c => c.IsChecked);
                switch (viewModel.Options.BulkAction) {
                    case CommentIndexBulkAction.None:
                        break;
                    case CommentIndexBulkAction.MarkAsSpam:
                        if (!_authorizer.Authorize(Permissions.ModerateComment, T("Couldn't moderate comment")))
                            return new HttpUnauthorizedResult();
                        //TODO: Transaction
                        foreach (CommentEntry entry in checkedEntries) {
                            _commentService.MarkCommentAsSpam(entry.Comment.Id);
                        }
                        break;
                    case CommentIndexBulkAction.Delete:
                        if (!_authorizer.Authorize(Permissions.ModerateComment, T("Couldn't delete comment")))
                            return new HttpUnauthorizedResult();

                        foreach (CommentEntry entry in checkedEntries) {
                            _commentService.DeleteComment(entry.Comment.Id);
                        }
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception exception) {
                _notifier.Error(T("Editing comments failed: " + exception.Message));
                return Index(viewModel.Options);
            }

            return RedirectToAction("Index");
        }

        public ActionResult Create() {
            return View(new CommentsCreateViewModel());
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Create(FormCollection input, string returnUrl) {
            var viewModel = new CommentsCreateViewModel();
            try {
                UpdateModel(viewModel, input.ToValueProvider());
                if (CurrentSite.As<CommentSettings>().Record.RequireLoginToAddComment) {
                    if (!_authorizer.Authorize(Permissions.AddComment, T("Couldn't add comment")))
                        return new HttpUnauthorizedResult();
                }
                Comment comment = new Comment {
                    Author = viewModel.Name,
                    CommentDate = DateTime.Now,
                    CommentText = viewModel.CommentText,
                    Email = viewModel.Email,
                    SiteName = viewModel.SiteName,
                    UserName = CurrentUser == null ? "Anonymous" : CurrentUser.UserName,
                    CommentedOn = viewModel.CommentedOn
                };
                _commentService.CreateComment(comment);
                if (!String.IsNullOrEmpty(returnUrl)) {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index");
            }
            catch (Exception exception) {
                _notifier.Error(T("Creating Comment failed: " + exception.Message));
                return View(viewModel);
            }
        }

        public ActionResult Details(int id, CommentDetailsOptions options) {
            // Default options
            if (options == null)
                options = new CommentDetailsOptions();

            // Filtering
            IEnumerable<Comment> comments;
            try {
                switch (options.Filter) {
                    case CommentDetailsFilter.All:
                        comments = _commentService.GetCommentsForCommentedContent(id);
                        break;
                    case CommentDetailsFilter.Approved:
                        comments = _commentService.GetCommentsForCommentedContent(id, CommentStatus.Approved);
                        break;
                    case CommentDetailsFilter.Spam:
                        comments = _commentService.GetCommentsForCommentedContent(id, CommentStatus.Spam);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                var entries = comments.Select(comment => CreateCommentEntry(comment)).ToList();
                var model = new CommentsDetailsViewModel {
                    Comments = entries,
                    Options = options,
                    DisplayNameForCommentedItem = _commentService.GetDisplayForCommentedContent(id).DisplayText,
                    CommentedItemId = id,
                    CommentsClosedOnItem = _commentService.CommentsClosedForCommentedContent(id),
                };
                return View(model);
            }
            catch (Exception exception) {
                _notifier.Error(T("Listing comments failed: " + exception.Message));
                return Index(new CommentIndexOptions());
            }
        }

        [HttpPost]
        [FormValueRequired("submit.BulkEdit")]
        public ActionResult Details(FormCollection input) {
            var viewModel = new CommentsDetailsViewModel { Comments = new List<CommentEntry>(), Options = new CommentDetailsOptions() };
            UpdateModel(viewModel, input.ToValueProvider());

            try {
                IEnumerable<CommentEntry> checkedEntries = viewModel.Comments.Where(c => c.IsChecked);
                switch (viewModel.Options.BulkAction) {
                    case CommentDetailsBulkAction.None:
                        break;
                    case CommentDetailsBulkAction.MarkAsSpam:
                        if (!_authorizer.Authorize(Permissions.ModerateComment, T("Couldn't moderate comment")))
                            return new HttpUnauthorizedResult();
                        //TODO: Transaction
                        foreach (CommentEntry entry in checkedEntries) {
                            _commentService.MarkCommentAsSpam(entry.Comment.Id);
                        }
                        break;
                    case CommentDetailsBulkAction.Delete:
                        if (!_authorizer.Authorize(Permissions.ModerateComment, T("Couldn't delete comment")))
                            return new HttpUnauthorizedResult();

                        foreach (CommentEntry entry in checkedEntries) {
                            _commentService.DeleteComment(entry.Comment.Id);
                        }
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception exception) {
                _notifier.Error(T("Editing comments failed: " + exception.Message));
                return Details(viewModel.CommentedItemId, viewModel.Options);
            }

            return RedirectToAction("Details", new { viewModel.CommentedItemId, viewModel.Options });
        }

        public ActionResult Close(int commentedItemId) {
            try {
                if (!_authorizer.Authorize(Permissions.CloseComment, T("Couldn't close comments")))
                    return new HttpUnauthorizedResult();
                _commentService.CloseCommentsForCommentedContent(commentedItemId);
                return RedirectToAction("Index");
            }
            catch (Exception exception) {
                _notifier.Error(T("Closing Comments failed: " + exception.Message));
                return RedirectToAction("Index");
            }
        }

        public ActionResult Enable(int commentedItemId) {
            try {
                if (!_authorizer.Authorize(Permissions.EnableComment, T("Couldn't enable comments")))
                    return new HttpUnauthorizedResult();
                _commentService.EnableCommentsForCommentedContent(commentedItemId);
                return RedirectToAction("Index");
            }
            catch (Exception exception) {
                _notifier.Error(T("Enabling Comments failed: " + exception.Message));
                return RedirectToAction("Index");
            }
        }

        public ActionResult Edit(int id) {
            try {
                Comment comment = _commentService.GetComment(id);
                var viewModel = new CommentsEditViewModel {
                    CommentText = comment.CommentText,
                    Email = comment.Email,
                    Id = comment.Id,
                    Name = comment.Author,
                    SiteName = comment.SiteName
                };
                return View(viewModel);

            }
            catch (Exception exception) {
                _notifier.Error(T("Editing comment failed: " + exception.Message));
                return Index(new CommentIndexOptions());
            }
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Edit(FormCollection input) {
            var viewModel = new CommentsEditViewModel();
            try {
                UpdateModel(viewModel, input.ToValueProvider());
                if (!_authorizer.Authorize(Permissions.ModerateComment, T("Couldn't edit comment")))
                    return new HttpUnauthorizedResult();

                _commentService.UpdateComment(viewModel.Id, viewModel.Name, viewModel.Email, viewModel.SiteName, viewModel.CommentText);
                return RedirectToAction("Index");
            }
            catch (Exception exception) {
                _notifier.Error(T("Editing Comment failed: " + exception.Message));
                return View(viewModel);
            }
        }

        private CommentEntry CreateCommentEntry(Comment comment) {
            return new CommentEntry {
                Comment = comment,
                CommentedOn = _commentService.GetDisplayForCommentedContent(comment.CommentedOn).DisplayText,
                IsChecked = false,
            };
        }

        public class FormValueRequiredAttribute : ActionMethodSelectorAttribute {
            private readonly string _submitButtonName;

            public FormValueRequiredAttribute(string submitButtonName) {
                _submitButtonName = submitButtonName;
            }

            public override bool IsValidForRequest(ControllerContext controllerContext, MethodInfo methodInfo) {
                var value = controllerContext.HttpContext.Request.Form[_submitButtonName];
                return !string.IsNullOrEmpty(value);
            }
        }
    }
}
