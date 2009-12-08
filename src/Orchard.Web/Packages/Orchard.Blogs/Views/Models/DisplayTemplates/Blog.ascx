﻿<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl<ItemDisplayViewModel<Blog>>" %>
<%@ Import Namespace="Orchard.Mvc.Html"%>
<%@ Import Namespace="Orchard.Models.ViewModels"%>
<%@ Import Namespace="Orchard.Blogs.Extensions"%>
<%@ Import Namespace="Orchard.Blogs.Models"%>
<div class="manage"><a href="<%=Url.BlogEdit(Model.Item.Slug) %>" class="ibutton edit">edit</a></div>
<h2><%=Html.Encode(Model.Item.Name) %></h2>
<div><%=Html.Encode(Model.Item.Description) %></div>
<%--TODO: (erikpo) Need to figure out which zones should be displayed in this template--%>
<%=Html.DisplayZonesAny() %>