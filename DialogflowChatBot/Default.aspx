﻿<%@ Page Title="Dialogflow Chatbot" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="DialogflowChatBot._Default" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <h2 class="text-center" style="margin-top: 0; padding-top: 30px; padding-bottom: 30px;"><%: Page.Title %></h2>
    <div class="container" id="chatwnd" style="height: 75vh; overflow: auto;">
        <div id="discussion" style="background-color: whitesmoke; "></div>
        <div style="width: 100%; border-left-style: ridge; border-right-style: ridge;">
            <asp:TextBox ID="message" style="width: 100%; padding: 5px 10px; border-style: hidden;" runat="server" 
                placeholder="Type message to be translated and press Enter to send..." Width="100%" BorderStyle="None"></asp:TextBox>
<%--            <textarea id="message"
                      style="width: 100%; padding: 5px 10px; border-style: hidden;"
                      placeholder="Type message to be translated and press Enter to send..."></textarea>--%>
        </div>
        <div style="overflow: auto; border-style: ridge; border-top-style: hidden;">
            <asp:Button CssClass="btn-warning pull-right" ID="echo" runat="server" Value="Echo" UseSubmitBehavior="false" OnClientClick="return ClickHiddenEcho();"
                title="Translate your message to the &#13;receiver's language and then &#13;back to your language" />
            <asp:Button CssClass="btn-success pull-right" ID="sendmessage" runat="server" Value="Send" UseSubmitBehavior="false" OnClientClick="return ClickHiddenSend();"
                title="Translate your message to the &#13;receiver's language and transmit" />
            <asp:DropDownList AutoPostBack="true" CssClass="pull-right" ID="ddlLanguageSelect" runat="server" style="width: 100pt; height: 18pt"
                title="Select your language" OnSelectedIndexChanged="ddlLanguageSelect_Change"></asp:DropDownList>

            <button id="hiddenecho" style="visibility: hidden;"/>
            <button id="hiddensend" style="visibility: hidden;"/>
<%--            <button class="btn-success pull-right" id="sendmessage" title="Translate your message to the &#13;receiver's language and transmit">Send</button>--%>
<%--            <select class="pull-right" id="languageselect" name="userlanguage">
                <option></option>
            </select>--%>
            <input type="hidden" id="displayname" />
            <input type="hidden" id="lastlanguage" />
         </div>
    </div>
    <!--Script references. -->
    <!--The jQuery library is required and is referenced by default in _Layout.cshtml. -->
    <!--Reference the SignalR library. -->
    <script src="/Scripts/jquery.signalR-2.2.2.min.js"></script>
    <!--Reference the autogenerated SignalR hub script. -->
    <script src="/signalr/hubs"></script>
    <!--SignalR script to update the chat page and send messages.--> 
    <script>

        //Stop Form Submission of Enter Key Press
        function stopRKey(evt) {
            var evt = (evt) ? evt : ((event) ? event : null);
            var node = (evt.target) ? evt.target : ((evt.srcElement) ? evt.srcElement : null);
            if ((evt.keyCode == 13) && (node.type == "text")) { return ClickHiddenSend(); }
        }
        document.onkeypress = stopRKey;

        function ClickHiddenEcho() {
            $('#hiddenecho').click();

            return false; //cancel event to stop postback
        }

        function ClickHiddenSend() {
            $('#hiddensend').click();

            return false; //cancel event to stop postback
        }

        $(function () {
            
            // Reference the auto-generated proxy for the hub.  
            var chat = $.connection.chatHub;

            chat.client.echoTranslate = function (message) {
                var entry = document.createElement('div');
                entry.classList.add("message-entry");
                entry.innerHTML = '<div class="message-avatar pull-left">' + $('#<%=echo.ClientID%>').val() + '</div>' +
                    '<div class="message-content pull-left" style="background-color: orange">' + message + '<div>';
                $('#discussion').append(entry);
                $("#discussion").animate({ scrollTop: $("#discussion")[0].scrollHeight }, 1);
            }

            chat.client.postMessageTranslation = function (name, message) {
                var entry = document.createElement('div');
                entry.classList.add("message-entry");
                if (name === "_SYSTEM_") {
                    entry.innerHTML = message;
                    entry.classList.add("text-center");
                    entry.classList.add("system-message");
                } else if (name === "_BROADCAST_") {
                    entry.classList.add("text-center");
                    entry.innerHTML = '<div class="text-center broadcast-message">' + message + '</div>';
                } else if (name === $('#displayname').val()) {
                    entry.innerHTML = '<div class="message-avatar pull-left">' + name + '</div>' +
                        '<div class="message-content pull-left">' + message + '<div>';
                } else {
                    entry.innerHTML = '<div class="message-avatar pull-right">' + name + '</div>' +
                        '<div class="message-content pull-right">' + message + '<div>';
                }

                $('#discussion').append(entry);
                $("#discussion").animate({ scrollTop: $("#discussion")[0].scrollHeight }, 1);
            }

            // Create a function that the hub can call back to display messages.
            chat.client.addNewMessageToPage = function (name, language, message) {

                if (!message) return;
                var encodedName = name;
                var encodedMsg = message.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
                //alert(encodedMsg);
                chat.server.translate(language, $('#<%=ddlLanguageSelect.ClientID%>').val(), encodedName, encodedMsg);

                if (language != $('#<%=ddlLanguageSelect.ClientID%>').val()) {
                    $('#lastlanguage').val(language);
                }
                //alert(messageEntry.innerHTML);
                // Add the message to the page.
                //$('#discussion').append(messageEntry);
                //$('#discussion').append('<strong>' + htmlEncode(name)
                //    + '</strong>: ' + htmlEncode(message));
            };

            // Get the user name and store it to prepend to messages.
            $('#displayname').val(prompt('Enter your name:', ''));

            $('#lastlanguage').val($('#<%=ddlLanguageSelect.ClientID%>').val());

            // Set initial focus to message input box.
            $('#message').focus();

            // Start the connection.
            $.connection.hub.start().done(function () {
                
                $('#hiddensend').click(function () {
                    // Call the Send method on the hub.
                    chat.server.send($('#displayname').val(), $('#<%=ddlLanguageSelect.ClientID%>').val(), $('#<%=message.ClientID%>').val());
                    // Clear text box and reset focus for next comment. 
                    $('#<%=message.ClientID%>').val('').focus();
                    return false;
                });

                $('#hiddenecho').click(function () {
                    
                    // Call the Echo method on the hub.
                    var fromVal = $('#<%=ddlLanguageSelect.ClientID%>').val();
                    var toVal = $('#lastlanguage').val();
                    var cleanMsg = $('#<%=message.ClientID%>').val().replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
                    chat.server.echoSent(fromVal, toVal, cleanMsg);
                    // Clear text box and reset focus for next comment. 
                    $('#<%=message.ClientID%>').val('').focus();
                    return false;
                });
            });
        });
        // This optional function html-encodes messages for display in the page.
        function htmlEncode(value) {
            var encodedValue = $('<div />').text(value).html();
            return encodedValue;
        }
    </script>

    </asp:Content>

