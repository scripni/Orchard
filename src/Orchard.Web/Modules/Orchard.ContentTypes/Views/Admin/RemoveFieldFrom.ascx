<%@ Control Language="C#" Inherits="Orchard.Mvc.ViewUserControl<Orchard.ContentTypes.ViewModels.RemoveFieldViewModel>" %>
<%
Html.RegisterStyle("admin.css"); %>
<h1><%:Html.TitleForPage(T("Remove the \"{0}\" part from  \"{1}\"", Model.Name, Model.Part.DisplayName).ToString())%></h1><%
using (Html.BeginFormAntiForgeryPost()) { %>
    <p><%:T("Looks like you couldn't use the fancy way to remove the field. Try hitting the button below to force the issue.") %></p>
    <fieldset>
        <%=Html.HiddenFor(m => m.Name) %>
        <button class="primaryAction" type="submit"><%:T("Remove") %></button>
    </fieldset><%
} %>