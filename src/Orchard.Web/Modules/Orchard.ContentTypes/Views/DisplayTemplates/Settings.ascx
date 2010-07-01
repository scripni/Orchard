﻿<%@ Control Language="C#" Inherits="Orchard.Mvc.ViewUserControl<SettingsDictionary>" %>
<%@ import Namespace="Orchard.ContentManagement.MetaData.Models" %>
<%
if (Model.Any()) { %>
    <dl><%
        foreach (var setting in Model) { %>
        <dt><%:setting.Key %></dt>
        <dd><%:setting.Value %></dd><%
        } %>
    </dl><%
} %>