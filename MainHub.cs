using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace RealTimeHub
{
    public class MainHub : Hub
    {
        Helper.DbHelper helper = new Helper.DbHelper();
        public void SendMessage(string clientRecevierId, int senderUserID, string message, string language, bool isMobile, int receiveUserID, bool temporaryMessage = false, bool isImage = false, string senderProfileImage = "")
        {
            string dateTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            string key = Guid.NewGuid().ToString();
            if (!string.IsNullOrEmpty(message))
            {
                Clients.User(clientRecevierId).ReceiverMessage(key, dateTime, senderUserID, message, language, isMobile, senderProfileImage, isImage, temporaryMessage);
                helper.addMessage(clientRecevierId, senderUserID, message, language, isMobile, receiveUserID, key, temporaryMessage, isImage);
            }
        }
        public void MessageSeen(string clientRecevierId, int senderUserID, int receiverUserID)
        {
            Clients.User(clientRecevierId).ReceiverMessageSeen(clientRecevierId, receiverUserID, senderUserID);
            helper.seenMessage(clientRecevierId, receiverUserID);
        }
        public void SendTyping(string clientRecevierId, int senderUserID, bool typing)
        {
            Clients.User(clientRecevierId).ReceiverTyping(senderUserID, typing);
        }
    }
}