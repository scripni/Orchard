﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ClaySharp;
using Orchard.DisplayManagement;
using Orchard.Localization;

namespace Orchard.Mvc.Html {
    public class Shapes : IShapeDriver {
        public Shapes(IShapeHelperFactory shapeHelperFactory) {
            New = shapeHelperFactory.CreateHelper();
        }

        dynamic New { get; set; }

        public IHtmlString Link(dynamic Display, object Content, string Url, INamedEnumerable<object> Attributes) {
            var a = new TagBuilder("a") {
                InnerHtml = Display(Content).ToString()
            };
            a.MergeAttributes(Attributes.Named);
            a.MergeAttribute("href", Url);
            return Display(new HtmlString(a.ToString()));
        }

        public IHtmlString Image(dynamic Display, string Url, INamedEnumerable<object> Attributes) {
            var img = new TagBuilder("img");
            img.MergeAttributes(Attributes.Named);
            img.MergeAttribute("src", Url);
            return Display(new HtmlString(img.ToString(TagRenderMode.SelfClosing)));
        }

        public IHtmlString UnorderedList(dynamic Display, dynamic Shape, INamedEnumerable<object> Attributes) {
            var ul = new TagBuilder("ul") {
                InnerHtml = Combine(DisplayAll(Display, Shape, New.ListItem())).ToString()
            };
            ul.MergeAttributes(Attributes.Named);
            return Display(new HtmlString(ul.ToString()));
        }

        public IHtmlString ListItem(dynamic Display, dynamic Shape, INamedEnumerable<object> Attributes) {
            var li = new TagBuilder("li") {
                InnerHtml = Display(Shape.Content).ToString()
            };
            li.MergeAttributes(Attributes.Named);
            if (!string.IsNullOrWhiteSpace(Shape.Content.Name as string))
                li.MergeAttribute("class", Shape.Content.Name);
            return Display(new HtmlString(li.ToString()));
        }

        #region form and company

        public IHtmlString Form(dynamic Display, dynamic Shape, object Submit, INamedEnumerable<object> Attributes) {
            var form = new TagBuilder("form") {
                InnerHtml = Display(Shape.Fieldsets).ToString()
            };
            form.MergeAttributes(Attributes.Named);
            return Display(new HtmlString(form.ToString()));
        }

        public IHtmlString Fieldsets(dynamic Display, dynamic Shape) {
            return Display(new HtmlString(Combine(DisplayAll(Display, Shape)).ToString()));
        }

        public IHtmlString Fieldset(dynamic Display, dynamic Shape, INamedEnumerable<object> Attributes) {
            var fieldset = new TagBuilder("fieldset");
            fieldset.MergeAttributes(Attributes.Named);
            if (!string.IsNullOrWhiteSpace(Shape.Name as string))
                fieldset.MergeAttribute("class", Shape.Name);

            if (Shape.Count > 1) {
                Shape.ShapeMetadata.Type = "UnorderedList";
                fieldset.InnerHtml = Display(Shape).ToString();
            }
            else {
                fieldset.InnerHtml = Combine(DisplayAll(Display, Shape)).ToString();
            }

            return Display(new HtmlString(fieldset.ToString()));
        }

        public IHtmlString FormButton(dynamic Display, dynamic Shape, INamedEnumerable<object> Attributes) {
            var button = new TagBuilder("button") {
                InnerHtml = Display(Shape.Text).ToString() //not caring about anything other than text at the moment
            };
            button.MergeAttributes(Attributes.Named);
            return Display(new HtmlString(button.ToString()));
        }

        public IHtmlString FormSubmit(dynamic Display, dynamic Shape) {
            Shape.ShapeMetadata.Type = "FormButton";
            Shape.Attributes(new { type = "submit" });
            return Display(Shape);
        }

        public IHtmlString Input(dynamic Display, dynamic Shape, INamedEnumerable<object> Attributes) {
            var input = new TagBuilder("input");
            input.MergeAttributes(Attributes.Named);
            input.MergeAttribute("name", Shape.Name);
            if (!string.IsNullOrWhiteSpace(Shape.Value as string))
                input.MergeAttribute("value", Shape.Value);
            return Display(new HtmlString(input.ToString(TagRenderMode.SelfClosing)));
        }

        public IHtmlString Label(dynamic Display, dynamic Shape, INamedEnumerable<object> Attributes) {
            var label = new TagBuilder("label") {
                InnerHtml = Display(Shape.Text).ToString()
            };
            label.MergeAttributes(Attributes.Named);
            return Display(new HtmlString(label.ToString()));
        }

        public IHtmlString Text(dynamic Shape) {
            return new HtmlString(Shape.ToString());
        }

        public IHtmlString InputPassword(dynamic Display, dynamic Shape) {
            Shape.ShapeMetadata.Type = "Input";
            Shape.Attributes(new { type = "password" });
            return Display(
                New.Label().Text(Shape.Text),
                Shape); //need validation shape
        }

        public IHtmlString InputText(dynamic Display, dynamic Shape) {
            Shape.ShapeMetadata.Type = "Input";
            Shape.Attributes(new { type = "text" });
            return Display(
                New.Label().Text(Shape.Text),
                Shape); //need validation shape
        }

        #endregion

        static IHtmlString Combine(IEnumerable<IHtmlString> contents) {
            return new HtmlString(contents.Aggregate("", (a, b) => a + b));
        }

        IEnumerable<IHtmlString> DisplayAll(dynamic Display, dynamic Shape) {
            foreach (var item in Shape) {
                yield return Display(item);
            }
        }

        IEnumerable<IHtmlString> DisplayAll(dynamic Display, dynamic Shape, dynamic Wrapper) {
            foreach (var item in Shape) {
                yield return Display(Wrapper.Content(item));
            }
        }
    }
}
