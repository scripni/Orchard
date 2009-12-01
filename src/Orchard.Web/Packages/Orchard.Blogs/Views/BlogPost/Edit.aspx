<%@ Page Language="C#" Inherits="System.Web.Mvc.ViewPage<BlogPostEditViewModel>" %>
<%@ Import Namespace="Orchard.Mvc.Html"%>
<%@ Import Namespace="Orchard.Blogs.Extensions"%>
<%@ Import Namespace="Orchard.Blogs.ViewModels"%>
<% Html.Include("Head"); %>
    <h2>Edit Blog Post</h2>
    <p><a href="<%=Url.Blogs() %>">Manage Blogs</a> &gt; <a href="<%=Url.BlogEdit(Model.Blog.Slug) %>"><%=Html.Encode(Model.Blog.Name)%></a> &gt; <a href="<%=Url.BlogPostEdit(Model.Blog.Slug, Model.Post.Slug) %>"><%=Model.Title %></a></p>
    <% using (Html.BeginForm()) { %>
        <%=Html.ValidationSummary() %>
        <%=Html.EditorForModel() %>
        <fieldset><input class="button" type="submit" value="Save" /></fieldset>
    <% } %>
<% Html.Include("Foot"); %>