﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Orchard.DisplayManagement;

namespace Orchard.DevTools {
    public class Shapes : IDependency {
        [Shape]
        public IHtmlString Title(dynamic text) {
            return new HtmlString("<h2>" + text + "</h2>");
        }

        [Shape]
        public IHtmlString Explosion(int? Height, int? Width) {

            return new HtmlString(string.Format("<span>Boom {0}x{1}</span>", Height, Width));
        }

        [Shape]
        public IHtmlString Page(dynamic Display, dynamic Shape) {
            return Display(Shape.Sidebar, Shape.Messages);
        }

        [Shape]
        public IHtmlString Zone(dynamic Display, dynamic Shape) {
            var tag = new TagBuilder("div");
            tag.GenerateId("zone-" + Shape.Name);
            tag.AddCssClass("zone-" + Shape.Name);
            tag.AddCssClass("zone");

            IEnumerable<IHtmlString> all = DisplayAll(Display, Shape);
            tag.InnerHtml = Combine(all.ToArray()).ToString();

            return new HtmlString(tag.ToString());
        }

        [Shape]
        public IHtmlString Message(dynamic Display, object Content, string Severity) {
            return Display(new HtmlString("<p class=\"message\">"), Severity ?? "Neutral", ": ", Content, new HtmlString("</p>"));
        }

        static IHtmlString Combine(IEnumerable<IHtmlString> contents) {
            return new HtmlString(contents.Aggregate("", (a, b) => a + b));
        }

        static IEnumerable<IHtmlString> DisplayAll(dynamic Display, dynamic Shape) {
            foreach (var item in Shape) {
                yield return Display(item);
            }
        }
    }
}
