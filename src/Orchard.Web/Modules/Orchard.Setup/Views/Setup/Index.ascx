﻿<%@ Control Language="C#" Inherits="Orchard.Mvc.ViewUserControl<SetupViewModel>" %>
<%@ Import Namespace="Orchard.Mvc.Html"%>
<%@ Import Namespace="Orchard.Setup.ViewModels"%>
<h1><%=Html.TitleForPage(_Encoded("Get Started").ToHtmlString())%></h1>
<%
using (Html.BeginFormAntiForgeryPost()) { %>
<%=Html.ValidationSummary() %>
<h2><%=_Encoded("Please answer a few questions to configure your site.")%></h2>
<fieldset class="site">
    <div>
        <label for="SiteName"><%=_Encoded("What is the name of your site?") %></label>
        <%=Html.EditorFor(svm => svm.SiteName) %>
        <%=Html.ValidationMessage("SiteName", "*") %>
    </div>
    <div>
        <label for="AdminUsername"><%=_Encoded("Choose a user name:") %></label>
        <%=Html.EditorFor(svm => svm.AdminUsername)%>
        <%=Html.ValidationMessage("AdminUsername", "*")%>
    </div>
    <div>
        <label for="AdminPassword"><%=_Encoded("Choose a password:") %></label>
        <%=Html.PasswordFor(svm => svm.AdminPassword) %>
        <%=Html.ValidationMessage("AdminPassword", "*") %>
    </div>
</fieldset>
<fieldset class="data">
    <legend><%=_Encoded("How would you like to store your data?") %></legend>
    <%=Html.ValidationMessage("DatabaseOptions", "Unable to setup data storage") %>
    <div>
        <input type="radio" name="databaseOptions" id="builtin" value="true" checked="checked" />
        <label for="builtin" class="forcheckbox"><%=_Encoded("Use built-in data storage (SQL Lite)") %></label>
    </div>
    <div>
        <input type="radio" name="databaseOptions" id="sql" value="false" />
        <label for="sql" class="forcheckbox"><%=_Encoded("Use an existing SQL Server (or SQL Express) database") %></label>
        <span>
            <label for="DatabaseConnectionString"><%=_Encoded("Connection string") %></label>
            <%=Html.EditorFor(svm => svm.DatabaseConnectionString)%>
        </span>
    </div>
</fieldset>
<fieldset>
    <input class="button" type="submit" value="<%=_Encoded("Finish Setup") %>" />
</fieldset><%
} %>
<script type="text/javascript">
    $(function() {
        $("#sql").change(function() { $(this).siblings("span").slideDown(200).find("input").focus(); });
        $("#builtin").change(function() { $("#sql").siblings("span").slideUp(200); });
    });
</script>