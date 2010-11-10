﻿using System.Linq;
using Orchard.ContentManagement;
using Orchard.Core.Containers.Models;
using Orchard.Core.Routable.Models;
using Orchard.Environment;
using Orchard.Tasks;

namespace Orchard.Core.Containers.Services {
    public class ContainersPathConstraintUpdater : IOrchardShellEvents, IBackgroundTask {
        private readonly IContainersPathConstraint _containersPathConstraint;
        private readonly IContentManager _contentManager;

        public ContainersPathConstraintUpdater(IContainersPathConstraint containersPathConstraint, IContentManager contentManager) {
            _containersPathConstraint = containersPathConstraint;
            _contentManager = contentManager;
        }

        void IOrchardShellEvents.Activated() {
            Refresh();
        }

        void IOrchardShellEvents.Terminating() {
        }

        void IBackgroundTask.Sweep() {
            Refresh();
        }

        private void Refresh() {
            var routeParts = _contentManager.Query<RoutePart, RoutePartRecord>().Join<ContainerPartRecord>().List();
            _containersPathConstraint.SetPaths(routeParts.Select(x=>x.Path));
        }
    }
}