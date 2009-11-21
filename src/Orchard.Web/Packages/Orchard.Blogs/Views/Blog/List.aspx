<%@ Page Language="C#" Inherits="System.Web.Mvc.ViewPage<IEnumerable<Blog>>" %>
<%@ Import Namespace="Orchard.Blogs.Models"%>
<%@ Import Namespace="Orchard.Mvc.Html"%>
<%@ Import Namespace="Orchard.Mvc.ViewModels"%>
<% Html.Include("Header"); %>
    <div class="yui-g">
        <h2>Blogs</h2><%
        //TODO: (erikpo) Replace this with an Html extension method of some sort (ListForModel?)
        if (Model.Count() > 0) { %>
        <ul><%
            foreach (Blog blog in Model) { %>
            <li><a href="<%=Url.Action("Item", "Blog", new {blogSlug = blog.Slug}) %>"><%=Html.Encode(blog.Name) %></a></li><%
            } %>
        </ul><%
        } %>
    </div>
<% Html.Include("Footer"); %>