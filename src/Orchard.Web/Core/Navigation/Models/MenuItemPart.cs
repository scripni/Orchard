﻿using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using Orchard.ContentManagement;

namespace Orchard.Core.Navigation.Models {
    public class MenuItemPart : ContentPart<MenuItemPartRecord> {
        [Required]
        public string Url {
            get { return Record.Url; }
            set { Record.Url = value; }
        }
    }
}
