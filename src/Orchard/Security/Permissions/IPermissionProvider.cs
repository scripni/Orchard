﻿using System.Collections.Generic;

namespace Orchard.Security.Permissions {
    /// <summary>
    /// Implemented by packages to enumerate the types of permissions
    /// the which may be granted
    /// </summary>
    public interface IPermissionProvider : IDependency {
        string PackageName { get; }
        IEnumerable<Permission> GetPermissions();
        IEnumerable<PermissionStereotype> GetDefaultStereotypes();
    }

    public class PermissionStereotype {
        public string Name { get; set; }
        public IEnumerable<Permission> Permissions { get; set; }
    }
}