﻿<%@ Control Language="C#" Inherits="Orchard.Mvc.ViewUserControl" %>
<% if (Model.CurrentUser != null) {
    %><div id="login"><%: T("User:")%> <%: Model.CurrentUser.UserName %> | <%: Html.ActionLink(T("Logout").ToString(), "LogOff", new { Area = "Orchard.Users", Controller = "Account" }) %></div><%
   } %>