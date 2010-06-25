﻿<%@ Control Language="C#" Inherits="Orchard.Mvc.ViewUserControl<Orchard.ContentTypes.ViewModels.EditTypePartViewModel>" %>
<%@ Import Namespace="Orchard.Core.Contents.ViewModels" %>
<%@ Import Namespace="Orchard.ContentTypes.ViewModels" %>
    <fieldset class="manage-part">
        <h3><%:Model.PartDefinition.Name %></h3>
        <div class="manage">
        <%--// these inline forms can't be here. should probably have some JavaScript in here to build up the forms and add the "remove" link.
            // get the antiforgery token from the edit type form and mark up the part in a semantic way so I can get some info from the DOM --%>
            <%:Html.Link("[remove]", "#forshowonlyandnotintendedtowork") %>
<%--        <% using (Html.BeginFormAntiForgeryPost(Url.Action("RemovePart", new { area = "Contents" }), FormMethod.Post, new {@class = "inline link"})) { %>
            <%:Html.Hidden("name", Model.PartDefinition.Name, new { id = "" }) %>
            <button type="submit" title="<%:T("Remove") %>"><%:T("Remove") %></button>
        <% } %> --%>
        </div>
        <% Html.RenderTemplate(Model.Templates); %>
                
        <h4><%:T("Global configuration") %></h4>
        <div class="manage minor"><%:Html.ActionLink(T("Edit").Text, "EditPart", new { area = "Orchard.ContentTypes", id = Model.PartDefinition.Name }) %></div>
        <%:Html.DisplayFor(m => m.PartDefinition.Settings, "Settings", "PartDefinition") %>
        <%:Html.DisplayFor(m => m.PartDefinition.Fields, "Fields") %>
        <%:Html.Hidden("PartDefinition.Name", Model.PartDefinition.Name) %>
    </fieldset>