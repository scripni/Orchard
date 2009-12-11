<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl<Orchard.Users.ViewModels.UserCreateViewModel>" %>
<%@ Import Namespace="Orchard.Utility" %>
    <%=Html.EditorFor(m=>m.UserName, "inputTextLarge") %>
    <%=Html.EditorFor(m=>m.Email, "inputTextLarge") %>
    <%=Html.EditorFor(m=>m.Password, "inputPasswordLarge") %>
    <%=Html.EditorFor(m=>m.ConfirmPassword, "inputPasswordLarge") %>
<%
foreach(var e in Model.EditorModel.Editors) {
    var editor = e;%>
    <%=Html.EditorFor(m => editor.Model, editor.TemplateName, editor.Prefix)%>
<% } %>
