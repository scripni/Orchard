﻿using System.Collections.Generic;
using Orchard.Models.ViewModels;
using Orchard.Mvc.ViewModels;
using Orchard.Sandbox.Models;

namespace Orchard.Sandbox.ViewModels {
    public class PageIndexViewModel : BaseViewModel {
        public IEnumerable<ItemDisplayModel<SandboxPage>> Pages { get; set; }
    } 
}
