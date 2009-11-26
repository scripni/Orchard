﻿using System.Collections.Generic;
using Orchard.Mvc.ViewModels;
using Orchard.Sandbox.Models;
using Orchard.UI.Models;

namespace Orchard.Sandbox.ViewModels {
    public class PageEditViewModel : BaseViewModel {
        public SandboxPage Page { get; set; }
        public IEnumerable<ModelTemplate> Editors { get; set; }
    }
}
