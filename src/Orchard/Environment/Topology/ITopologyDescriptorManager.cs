﻿using System.Collections.Generic;
using Orchard.Environment.Topology.Models;

namespace Orchard.Environment.Topology {
    /// <summary>
    /// Service resolved out of the shell container. Primarily used by host.
    /// </summary>
    public interface ITopologyDescriptorManager {
        /// <summary>
        /// Uses shell-specific database or other resources to return 
        /// the current "correct" configuration. The host will use this information
        /// to reinitialize the shell.
        /// </summary>
        ShellTopologyDescriptor GetTopologyDescriptor();

        /// <summary>
        /// Alters databased information to match information passed as arguments.
        /// Prior SerialNumber used for optomistic concurrency, and an exception
        /// should be thrown if the number in storage doesn't match what's provided.
        /// </summary>
        void UpdateTopologyDescriptor(
            int priorSerialNumber,
            IEnumerable<TopologyFeature> enabledFeatures,
            IEnumerable<TopologyParameter> parameters);
    }


}
