﻿using System;
using System.Collections.Generic;
using Orchard.Localization;
using Orchard.Localization.Services;
using Orchard.UI.Notify;

namespace Orchard.Indexing.Services
{
    public class IndexingService : IIndexingService
    {
        private const string SearchIndexName = "Search";
        private readonly IIndexManager _indexManager;
        private readonly IEnumerable<IIndexNotifierHandler> _indexNotifierHandlers;

        public IndexingService(IOrchardServices services, IIndexManager indexManager, IEnumerable<IIndexNotifierHandler> indexNotifierHandlers, ICultureManager cultureManager) {
            Services = services;
            _indexManager = indexManager;
            _indexNotifierHandlers = indexNotifierHandlers;
            T = NullLocalizer.Instance;
        }

        public IOrchardServices Services { get; set; }
        public Localizer T { get; set; }

        public bool HasIndexToManage {
            get { return _indexManager.HasIndexProvider(); }
        }

        void IIndexingService.RebuildIndex() {
            if (!_indexManager.HasIndexProvider()) {
                Services.Notifier.Warning(T("There is no search index to rebuild."));
                return;
            }

            var searchProvider = _indexManager.GetSearchIndexProvider();
            if (searchProvider.Exists(SearchIndexName))
                searchProvider.DeleteIndex(SearchIndexName);

            searchProvider.CreateIndex(SearchIndexName); // or just reset the updated date and let the background process recreate the index

            Services.Notifier.Information(T("The search index has been rebuilt."));
        }

        void IIndexingService.UpdateIndex() {
            
            foreach(var handler in _indexNotifierHandlers) {
                handler.UpdateIndex(SearchIndexName);
            }

            Services.Notifier.Information(T("The search index has been updated."));
        }

        DateTime IIndexingService.GetIndexUpdatedUtc() {
            return !HasIndexToManage
                ? DateTime.MinValue
                : _indexManager.GetSearchIndexProvider().GetLastIndexUtc(SearchIndexName);
        }
    }
}