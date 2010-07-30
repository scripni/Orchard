﻿<%@ Control Language="C#" Inherits="Orchard.Mvc.ViewUserControl<Orchard.Packaging.ViewModels.PackagingModulesViewModel>" %>
<% Html.RegisterStyle("admin.css"); %>

<h1>
    <%: Html.TitleForPage(T("Browse Gallery").ToString())%></h1>

<div class="manage">
        <%:Html.ActionLink(T("Refresh").ToString(), "Update", new object{}, new { @class = "button primaryAction" }) %>
</div>
<% using ( Html.BeginFormAntiForgeryPost(Url.Action("Modules", "Gallery")) ) {%>
    <fieldset class="bulk-actions">
        <label for="filterResults" class="bulk-filter"><%:T("Feed:")%></label>
        <select id="sourceId" name="sourceId">
            <%:Html.SelectOption("", Model.SelectedSource == null, T("Any (show all feeds)").ToString())%>
            <%
       foreach (var source in Model.Sources) {%>
                <%:Html.SelectOption(source.Id, Model.SelectedSource != null && Model.SelectedSource.Id == source.Id, source.FeedTitle)%><%
       }%>
        </select>
        <button type="submit"><%:T("Apply")%></button>
    </fieldset>
<%
   } %>
    

<% if (Model.Modules.Count() > 0) { %>
    <ul class="contentItems">
    <%foreach (var item in Model.Modules) {%>
        <li> 
            <div class="moduleName">
                <h2><%: (item.SyndicationItem.Title == null ? T("(No title)").Text : item.SyndicationItem.Title.Text)%><span> - <%: T("Version: {0}", "1.0")%></span></h2>
            </div>

            <div class="related">
                <%:Html.ActionLink(T("Install").ToString(), "Install", new RouteValueDictionary {{"SyndicationId",item.SyndicationItem.Id}})%><%:T(" | ") %>
                <a href="<%:item.PackageStreamUri%>"><%: T("Download") %></a>
             </div>

             <div class="properties">
                <p><%: (item.SyndicationItem.Summary == null ? T("(No description").Text : item.SyndicationItem.Summary.Text) %></p>
                <ul class="pageStatus">
                    <li><%: T("Last Updated: {0}", item.SyndicationItem.LastUpdatedTime.ToLocalTime()) %></li>
                    <li>&nbsp;&#124;&nbsp;<%: T("Author: {0}", item.SyndicationItem.Authors.Any() ? String.Join(", ", item.SyndicationItem.Authors.Select(a => a.Name)) : T("Unknown").Text)%></li>
                </ul>
           </div>   
        </li><%
      }%>
    </ul><%
   }%>

