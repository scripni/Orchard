<%@ Page Language="C#" Inherits="Orchard.Mvc.ViewPage<CreateItemViewModel>" %>

<%@ Import Namespace="Orchard.Core.Contents.ViewModels" %>
<% Html.AddTitleParts(T("Create Content").ToString()); %>
<% using (Html.BeginFormAntiForgeryPost()) { %>
<%:Html.ValidationSummary() %>
<%:Html.EditorForItem(m=>m.Content) %>
<%} %>
