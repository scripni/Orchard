﻿<%@ Control Language="C#" Inherits="Orchard.Mvc.ViewUserControl<ShellSettings>" %>
<%@ Import Namespace="Orchard.Environment.Configuration" %>
<%: Html.ActionLink(T("Make Valid*").ToString(), "_setup", new {tenantName = Model.Name, area = "Orchard.MultiTenancy"}) %>
