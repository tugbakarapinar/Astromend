using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace RealTimeHub.Helper
{
    public class DbHelper
    {
        DB.Entities _db = new DB.Entities();
        DB.EntitiesSite _dbSite = new DB.EntitiesSite();
        public Task addMessage(string clientRecevierId, int senderUserID, string message, string language, bool isMobile, int receiverUserID, string key, bool temporaryMessage, bool isImage)
        {
            return Task.Run(() =>
            {
                DB.Messages messages = new DB.Messages()
                {
                    recid = Guid.Parse(key),
                    message = message,
                    clientRecevierId = clientRecevierId,
                    createdDate = DateTime.Now,
                    isMobile = isMobile,
                    language = language,
                    receiverUserID = receiverUserID,
                    seen = false,
                    senderUserID = senderUserID,
                    visibleUserID = 0,
                    isImage = isImage,
                    temporaryMessage = temporaryMessage,
                    senderProfileImage = _dbSite.usersDetail.FirstOrDefault(x => x.userRecid == senderUserID).islemResimler?.resim,
                    timeOut = false
                };
                _db.Messages.Add(messages);
                var nm = _db.NotificationMessages.
                FirstOrDefault(x => (x.receiverUserID == receiverUserID & x.senderUserId == senderUserID) || (x.receiverUserID == senderUserID & x.senderUserId == receiverUserID));
                if (nm != null)
                    _db.NotificationMessages.Remove(nm);

                DB.NotificationMessages notificationMessages = new DB.NotificationMessages()
                {
                    recid = Guid.NewGuid(),
                    isDeletedUser = 0,
                    createdDate = DateTime.Now,
                    isRead = false,
                    receiverUserID = receiverUserID,
                    senderUserId = senderUserID,
                    senderProfileImage = _dbSite.usersDetail.FirstOrDefault(x => x.userRecid == senderUserID).islemResimler?.resim,
                    senderNickName = _dbSite.users.FirstOrDefault(x => x.recid == senderUserID).rumuz,
                    receiverProfileImage = _dbSite.usersDetail.FirstOrDefault(x => x.userRecid == receiverUserID).islemResimler?.resim,
                    receiverNickName = _dbSite.users.FirstOrDefault(x => x.recid == receiverUserID).rumuz,
                    summaryMessage = message.Length > 90 ? message.Substring(0, 90) : message,
                    isImage = isImage,
                    isNotification = true
                };
                _db.NotificationMessages.Add(notificationMessages);
                _db.SaveChanges();
                #region Mesaj count yaz
                var queryUser = _dbSite.usersDetail.FirstOrDefault(x => x.userRecid == receiverUserID);
                string sql = "select count(senderUserId) total from NotificationMessages where receiverUserID = " + receiverUserID + " and isNotification = 1 group by senderUserId";
                int data = _db.Database.SqlQuery<int>(sql).FirstOrDefault();
                queryUser.firebase_mesaj = data;
                _dbSite.SaveChanges();
                #endregion
            });
        }
        public Task seenMessage(string clientRecevierId, int receiverUserID)
        {
            return Task.Run(() =>
            {
                foreach (var item in _db.Messages.Where(x => x.clientRecevierId == clientRecevierId & x.receiverUserID == receiverUserID & !x.seen))
                {
                    item.seen = true;
                }
                int senderUserId = int.Parse(clientRecevierId.Replace(receiverUserID.ToString(), "").Replace("!", ""));

                var notifications = _db.NotificationMessages.FirstOrDefault(x => x.receiverUserID == receiverUserID & x.senderUserId == senderUserId & x.isNotification);
                if (notifications != null)
                    notifications.isNotification = false;
                notifications.isRead = true;
                _db.SaveChanges();
                #region Mesaj count yaz
                var queryUser = _dbSite.usersDetail.FirstOrDefault(x => x.userRecid == receiverUserID);
                string sql = "select count(senderUserId) total from NotificationMessages where receiverUserID = " + receiverUserID + " and isNotification = 1 group by senderUserId";
                int data = _db.Database.SqlQuery<int>(sql).FirstOrDefault();
                queryUser.firebase_mesaj = data;
                _dbSite.SaveChanges();
                #endregion
            });
        }
    }
}