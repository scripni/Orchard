﻿<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl<string>" %>
<%@ Import Namespace="System.Web.Mvc.Html" %>
<%-- todo: (heskew) need script include and init script to not be duplicated --%>
<%--<script src="<%=ResolveUrl("~/Packages/TinyMce/scripts/tiny_mce.js") %>" type="text/javascript"></script>--%>
<%=Html.TextArea("", Model, 25, 80, new { @class = "html" }) %>
<%--<script type="text/javascript">
    tinyMCE.init({ theme: "advanced", mode: "textareas", plugins: "fullscreen,autoresize", theme_advanced_toolbar_location: "top", theme_advanced_toolbar_align: "left", theme_advanced_buttons3_add: "fullscreen" });
</script>--%>