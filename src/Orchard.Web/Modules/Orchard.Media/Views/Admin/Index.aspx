<%@ Page Language="C#" Inherits="Orchard.Mvc.ViewPage<MediaFolderIndexViewModel>" %>
<%@ Import Namespace="Orchard.Media.ViewModels"%>
<h1><%: Html.TitleForPage(T("Manage Media Folders").ToString()) %></h1>
<% using(Html.BeginFormAntiForgeryPost()) { %>
    <%: Html.ValidationSummary() %>
    <fieldset class="actions bulk">
        <label for="publishActions"><%: T("Actions:") %></label>
		<select id="Select1" name="publishActions">
		    <option value="1"><%: T("Remove")%></option>
		</select>
		<input class="button roundCorners" type="submit" value="<%: T("Apply") %>" />
    </fieldset>
    <div class="manage"><%: Html.ActionLink(T("Add a folder").ToString(), "Create", new { }, new { @class = "button primaryAction" })%></div>
    <fieldset>
	    <table class="items" summary="<%: T("This is a table of the media folders currently available for use in your application.") %>">
		    <colgroup>
			    <col id="Col1" />
			    <col id="Col2" />
			    <col id="Col3" />
			    <col id="Col4" />
			    <col id="Col5" />
			    <col id="Col6" />
		    </colgroup>
		    <thead>
			    <tr>
				    <th scope="col">&nbsp;&darr;<%-- todo: (heskew) something more appropriate for "this applies to the bulk actions --%></th>
				    <th scope="col"><%: T("Name")%></th>
				    <th scope="col"><%: T("Author") %></th>
				    <th scope="col"><%: T("Last Updated") %></th>
				    <th scope="col"><%: T("Type") %></th>
				    <th scope="col"><%: T("Size") %></th>
			    </tr>
		    </thead>
            <%foreach (var mediaFolder in Model.MediaFolders) {
            %>
            <tr>
                <td><input type="checkbox" value="true" name="<%: T("Checkbox.{0}", mediaFolder.Name) %>"/></td>
                <%-- todo: (heskew) this URL needs to be determined from current module location --%>
                <td>
                    <img src="<%=ResolveUrl("~/Modules/Orchard.Media/Content/Admin/images/folder.gif")%>" height="16" width="16" class="mediaTypeIcon" alt="<%: T("Folder") %>" />
                    <%: Html.ActionLink(mediaFolder.Name, "Edit", new { name = mediaFolder.Name, mediaPath = mediaFolder.MediaPath })%>
                </td>
                <td><%: T("Orchard User")%></td>
                <td><%=mediaFolder.LastUpdated %></td>
                <td><%: T("Folder")%></td>
                <td><%=mediaFolder.Size %></td>
            </tr>
            <%}%>
        </table>
    </fieldset>
    <div class="manage"><%: Html.ActionLink(T("Add a folder").ToString(), "Create", new { }, new { @class = "button primaryAction" })%></div>
<% } %>