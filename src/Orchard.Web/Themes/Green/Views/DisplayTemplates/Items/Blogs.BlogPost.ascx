﻿<%@ Control Language="C#" Inherits="Orchard.Mvc.ViewUserControl<ContentItemViewModel<BlogPart>>" %>
<%@ Import Namespace="Orchard.Mvc.ViewModels"%>
<%@ Import Namespace="Orchard.Blogs.Models"%>
<h1><%: Html.TitleForPage(Model.Item.Title)%></h1>
<% Html.Zone("primary"); %>