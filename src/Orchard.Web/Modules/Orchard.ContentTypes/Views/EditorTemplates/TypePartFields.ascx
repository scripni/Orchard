﻿<%@ Control Language="C#" Inherits="Orchard.Mvc.ViewUserControl<IEnumerable<Orchard.ContentTypes.ViewModels.EditPartFieldViewModel>>" %>
<%
if (Model.Any()) {
    var fi = 0;
    foreach (var field in Model) {
        var f = field;
        var htmlFieldName = string.Format("Fields[{0}]", fi++); %>
        <%:Html.EditorFor(m => f, "TypePartField", htmlFieldName) %><%
    }
} %>