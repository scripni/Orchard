<%@ Page Language="C#" Inherits="Orchard.Mvc.ViewPage<MediaItemEditViewModel>" %>
<%@ Import Namespace="Orchard.Media.Models"%>
<%@ Import Namespace="Orchard.Media.Helpers"%>
<%@ Import Namespace="Orchard.Media.ViewModels"%><%
Html.RegisterStyle("admin.css"); %>
<h1><%: Html.TitleForPage(T("Edit Media - {0}", Model.Name).ToString())%></h1>

<div class="breadCrumbs">
<p><%: Html.ActionLink(T("Media Folders").ToString(), "Index")%> &#62; 
    <%foreach (FolderNavigation navigation in MediaHelpers.GetFolderNavigationHierarchy(Model.MediaPath)) {%>
        <%: Html.ActionLink(navigation.FolderName, "Edit",
                  new {name = navigation.FolderName, mediaPath = navigation.FolderPath})%> &#62;
    <% } %>
    <%: T("Edit Media")%></p>
 </div>   
    
<div class="sections clearBoth">
	<%using (Html.BeginFormAntiForgeryPost()) { %>
        <%: Html.ValidationSummary() %>
        <div class="primary">
		    <div>
		    <img src="<%=Model.PublicUrl%>" class="previewImage" alt="<%: Model.Caption %>" />
		    </div>
		    <fieldset>
		        <%-- todo: make these real (including markup) --%>
			    <div>
			    <label><%: T("Dimensions: <span>500 x 375 pixels</span>")%></label>
			   
			    <label><%: T("Size: <span>{0}</span>", Model.Size)%></label>
			   
			    <label><%: T("Added on: <span>{0} by Orchard User</span>", Model.LastUpdated)%></label>
			    </div>
			    <div>
			        <label for="embedPath"><%: T("Embed:")%></label>
			        <input id="embedPath" class="textMedium" name="embedPath" type="text" readonly="readonly" value="<%: T("<img src=\"{0}\" width=\"{1}\" height=\"{2}\" alt=\"{3}\" />", ResolveUrl("~/Media/" + Model.RelativePath + "/" + Model.Name), 500, 375, Model.Caption)%>" />
			        <span class="hint"><%: T("Copy this html to add this image to your site.") %></span>
			      </div>
			  
                <div>
                    <label for="Name"><%: T("Name")%></label>
			        <input id="Name" name="Name" type="hidden" value="<%: Model.Name %>"/>
			        <input id="NewName" class="textMedium" name="NewName" type="text" value="<%: Model.Name %>"/>
			    </div>
                <div>
			        <label for="Caption"><%: T("Caption")%></label>
			        <input id="Caption" class="textMedium" name="Caption" type="text" value="<%= Model.Caption %>"/>
			        <span class="hint"><%: T("This will be used for the image alt tag.")%></span>
			        <input type="hidden" id="LastUpdated" name="LastUpdated" value="<%= Model.LastUpdated %>"/>
			        <input type="hidden" id="Size" name="Size" value="<%= Model.Size %>"/>
			        <input type="hidden" id="FolderName" name="FolderName" value="<%= Model.FolderName %>"/>
			        <input type="hidden" id="MediaPath" name="MediaPath" value="<%= Model.MediaPath %>" />
			    </div>
		    </fieldset>
		    <fieldset>
			    <input type="submit" class="button primaryAction" name="submit.Save" value="<%: T("Save") %>" />
			    <%--<input type="submit" class="button" name="submit.Delete" value="<%: T("Remove") %>" />--%>
            </fieldset>
	    </div>
	    <%--<div class="secondary" style="border:1px solid #ff0000;">
		    <h2><%: T("Preview")%></h2>
		    <div><img src="<%=ResolveUrl("~/Media/" + Html.Encode(Model.RelativePath + "/" + Model.Name))%>" class="previewImage" alt="<%: Model.Caption %>" /></div>
		    <ul>
		        <%-- todo: make these real (including markup) 
			    <li><label><%: T("Dimensions: <span>500 x 375 pixels</span>")%></label></li>
			    <li><label><%: T("Size: <span>{0}</span>", Model.Size)%></label></li>
			    <li><label><%: T("Added on: <span>{0} by Orchard User</span>", Model.LastUpdated)%></label></li>
			    <li>
			        <label for="embedPath"><%: T("Embed:")%></label>
			        <input id="embedPath" class="text" name="embedPath" type="text" readonly="readonly" value="<%: T("<img src=\"{0}\" width=\"{1}\" height=\"{2}\" alt=\"{3}\" />", ResolveUrl("~/Media/" + Model.RelativePath + "/" + Model.Name), 500, 375, Model.Caption) %>" />
			        <span class="hint"><%: T("Copy this html to add this image to your site.") %></p>
			    </li>
		    </ul>
	    </div>--%>
	<% } %>
</div>