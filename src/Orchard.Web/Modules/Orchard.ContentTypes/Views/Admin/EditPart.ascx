<%@ Control Language="C#" Inherits="Orchard.Mvc.ViewUserControl<Orchard.ContentTypes.ViewModels.EditPartViewModel>" %>
<%
Html.RegisterStyle("admin.css"); %>
<h1><%:Html.TitleForPage(T("Edit Part").ToString()) %></h1><%
using (Html.BeginFormAntiForgeryPost()) { %>
    <%:Html.ValidationSummary() %>
    <fieldset>
        <label for="Name"><%:T("Name") %></label>
        <%--// has unintended consequences (renamging the part) - changing the name creates a new part of that name--%>
        <%:Html.TextBoxFor(m => m.Name, new {@class = "textMedium"}) %>
    </fieldset>
    <% Html.RenderTemplates(Model.Templates); %>
    <h2><%:T("Fields") %></h2>
    <div class="manage add-to-type"><%: Html.ActionLink(T("Add").Text, "AddFieldTo", new { area = "Orchard.ContentTypes", id = Model.Name }, new { @class = "button" }) %></div>
    <%:Html.EditorFor(m => m.Fields, "Fields", "") %>
    <fieldset>
        <button class="primaryAction" type="submit"><%:T("Save") %></button>
    </fieldset><%
} %>
