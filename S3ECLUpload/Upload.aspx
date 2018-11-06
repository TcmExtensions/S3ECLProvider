<%@ Page Language="C#" ClientIDMode="Static" CodeFile="Upload.aspx.cs" Inherits="S3ECLProvider.Web.UploadPopup" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html class="popup" xmlns="http://www.w3.org/1999/xhtml">
	<head>
		<title>Upload Asset to S3</title>
		<link rel="shortcut icon" href="<%=ThemePath%>Images/Ico/favicon.ico" type="image/x-icon" />

		<cc:TridionManager runat="server" Editor="CME">
			<dependencies runat="server">		
				<dependency runat="server">Tridion.Web.UI.Editors.CME</dependency>
			</dependencies>
		</cc:TridionManager>

        <script type="text/javascript">
            function updateType(fileInput) {
                if (fileInput.files.length > 0) {
                    var type = fileInput.files[0].type;

                    if (!type || type == "") {
                        type = "application/octet-stream";
                    }

                    document.getElementsByName("Content-Type")[0].value = type;
                }
            }
        </script>
	</head>
	<body class="popupview">
        <asp:PlaceHolder runat="server" ID="phForm">
            <form action="<%=Action%>" method="post" enctype="multipart/form-data">
                <!-- Default values -->
                <input type="hidden" name="acl" value="public-read" />
                <input type="hidden" name="Cache-Control" value="<%=CacheControl%>" />
                <input type="hidden" name="policy" value="<%=GeneratePolicy() %>" />
                <input type="hidden" name="success_action_redirect" value="<%=SuccessRedirect %>" />

                <input type="hidden" name="x-amz-algorithm" value="AWS4-HMAC-SHA256" />
                <input type="hidden" name="x-amz-credential" value="<%=Credential%>" />
                <input type="hidden" name="x-amz-date" value="<%=Date%>" />
                <input type="hidden" name="x-amz-signature" value="<%=GetSignature() %>" />

                <div class="stack horizontal fixed">
                    <div class="form fieldgroup fieldbuilder">
                        <div class="field">
                            <label for="key"><span class="asterisk">*</span>Object key:</label>
                            <div class="input">
                                <input type="text" class="text" maxlength="250" name="key" value="<%=Prefix %>" />
                            </div>
                        </div>
                        <div class="field">
                            <label for="Content-Type"><span class="asterisk">*</span>Content-Type:</label>
                            <div class="input">
                                <input type="text" class="text" maxlength="250" name="Content-Type" value="image/jpeg" />
                            </div>
                        </div>
                        <div class="field">
                            <label for="file"><span class="asterisk">*</span>File:</label><br /><br />
                            <input type="file" class="tridion button file" name="file" style="width: 500px;" onchange="updateType(this);" /><br />
                        </div>
                    </div>
                    <button style="user-select: none; float: right; margin: 10px 5px 0 0;" class="tridion button mouseover" name="submit" type="submit" value="submit">Upload</button>
                </div>
            </form>
        </asp:PlaceHolder>
        <asp:PlaceHolder runat="server" ID="phMessage" Visible="false">
            <div style="margin: 5px 20px;">
                <p>
                    <asp:Literal runat="server" ID="litMessage" />
                </p>                
            </div>
        </asp:PlaceHolder>
	</body>
</html>
