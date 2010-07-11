﻿<%@ Control Language="C#" Inherits="Orchard.Mvc.ViewUserControl<Orchard.ContentTypes.ViewModels.EditPartViewModel>" %>
<div class="summary">
    <div class="properties">
        <h3><%:Model.Name%></h3>
    </div>
    <div class="related">
        <%:Html.ActionLink(T("Edit").ToString(), "EditPart", new {area = "Orchard.ContentTypes", id = Model.Name})%>
    </div>
</div>