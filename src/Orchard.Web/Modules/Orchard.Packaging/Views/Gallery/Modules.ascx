﻿<%@ Control Language="C#" Inherits="Orchard.Mvc.ViewUserControl<Orchard.Packaging.ViewModels.PackagingModulesViewModel>" %>
<h1>
    <%: Html.TitleForPage(T("Browse Gallery").ToString())%></h1>

<p><%:Html.ActionLink("Update List", "Update") %></p>

<ul>
    <%foreach (var item in Model.Modules) {%>
    <li>
        <a href="<%:item.PackageStreamUri%>"><%:item.SyndicationItem.Title.Text%></a>
        [<%:Html.ActionLink("Install", "Install", new RouteValueDictionary {{"SyndicationId",item.SyndicationItem.Id}})%>]
    </li>
    <%}%>
</ul>
