﻿using System;
using System.Linq;
using System.Web.Mvc;
using Orchard.ContentManagement;
using Orchard.ContentManagement.MetaData;
using Orchard.ContentManagement.MetaData.Models;
using Orchard.ContentTypes.Services;
using Orchard.ContentTypes.ViewModels;
using Orchard.Localization;
using Orchard.Mvc.Results;
using Orchard.UI.Notify;

namespace Orchard.ContentTypes.Controllers {
    public class AdminController : Controller {
        private readonly IContentDefinitionService _contentDefinitionService;
        private readonly IContentDefinitionManager _contentDefinitionManager;
        private readonly IContentDefinitionEditorEvents _extendViewModels;

        public AdminController(
            IOrchardServices orchardServices,
            IContentDefinitionService contentDefinitionService,
            IContentDefinitionManager contentDefinitionManager,
            IContentDefinitionEditorEvents extendViewModels) {
            Services = orchardServices;
            _contentDefinitionService = contentDefinitionService;
            _contentDefinitionManager = contentDefinitionManager;
            _extendViewModels = extendViewModels;
            T = NullLocalizer.Instance;
        }

        public IOrchardServices Services { get; private set; }
        public Localizer T { get; set; }
        public ActionResult Index() {
            return List();
        }

        #region Types

        public ActionResult List() {
            return View("List", new ListContentTypesViewModel {
                Types = _contentDefinitionService.GetTypeDefinitions()
            });
        }

        public ActionResult Create() {
            if (!Services.Authorizer.Authorize(Permissions.CreateContentTypes, T("Not allowed to create a content type.")))
                return new HttpUnauthorizedResult();

            return View(new CreateTypeViewModel());
        }

        [HttpPost, ActionName("Create")]
        public ActionResult CreatePOST(CreateTypeViewModel viewModel) {
            if (!Services.Authorizer.Authorize(Permissions.CreateContentTypes, T("Not allowed to create a content type.")))
                return new HttpUnauthorizedResult();

            if (!ModelState.IsValid)
                return View(viewModel);

            var definition = _contentDefinitionService.AddTypeDefinition(viewModel.DisplayName);

            return RedirectToAction("Edit", new { id = definition.Name });
        }

        public ActionResult Edit(string id) {
            if (!Services.Authorizer.Authorize(Permissions.CreateContentTypes, T("Not allowed to edit a content type.")))
                return new HttpUnauthorizedResult();

            var contentTypeDefinition = _contentDefinitionService.GetTypeDefinition(id);

            if (contentTypeDefinition == null)
                return new NotFoundResult();

            var viewModel = new EditTypeViewModel(contentTypeDefinition);
            viewModel.Parts = viewModel.Parts.ToArray();
            viewModel.Templates = _extendViewModels.TypeEditor(contentTypeDefinition);

            var entries = viewModel.Parts.Join(contentTypeDefinition.Parts,
                                               m => m.PartDefinition.Name,
                                               d => d.PartDefinition.Name,
                                               (model, definition) => new {model, definition});
            foreach (var entry in entries) {
                entry.model.PartDefinition.Fields = entry.model.PartDefinition.Fields.ToArray();
                entry.model.Templates = _extendViewModels.TypePartEditor(entry.definition);

                var fields = entry.model.PartDefinition.Fields.Join(entry.definition.PartDefinition.Fields,
                                   m => m.Name,
                                   d => d.Name,
                                   (model, definition) => new { model, definition });

                foreach (var field in fields) {
                    field.model.Templates = _extendViewModels.PartFieldEditor(field.definition);
                }
            }


            //Oy, this action is getting massive :(
            //todo: put this action on a diet
            var contentPartDefinition = _contentDefinitionService.GetPartDefinition(id);
            if (contentPartDefinition != null) {
                viewModel.Fields = viewModel.Fields.ToArray();
                var fields = viewModel.Fields.Join(contentPartDefinition.Fields,
                                    m => m.Name,
                                    d => d.Name,
                                    (model, definition) => new { model, definition });

                foreach (var field in fields) {
                    field.model.Templates = _extendViewModels.PartFieldEditor(field.definition);
                }
            }
            
            return View(viewModel);
        }

        [HttpPost, ActionName("Edit")]
        public ActionResult EditPOST(EditTypeViewModel viewModel) {
            if (!Services.Authorizer.Authorize(Permissions.CreateContentTypes, T("Not allowed to edit a content type.")))
                return new HttpUnauthorizedResult();

            var contentTypeDefinition = _contentDefinitionService.GetTypeDefinition(viewModel.Name);

            if (contentTypeDefinition == null)
                return new NotFoundResult();

            var updater = new Updater(this);
            _contentDefinitionManager.AlterTypeDefinition(viewModel.Name, typeBuilder => {

                typeBuilder.DisplayedAs(viewModel.DisplayName);

                // allow extensions to alter type configuration
                viewModel.Templates = _extendViewModels.TypeEditorUpdate(typeBuilder, updater);

                foreach (var entry in viewModel.Parts.Select((part, index) => new { part, index })) {
                    var partViewModel = entry.part;

                    // enable updater to be aware of changing part prefix
                    // todo: stick this info on the view model so the strings don't need to be in code & view
                    var firstHalf = "Parts[" + entry.index + "].";
                    updater._prefix = secondHalf => firstHalf + secondHalf;

                    // allow extensions to alter typePart configuration
                    typeBuilder.WithPart(entry.part.PartDefinition.Name, typePartBuilder => {
                        partViewModel.Templates = _extendViewModels.TypePartEditorUpdate(typePartBuilder, updater);
                    });

                    if (!partViewModel.PartDefinition.Fields.Any())
                        continue;

                    _contentDefinitionManager.AlterPartDefinition(partViewModel.PartDefinition.Name, partBuilder => {
                        foreach (var fieldEntry in partViewModel.PartDefinition.Fields.Select((field, index) => new { field, index })) {
                            partViewModel.PartDefinition.Fields = partViewModel.PartDefinition.Fields.ToArray();
                            var fieldViewModel = fieldEntry.field;

                            // enable updater to be aware of changing field prefix
                            var firstHalfFieldName = firstHalf + "PartDefinition.Fields[" + fieldEntry.index + "].";
                            updater._prefix = secondHalf => firstHalfFieldName + secondHalf;

                            // allow extensions to alter partField configuration
                            partBuilder.WithField(fieldViewModel.Name, partFieldBuilder => {
                                fieldViewModel.Templates = _extendViewModels.PartFieldEditorUpdate(partFieldBuilder, updater);
                            });
                        }
                    });
                }

                if (viewModel.Fields.Any()) {
                    _contentDefinitionManager.AlterPartDefinition(viewModel.Name, partBuilder => {
                        viewModel.Fields = viewModel.Fields.ToArray();
                        foreach (var fieldEntry in viewModel.Fields.Select((field, index) => new { field, index })) {
                            var fieldViewModel = fieldEntry.field;

                            // enable updater to be aware of changing field prefix
                            var firstHalfFieldName = "Fields[" + fieldEntry.index + "].";
                            updater._prefix = secondHalf => firstHalfFieldName + secondHalf;

                            // allow extensions to alter partField configuration
                            partBuilder.WithField(fieldViewModel.Name, partFieldBuilder => {
                                fieldViewModel.Templates = _extendViewModels.PartFieldEditorUpdate(partFieldBuilder, updater);
                            });
                        }
                    });
                }
            });

            if (!ModelState.IsValid) {
                Services.TransactionManager.Cancel();
                return View(viewModel);
            }

            Services.Notifier.Information(T("\"{0}\" settings have been saved.", viewModel.DisplayName));

            return RedirectToAction("Index");
        }

        public ActionResult AddPartsTo(string id) {
            if (!Services.Authorizer.Authorize(Permissions.CreateContentTypes, T("Not allowed to edit a content type.")))
                return new HttpUnauthorizedResult();

            var contentTypeDefinition = _contentDefinitionService.GetTypeDefinition(id);

            if (contentTypeDefinition == null)
                return new NotFoundResult();

            var viewModel = new AddPartsViewModel {
                Type = new EditTypeViewModel(contentTypeDefinition),
                PartSelections = _contentDefinitionService.GetPartDefinitions()
                    .Where(cpd => !contentTypeDefinition.Parts.Any(p => p.PartDefinition.Name == cpd.Name))
                    .Select(cpd => new PartSelectionViewModel {PartName = cpd.Name})
            };

            return View(viewModel);
        }

        [HttpPost, ActionName("AddPartsTo")]
        public ActionResult AddPartsToPOST(string id) {
            if (!Services.Authorizer.Authorize(Permissions.CreateContentTypes, T("Not allowed to edit a content type.")))
                return new HttpUnauthorizedResult();

            var contentTypeDefinition = _contentDefinitionService.GetTypeDefinition(id);

            if (contentTypeDefinition == null)
                return new NotFoundResult();

            var viewModel = new AddPartsViewModel();
            TryUpdateModel(viewModel);

            if (!ModelState.IsValid) {
                viewModel.Type = new EditTypeViewModel(contentTypeDefinition);
                return View(viewModel);
            }

            _contentDefinitionManager.AlterTypeDefinition(contentTypeDefinition.Name, typeBuilder => {
                var partsToAdd = viewModel.PartSelections.Where(ps => ps.IsSelected).Select(ps => ps.PartName);
                foreach (var partToAdd in partsToAdd)
                    typeBuilder.WithPart(partToAdd);
            });

            return RedirectToAction("Edit", new {id});
        }

        public ActionResult RemovePartFrom(string id) {
            if (!Services.Authorizer.Authorize(Permissions.CreateContentTypes, T("Not allowed to edit a content type.")))
                return new HttpUnauthorizedResult();

            var contentTypeDefinition = _contentDefinitionService.GetTypeDefinition(id);

            var viewModel = new RemovePartViewModel();
            if (contentTypeDefinition == null
                || !TryUpdateModel(viewModel)
                || !contentTypeDefinition.Parts.Any(p => p.PartDefinition.Name == viewModel.Name))
                return new NotFoundResult();

            viewModel.Type = new EditTypeViewModel { Name = contentTypeDefinition.Name, DisplayName = contentTypeDefinition.DisplayName };
            return View(viewModel);
        }

        [HttpPost, ActionName("RemovePartFrom")]
        public ActionResult RemovePartFromPOST(string id) {
            if (!Services.Authorizer.Authorize(Permissions.CreateContentTypes, T("Not allowed to edit a content type.")))
                return new HttpUnauthorizedResult();

            var contentTypeDefinition = _contentDefinitionService.GetTypeDefinition(id);

            var viewModel = new RemovePartViewModel();
            if (contentTypeDefinition == null
                || !TryUpdateModel(viewModel)
                || !contentTypeDefinition.Parts.Any(p => p.PartDefinition.Name == viewModel.Name))
                return new NotFoundResult();

            if (!ModelState.IsValid) {
                viewModel.Type = new EditTypeViewModel { Name = contentTypeDefinition.Name, DisplayName = contentTypeDefinition.DisplayName };
                return View(viewModel);
            }

            _contentDefinitionManager.AlterTypeDefinition(id, typeBuilder => typeBuilder.RemovePart(viewModel.Name));
            Services.Notifier.Information(T("The \"{0}\" part has been removed.", viewModel.Name));

            return RedirectToAction("Edit", new {id});
        }

        #endregion

        #region Parts

        public ActionResult ListParts() {
            return View(new ListContentPartsViewModel {
                Parts = _contentDefinitionService.GetPartDefinitions()
            });
        }

        public ActionResult CreatePart() {
            if (!Services.Authorizer.Authorize(Permissions.CreateContentTypes, T("Not allowed to create a content part.")))
                return new HttpUnauthorizedResult();

            return View(new CreatePartViewModel());
        }

        [HttpPost, ActionName("CreatePart")]
        public ActionResult CreatePartPOST(CreatePartViewModel viewModel) {
            if (!Services.Authorizer.Authorize(Permissions.CreateContentTypes, T("Not allowed to create a content part.")))
                return new HttpUnauthorizedResult();

            if (!ModelState.IsValid)
                return View(viewModel);

            var definition = _contentDefinitionService.AddPartDefinition(viewModel.Name);

            return RedirectToAction("EditPart", new { id = definition.Name });
        }

        public ActionResult EditPart(string id) {
            if (!Services.Authorizer.Authorize(Permissions.CreateContentTypes, T("Not allowed to edit a content part.")))
                return new HttpUnauthorizedResult();

            var contentPartDefinition = _contentDefinitionService.GetPartDefinition(id);

            if (contentPartDefinition == null)
                return new NotFoundResult();

            var viewModel = new EditPartViewModel(contentPartDefinition) {
                Templates = _extendViewModels.PartEditor(contentPartDefinition)
            };

            return View(viewModel);
        }

        [HttpPost, ActionName("EditPart")]
        public ActionResult EditPartPOST(EditPartViewModel viewModel) {
            if (!Services.Authorizer.Authorize(Permissions.CreateContentTypes, T("Not allowed to edit a content part.")))
                return new HttpUnauthorizedResult();

            var contentPartDefinition = _contentDefinitionService.GetPartDefinition(viewModel.Name);

            if (contentPartDefinition == null)
                return new NotFoundResult();

            var updater = new Updater(this);
            _contentDefinitionManager.AlterPartDefinition(viewModel.Name, partBuilder => {
                // allow extensions to alter part configuration
                viewModel.Templates = _extendViewModels.PartEditorUpdate(partBuilder, updater);
            });

            if (!ModelState.IsValid) {
                Services.TransactionManager.Cancel();
                return View(viewModel);
            }

            return RedirectToAction("ListParts");
        }

        public ActionResult AddFieldTo(string id) {
            if (!Services.Authorizer.Authorize(Permissions.CreateContentTypes, T("Not allowed to edit a content part.")))
                return new HttpUnauthorizedResult();

            var contentPartDefinition = _contentDefinitionService.GetPartDefinition(id);

            if (contentPartDefinition == null) {
                //id passed in might be that of a type w/ no implicit field
                var contentTypeDefinition = _contentDefinitionService.GetTypeDefinition(id);
                if (contentTypeDefinition != null)
                    contentPartDefinition = new ContentPartDefinition(id);
                else
                    return new NotFoundResult();
            }

            var viewModel = new AddFieldViewModel {
                Part = new EditPartViewModel(contentPartDefinition),
                Fields = _contentDefinitionService.GetFieldDefinitions()
            };

            return View(viewModel);
        }

        [HttpPost, ActionName("AddFieldTo")]
        public ActionResult AddFieldToPOST(string id) {
            if (!Services.Authorizer.Authorize(Permissions.CreateContentTypes, T("Not allowed to edit a content part.")))
                return new HttpUnauthorizedResult();

            var viewModel = new AddFieldViewModel();
            TryUpdateModel(viewModel);

            var contentPartDefinition = _contentDefinitionService.GetPartDefinition(id);
            var contentTypeDefinition = _contentDefinitionService.GetTypeDefinition(id);

            if (!ModelState.IsValid)
                return AddFieldTo(id);

            if (contentPartDefinition == null) {
                //id passed in might be that of a type w/ no implicit field
                if (contentTypeDefinition != null) {
                    contentPartDefinition = new ContentPartDefinition(id);
                    var contentTypeDefinitionParts = contentTypeDefinition.Parts.ToList();
                    contentTypeDefinitionParts.Add(new ContentTypeDefinition.Part(contentPartDefinition, null));
                    _contentDefinitionService.AlterTypeDefinition(
                        new ContentTypeDefinition(
                            contentTypeDefinition.Name,
                            contentTypeDefinition.DisplayName,
                            contentTypeDefinitionParts,
                            contentTypeDefinition.Settings
                            )
                        );
                }
                else {
                    return new NotFoundResult();
                }
            }

            var contentPartFields = contentPartDefinition.Fields.ToList();
            contentPartFields.Add(new ContentPartDefinition.Field(new ContentFieldDefinition(viewModel.FieldTypeName), viewModel.DisplayName, null));
            _contentDefinitionService.AlterPartDefinition(new ContentPartDefinition(contentPartDefinition.Name, contentPartFields, contentPartDefinition.Settings));

            Services.Notifier.Information(T("The \"{0}\" field has been added.", viewModel.DisplayName));

            if (contentTypeDefinition != null)
                return RedirectToAction("Edit", new { id });

            return RedirectToAction("EditPart", new { id });
        }


        public ActionResult RemoveFieldFrom(string id) {
            if (!Services.Authorizer.Authorize(Permissions.CreateContentTypes, T("Not allowed to edit a content part.")))
                return new HttpUnauthorizedResult();

            var contentPartDefinition = _contentDefinitionService.GetPartDefinition(id);

            var viewModel = new RemoveFieldViewModel();
            if (contentPartDefinition == null
                || !TryUpdateModel(viewModel)
                || !contentPartDefinition.Fields.Any(p => p.Name == viewModel.Name))
                return new NotFoundResult();

            viewModel.Part = new EditPartViewModel { Name = contentPartDefinition.Name };
            return View(viewModel);
        }

        [HttpPost, ActionName("RemoveFieldFrom")]
        public ActionResult RemoveFieldFromPOST(string id) {
            if (!Services.Authorizer.Authorize(Permissions.CreateContentTypes, T("Not allowed to edit a content part.")))
                return new HttpUnauthorizedResult();

            var contentPartDefinition = _contentDefinitionService.GetPartDefinition(id);

            var viewModel = new RemoveFieldViewModel();
            if (contentPartDefinition == null
                || !TryUpdateModel(viewModel)
                || !contentPartDefinition.Fields.Any(p => p.Name == viewModel.Name))
                return new NotFoundResult();

            if (!ModelState.IsValid) {
                viewModel.Part = new EditPartViewModel { Name = contentPartDefinition.Name };
                return View(viewModel);
            }

            _contentDefinitionManager.AlterPartDefinition(id, typeBuilder => typeBuilder.RemoveField(viewModel.Name));
            Services.Notifier.Information(T("The \"{0}\" field has been removed.", viewModel.Name));

            if (_contentDefinitionService.GetTypeDefinition(id) != null)
                return RedirectToAction("Edit", new { id });

            return RedirectToAction("EditPart", new { id });
        }

        #endregion

        class Updater : IUpdateModel {
            private readonly AdminController _thunk;

            public Updater(AdminController thunk) {
                _thunk = thunk;
            }

            public Func<string, string> _prefix = x => x;

            public bool TryUpdateModel<TModel>(TModel model, string prefix, string[] includeProperties, string[] excludeProperties) where TModel : class {
                return _thunk.TryUpdateModel(model, _prefix(prefix), includeProperties, excludeProperties);
            }

            public void AddModelError(string key, LocalizedString errorMessage) {
                _thunk.ModelState.AddModelError(_prefix(key), errorMessage.ToString());
            }
        }

    }
}
