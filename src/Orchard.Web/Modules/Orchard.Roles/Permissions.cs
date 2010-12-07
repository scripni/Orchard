﻿using System.Collections.Generic;
using JetBrains.Annotations;
using Orchard.Environment.Extensions.Models;
using Orchard.Security.Permissions;

namespace Orchard.Roles {
    [UsedImplicitly]
    public class Permissions : IPermissionProvider {
        public static readonly Permission ManageRoles = new Permission { Description = "Create and manage roles", Name = "ManageRoles" };
        public static readonly Permission ApplyRoles = new Permission { Description = "Assign users to roles", Name = "AssignUsersToRoles", ImpliedBy = new[] { ManageRoles } };

        public virtual Feature Feature { get; set; }

        public IEnumerable<Permission> GetPermissions() {
            return new Permission[] {
                ManageRoles,
                ApplyRoles,
            };
        }

        public IEnumerable<PermissionStereotype> GetDefaultStereotypes() {
            return new[] {
                new PermissionStereotype {
                    Name = "Administrator",
                    Permissions = new[] {ManageRoles, ApplyRoles}
                }
            };
        }
    }
}
