﻿<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl<BaseViewModel>" %>
<%@ Import Namespace="Orchard.Mvc.ViewModels"%><%
Html.RegisterStyle("site.css");
Model.Zones.AddRenderPartial("header", "header", Model);
Model.Zones.AddRenderPartial("header:after", "user", Model);
Model.Zones.AddRenderPartial("menu", "menu", Model);
Model.Zones.AddRenderPartial("content:before", "messages", Model.Messages);
%>
<div id="page">
    <div id="header"><%
        Html.Zone("header");
        Html.Zone("menu"); %>
    </div>
    <div id="main">
        <div id=”content”><%
            Html.ZoneBody("primary");
        %></div>
        <div id=”sidebar”><%
            Html.Zone("secondary");
        %></div>
        <div id="footer"><%
            Html.Zone("footer");
        %></div>
    </div>
</div>