using Classes;
using Models.Ado;
using PagedList;
using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Controllers
{
    [RoutePrefix("app/api/account")]
    [Authorize]
    public class AccountController : ApiController
    {
        Models.Ado.Entities _db = new Models.Ado.Entities();
        Models.HubAdo.EntitiesHub _dbHub = new Models.HubAdo.EntitiesHub();
        app_api.Classes.Methods methods = new app_api.Classes.Methods();
        #region Üyelik bilgileri, paket bilgileri
        public class membership_inf
        {
            public string packageName { get; set; }
            public bool status { get; set; }
            public string startDate { get; set; }
            public string endDate { get; set; }
        }
        [HttpGet]
        [Route("membership-information")]
        public List<membership_inf> membership_information()
        {
            List<membership_inf> result = new List<membership_inf>();
            int currentUserRecid = methods.Request_userRecid();
            var query_1 = _db.goldElit.FirstOrDefault(x => x.userRecid == currentUserRecid);
            if (query_1 != null)
            {
                membership_inf table = new membership_inf()
                {
                    packageName = query_1.uyelikTurPaket.paketAdi.Replace("<br />", "").Trim(),
                    startDate = query_1.baslangicTarih.ToString("dd.MM.yyyy"),
                    endDate = query_1.bitisTarih.ToString("dd.MM.yyyy"),
                    status = query_1.bitisTarih < DateTime.Now ? false : true
                };
                result.Add(table);
            }
            return result;
        }        
        public class notifications_inf
        {
            public int recid { get; set; }
            public string title { get; set; }
            public string detail { get; set; }
            public string date { get; set; }
            public int profileRecid { get; set; }
        }
        public string notifireplace(string nickname, string title, string text)
        {
            string resultText = text.Replace("<b>", "").Replace("</b>", "").Replace("  ", " ");
            int iof = resultText.IndexOf("Puan Vermiştir.");
            if (iof > -1)
                resultText = resultText.Substring(0, iof + 15);
            if (title == "Paylaşımınız Beğenildi!")
                resultText = nickname + " Rumuzlu Üyemiz Sizin Paylaşımınızı Beğendi.";
            if (title == "Onaylı E-Posta/Telefon Bulunamadı")
                resultText = "Hesabınızda Onaylı E-posta veya Telefon numarası Bulunamadı. En Az Bir Tane Onaylı Hesap Gerekmektedir. Eğer Onaylı E-posta veya Telefon Bulunmaz İse, Şifre veya Rumuzunuzu Unuttuğunuzda Şifre Hatırlatıcıyı Kullanamazsınız. Hesap Kaybına Uğramak İstemiyorsanız Lütfen E-posta/Telefon Bilgilerinizi Onaylayınız.";
            return resultText.Replace("Adlı", "Rumuzlu");
        }
        public class inAppNotificationCounts
        {
            public int message { get; set; } = 0;
            public int favorite { get; set; } = 0;
            public int visitor { get; set; } = 0;
            public int likes { get; set; } = 0;
            public int virtualCount { get; set; } = 0;
        }
        [HttpGet]
        [Route("inapp-notification-counts")]
        public inAppNotificationCounts in_app_notification_counts()
        {
            inAppNotificationCounts result = new inAppNotificationCounts();
            int currentUserRecid = methods.Request_userRecid();
            var query = _db.usersDetail.First(x => x.userRecid == currentUserRecid);
            result.message = query.firebase_mesaj;
            result.favorite = query.firebase_favori;
            result.likes = query.firebase_begen;
            result.visitor = query.firebase_gezen;
            result.virtualCount = query.virtualBildirim.Value;
            return result;
        }
        [HttpPost]
        [Route("inapp-notification-counts-reset")]
        public void in_app_notification_counts_reset(int resetId)
        {
            int currentUserRecid = methods.Request_userRecid();
            var query = _db.usersDetail.First(x => x.userRecid == currentUserRecid);
            switch (resetId)
            {
                case 1:
                    query.firebase_mesaj = 0;
                    break;
                case 2:
                    query.firebase_favori = 0;
                    break;
                case 3:
                    query.firebase_gezen = 0;
                    break;
                case 4:
                    query.firebase_begen = 0;
                    break;
                case 5:
                    query.virtualBildirim = 0;
                    break;
            }
            _db.SaveChanges();
        }
        [HttpGet]
        [Route("messages")]
        public List<Models.HubAdo.Messages> messages(string clientRecevierId, int limit)
        {
            int currentUserRecid = methods.Request_userRecid();
            var query = _dbHub.Messages.Where(x => x.clientRecevierId == clientRecevierId & !x.visibleUserID.Equals(currentUserRecid)).OrderByDescending(x => x.createdDate).Take(limit).ToList().OrderBy(x => x.createdDate);
            return query.ToList();
        }
        [HttpPost]
        [Route("temporary-message-dispose")]
        public void temporary_message_dispose(string messageId)
        {
            Guid _recid = Guid.Parse(messageId);
            var query = _dbHub.Messages.FirstOrDefault(x => x.recid == _recid);
            query.timeOut = true;
            _dbHub.SaveChanges();
        }
        [HttpGet]
        [Route("notifications-messages-list")]
        public List<Models.HubAdo.NotificationMessages> notifications_messages_list()
        {
            int currentUserRecid = methods.Request_userRecid();
            var query = _dbHub.NotificationMessages.Where(x => x.isDeletedUser != currentUserRecid & (x.receiverUserID == currentUserRecid || x.senderUserId == currentUserRecid)).OrderBy(x => x.createdDate).ToList();
            return query.ToList();
        }
        [HttpPost]
        [Route("delete-message")]
        public void delete_Message(int memberID, bool allDelete)
        {
            int currentUserRecid = methods.Request_userRecid();
            List<int> allListUsers = new List<int>();
            if (allDelete)
            {
                var qllList = _dbHub.NotificationMessages.Where(x => x.isDeletedUser != currentUserRecid &
                (x.receiverUserID == currentUserRecid || x.senderUserId == currentUserRecid)).ToList();
                foreach (var item in qllList)
                {
                    int userRecid = item.senderUserId == currentUserRecid ? item.receiverUserID : item.senderUserId;
                    allListUsers.Add(userRecid);
                }
            }
            else
                allListUsers.Add(memberID);
            foreach (var profileItem in allListUsers)
            {
                int profile = profileItem;
                string clientRecevierId = currentUserRecid < profile ? currentUserRecid + "!" + profile : profile + "!" + currentUserRecid;
                var query = _dbHub.Messages.Where(x => x.clientRecevierId == clientRecevierId).ToList();
                foreach (var item in query)
                {
                    if (item.visibleUserID.Equals(0))
                        item.visibleUserID = currentUserRecid;
                    else
                        _dbHub.Messages.Remove(item);
                }
                var notifiQuery = _dbHub.NotificationMessages.
                    FirstOrDefault(x => (x.receiverUserID == currentUserRecid & x.senderUserId == profile) || (x.receiverUserID == profile & x.senderUserId == currentUserRecid));
                if (notifiQuery.isDeletedUser == 0)
                    notifiQuery.isDeletedUser = currentUserRecid;
                else
                    _dbHub.NotificationMessages.Remove(notifiQuery);
            }

            _dbHub.SaveChanges();
        }
        [HttpPost]
        [Route("update-messageilst-profile-image")]
        public void update_messageilst_profile_image(string recid)
        {
            Guid key = Guid.Parse(recid);
            var query = _dbHub.NotificationMessages.FirstOrDefault(x => x.recid == key);
            int currentUserRecid = methods.Request_userRecid();
            var currentUser = _db.users.First(x => x.recid == currentUserRecid);
            if (query != null)
            {
                if (query.receiverUserID == currentUserRecid)
                {
                    query.senderProfileImage = currentUser.usersDetail.cinsiyet == "E" ? "female.png" : "male.png";
                    query.receiverProfileImage = currentUser.usersDetail.cinsiyet != "E" ? "female.png" : "male.png";
                }
                else
                {
                    query.senderProfileImage = currentUser.usersDetail.cinsiyet != "E" ? "female.png" : "male.png";
                    query.receiverProfileImage = currentUser.usersDetail.cinsiyet == "E" ? "female.png" : "male.png";
                }
            }
            _dbHub.SaveChanges();
        }
        [HttpGet]
        [Route("notifications")]
        public List<notifications_inf> notifications(int page = 1)
        {
            List<notifications_inf> result = new List<notifications_inf>();
            int currentUserRecid = methods.Request_userRecid();
            foreach (var item in _db.usersNotifications.Where(x => x.userRecid.Equals(currentUserRecid) & !x.baslik.Contains("Adlı üyeden bir bildirim Var!") & !x.baslik.Contains("Adlı Üyeden Bildiriminiz Var !")).OrderByDescending(x => x.tarih).ToPagedList(page, 10))
            {
                int profileR = 0;
                #region bildirimden kullanıcıyı yakala. Eski yapıda olmayan bir işlem olduğu için böyle bir yol izlendi...
                try
                {
                    if (item.baslik.Contains("Tebrikler ! Yeni Puan"))
                    {
                        int index1 = item.detay.IndexOf("href=\"");
                        int index2 = item.detay.IndexOf("\">");
                        int opID = int.Parse(item.detay.Substring(index1, item.detay.Length - index1).Replace("\"", "").Replace("href=/Profil/", "").Replace(">Profiline Git</a>", "").Replace(" ", "").Trim());
                        var profile = _db.users.FirstOrDefault(x => x.recid.Equals(opID));
                        profileR = profile == null ? 0 : profile.recid;
                    }
                }
                catch (Exception ex) { }
                #endregion
                notifications_inf table = new notifications_inf()
                {
                    recid = item.recid,
                    date = item.tarih.Value.ToString("dd.MM.yyyy HH:mm"),
                    detail = notifireplace(item.rumuz.Trim(), item.baslik.Trim(), item.detay.Trim()).Trim(),
                    title = item.baslik.Trim(),
                    profileRecid = profileR
                };
                result.Add(table);
            }
            return result;
        }
        #endregion
        [HttpGet]
        [Route("profileImage")]
        public string profileImage()
        {
            int currentUserRecid = methods.Request_userRecid();
            return _db.usersDetail.First(x => x.userRecid.Equals(currentUserRecid)).islemResimler.resim.ToString();
        }
        [HttpPost]
        [Route("update-visit-time")]
        public void update_visit_time()
        {
            DateTime now = DateTime.Now;
            int currentUserRecid = methods.Request_userRecid();
            var query = _db.usersDetail.First(x => x.userRecid.Equals(currentUserRecid));
            query.aktifZaman = now.AddMinutes(45);
            query.sonGirisTarihi = now;
            query.vitrinZaman = now.AddMinutes(45);
            methods.getlocalinformation();
            _db.SaveChanges();
        }
        [HttpGet]
        [Route("photos")]
        public List<Models.User.Photos> photos()
        {
            List<Models.User.Photos> photos = new List<Models.User.Photos>();
            int currentUserRecid = methods.Request_userRecid();
            foreach (var item in _db.islemResimler.Where(x => x.kullanici.Equals(currentUserRecid)))
            {
                Models.User.Photos p = new Models.User.Photos()
                {
                    recid = item.recid,
                    name = item.resim,
                    status = item.onay,
                    order = item.resim.Contains("male") ? 1 : 0
                };
                photos.Add(p);

            }
            return photos.OrderBy(x => x.order).ToList();
        }
        [HttpPost]
        [Route("update/profile-image")]
        public void update_profile_image(int recid)
        {
            var res = _db.islemResimler.First(x => x.recid.Equals(recid));
            int currentUserRecid = methods.Request_userRecid();
            var us = _db.usersDetail.First(x => x.userRecid.Equals(currentUserRecid));
            us.profileImagesID = res.recid;

            DateTime dateTime = DateTime.Now;
            dateTime = dateTime.AddHours(-3);
            var query = _db.duvarCanli.FirstOrDefault(x => x.userRecid.Equals(currentUserRecid) & (x.tip.Equals(2) || x.tip.Equals(3)) & x.tarih >= dateTime);
            if (query == null)
            {
                Models.Ado.duvarCanli tableCanli = new Models.Ado.duvarCanli();
                tableCanli.begen = 0;
                tableCanli.durum = 1;
                tableCanli.detay = us.users.rumuz;
                tableCanli.routeBaslik = "Profil-Resmi-Guncellendi";
                tableCanli.tarih = DateTime.Now;
                tableCanli.tip = 3;
                tableCanli.userRecid = us.userRecid;
                _db.duvarCanli.Add(tableCanli);
            }
            _db.SaveChanges();

        }
        [HttpPost]
        [Route("add/profile-image")]
        public void add_profile_image(int recid, string imageName)
        {
            Models.Ado.islemResimler rs = new Models.Ado.islemResimler();
            rs = _db.islemResimler.First(x => x.recid.Equals(recid));
            rs.resim = imageName;
            rs.yuklenmeTarihi = DateTime.Now;
            rs.onay = 0;
            _db.SaveChanges();
        }
        [HttpPost]
        [Route("delete/profile-image")]
        public app_api.Classes.Result delete_profile_image(int recid)
        {
            app_api.Classes.Result result = new app_api.Classes.Result();
            app_api.Classes.Variables variables = new app_api.Classes.Variables();
            result.code = -1;
            int currentUserRecid = methods.Request_userRecid();
            var queryResUSer = _db.usersDetail.First(x => x.userRecid.Equals(currentUserRecid));
            Models.Ado.islemResimler resimler = new Models.Ado.islemResimler();
            resimler = _db.islemResimler.SingleOrDefault(x => x.recid.Equals(recid));
            resimler.onay = 0;
            if (!resimler.resim.Contains("male"))
            {
                string path = @"C:\inetpub\vhosts\" + variables.sitedomain + @"\httpdocs\" + "Ck-rReskaG61D65-4AsdER-aaDFt54tSAD" + @"\" + resimler.resim + "";
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }
            if (queryResUSer.profileImagesID == recid)
            {
                string resSon = "";
                if (queryResUSer.cinsiyet == "E")
                {
                    resSon = "male.png";
                }
                else
                {
                    if (queryResUSer.giyimTarzi == 5 || queryResUSer.giyimTarzi == 8)
                    {
                        resSon = "famale_2.png";
                    }
                    else
                    {
                        resSon = "famale.png";
                    }
                }
                var r = _db.islemResimler.First(x => x.kullanici == 0 & x.resim == resSon);
                queryResUSer.profileImagesID = r.recid;
                result.code = r.recid;
                result.message = r.resim;
            }
            if (queryResUSer.cinsiyet == "E")
            {
                resimler.resim = "male.png";
            }
            else
            {
                if (queryResUSer.giyimTarzi == 5 || queryResUSer.giyimTarzi == 8)
                {
                    resimler.resim = "famale_2.png";
                }
                else
                {
                    resimler.resim = "famale.png";
                }
            }
            _db.SaveChanges();
            return result;
        }
        [HttpPost]
        [Route("control/password")]
        public bool control_password(string password)
        {
            int currentUserRecid = methods.Request_userRecid();
            string passwordMd5 = methods.MD5_Create(password);
            var query = _db.users.FirstOrDefault(x => x.recid.Equals(currentUserRecid) & x.sifre.Equals(passwordMd5));
            if (query == null)
                return false;
            return true;
        }
        [HttpPost]
        [Route("update/password")]
        public string update_password(string password)
        {
            int currentUserRecid = methods.Request_userRecid();
            string passwordMd5 = methods.MD5_Create(password);
            var query = _db.users.FirstOrDefault(x => x.recid.Equals(currentUserRecid));
            query.sifre = passwordMd5;
            _db.SaveChanges();
            return passwordMd5;
        }
        [HttpPost]
        [Route("update/status")]
        public void update_status(int sRecid)
        {
            int currentUserRecid = methods.Request_userRecid();
            var query = _db.users.FirstOrDefault(x => x.recid.Equals(currentUserRecid));
            query.durumRecid = byte.Parse(sRecid.ToString());
            if (sRecid == (int)app_api.Classes.Enums.userStatus.hesapSilinmis)
            {
                query.cepTelefonu = null;
                query.cepTelefonuOnay = false;
                query.cepOnayActivCode = null;

                query.email = null;
                query.emailOnay = false;
                query.emailOnayActivCode = null;
            }
            _db.SaveChanges();          
        }
        [HttpPost]
        [Route("send/email-activation-code")]
        public app_api.Classes.Result send_activation_code(string emailadress)
        {
            app_api.Classes.Result result = new app_api.Classes.Result();
            try
            {
                app_api.Classes.Variables variables = new app_api.Classes.Variables();
                int currentUserRecid = methods.Request_userRecid();
                var mailControl = _db.users.FirstOrDefault(x => x.recid.Equals(currentUserRecid) & x.email.Equals(emailadress) & x.emailOnay.Equals(true));
                if (mailControl != null)
                {
                    result.success = false;
                    result.message = emailadress + " E-Posta adresi zaten hesabınızda onaylıdır.";
                    return result;
                }
                //var mailHas = _db.users.FirstOrDefault(x => x.recid != currentUserRecid & x.email.Equals(emailadress) & x.emailOnay.Equals(true));
                //if (mailHas != null)
                //{
                //    result.success = false;
                //    result.message = emailadress + " E-Posta adresi farklı bir kullanıcı tarafından onaylanmıştır. Hesap size ait ise, şifre sıfırlama ile hesabı geri alabilirsiniz";
                //    return result;
                //}
                Random rd = new Random();
                string akey = rd.Next(100000, 999999).ToString();
                string mailBody = System.IO.File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "Models/MailTemplates/_MailTemplateConfirmMailAdress.html");
                string bodyTeamplate = String.Format(mailBody, akey, emailadress.Trim());
                var sendMail = methods.SendEMAIL(emailadress.Trim(), "E-posta Aktivasyon Kodu", bodyTeamplate);
                if (sendMail.success)
                {
                    var q = _db.users.First(x => x.recid.Equals(currentUserRecid));
                    q.email = emailadress;
                    q.emailOnay = false;
                    q.emailOnayActivCode = akey;
                    result.success = true;
                    _db.SaveChanges();
                }
                else
                {
                    return sendMail;
                }
            }
            catch (Exception ex)
            {
                result.success = false;
                result.message = ex.Message;
                #region hatayı log olarak sakla...
                Models.Ado.C_Log_Errors c_Log_Errors = new Models.Ado.C_Log_Errors();
                c_Log_Errors.process = "Email Aktivasyon";
                c_Log_Errors.platform = "Application";
                c_Log_Errors.method = "account/send/email-activation-code";
                c_Log_Errors.parameters = "emailadress:" + emailadress;
                c_Log_Errors.errorMessage = ex.InnerException.InnerException.Message;
                methods.addErrorLog(c_Log_Errors);
                #endregion
            }
            return result;
        }
        [HttpPost]
        [Route("approve/email-activation-code")]
        public app_api.Classes.Result approve_email_activation_code(string key)
        {
            app_api.Classes.Result result = new app_api.Classes.Result();
            try
            {
                int currentUserRecid = methods.Request_userRecid();
                var query = _db.users.FirstOrDefault(x => x.recid.Equals(currentUserRecid) & x.emailOnayActivCode.Equals(key.Trim()));
                if (query == null)
                {
                    result.success = false;
                    return result;
                }

                var lastQ = _db.users.Where(x => !x.recid.Equals(currentUserRecid) & x.email.Equals(query.email));
                foreach (var item in lastQ)
                {
                    item.email = null;
                    item.emailOnayActivCode = null;
                    item.emailOnay = false;
                }
                if (lastQ.Count() > 0)
                    _db.SaveChanges();


                result.success = true;
                result.message = query.email.Trim();
                query.emailOnay = true;
                _db.SaveChanges();
            }
            catch (Exception ex)
            {
                result.success = false;
                result.message = ex.Message;
                #region hatayı log olarak sakla...
                Models.Ado.C_Log_Errors c_Log_Errors = new Models.Ado.C_Log_Errors();
                c_Log_Errors.process = "Email Aktivasyon";
                c_Log_Errors.platform = "Application";
                c_Log_Errors.method = "account/approve/email-activation-code";
                c_Log_Errors.parameters = "key:" + key;
                c_Log_Errors.errorMessage = ex.InnerException.InnerException.Message;
                methods.addErrorLog(c_Log_Errors);
                #endregion
            }
            return result;

        }
        [HttpPost]
        [Route("send/phone-activation-code")]
        public app_api.Classes.Result phone_activation_code(string phoneNumber)
        {
            app_api.Classes.Result result = new app_api.Classes.Result();
            try
            {
                app_api.Classes.Variables variables = new app_api.Classes.Variables();
                int currentUserRecid = methods.Request_userRecid();
                var phoneControl = _db.users.FirstOrDefault(x => x.recid.Equals(currentUserRecid) & x.cepTelefonu.Equals(phoneNumber) & x.cepTelefonuOnay.Equals(true));
                if (phoneControl != null)
                {
                    result.success = false;
                    result.message = phoneNumber + " Telefon numarası zaten hesabınızda onaylıdır.";
                    return result;
                }
                //var phoneHas = _db.users.FirstOrDefault(x => x.recid != currentUserRecid & x.cepTelefonu.Equals(phoneNumber) & x.cepTelefonuOnay.Equals(true));
                //if (phoneHas != null)
                //{
                //    result.success = false;
                //    result.message = phoneNumber + " Telefon numarası farklı bir kullanıcı tarafından onaylanmıştır. Hesap size ait ise, şifre sıfırlama ile hesabı geri alabilirsiniz";
                //    return result;
                //}
                Random rd = new Random();
                string akey = rd.Next(100000, 999999).ToString();
                string msg = variables.sitedomain + "'dan Mesajınız Var; Cep Telefonunu Güncellemek İçin Onay Kodunuz : " + akey + "";
                var sendSms = methods.SendSMS(phoneNumber, msg);
                if (sendSms.success)
                {

                    var q = _db.users.First(x => x.recid.Equals(currentUserRecid));
                    q.cepTelefonu = phoneNumber;
                    q.cepTelefonuOnay = false;
                    q.cepOnayActivCode = akey;
                    result.success = true;
                    _db.SaveChanges();

                }
                else
                {
                    return sendSms;
                }
            }
            catch (Exception ex)
            {
                result.success = false;
                result.message = ex.Message;
                #region hatayı log olarak sakla...
                Models.Ado.C_Log_Errors c_Log_Errors = new Models.Ado.C_Log_Errors();
                c_Log_Errors.process = "Telefon Aktivasyon";
                c_Log_Errors.platform = "Application";
                c_Log_Errors.method = "account/send/phone-activation-code";
                c_Log_Errors.parameters = "phoneNumber:" + phoneNumber;
                c_Log_Errors.errorMessage = ex.InnerException.InnerException.Message;
                methods.addErrorLog(c_Log_Errors);
                #endregion
            }
            return result;
        }
        [HttpPost]
        [Route("approve/phone-activation-code")]
        public app_api.Classes.Result phone_email_activation_code(string key)
        {
            app_api.Classes.Result result = new app_api.Classes.Result();
            try
            {
                int currentUserRecid = methods.Request_userRecid();
                var query = _db.users.FirstOrDefault(x => x.recid.Equals(currentUserRecid) & x.cepOnayActivCode.Equals(key.Trim()));
                if (query == null)
                {
                    result.success = false;
                    return result;
                }

                var lastQ = _db.users.Where(x => !x.recid.Equals(currentUserRecid) & x.cepTelefonu.Equals(query.cepTelefonu));
                foreach (var item in lastQ)
                {
                    item.cepTelefonu = null;
                    item.cepOnayActivCode = null;
                    item.cepTelefonuOnay = false;
                }
                if (lastQ.Count() > 0)
                    _db.SaveChanges();

                result.success = true;
                result.message = query.cepTelefonu.Trim();
                query.cepTelefonuOnay = true;
                _db.SaveChanges();
            }
            catch (Exception ex)
            {
                result.success = false;
                result.message = ex.Message;
                #region hatayı log olarak sakla...
                Models.Ado.C_Log_Errors c_Log_Errors = new Models.Ado.C_Log_Errors();
                c_Log_Errors.process = "Email Aktivasyon";
                c_Log_Errors.platform = "Application";
                c_Log_Errors.method = "account/approve/phone-activation-code";
                c_Log_Errors.parameters = "key:" + key;
                c_Log_Errors.errorMessage = ex.InnerException.InnerException.Message;
                methods.addErrorLog(c_Log_Errors);
                #endregion
            }
            return result;

        }
        [HttpGet]
        [Route("control/notification-mobile-application")]
        public bool notification_mobile_application()
        {
            // Web sitelerin yapısını bozmamak adına. Tersine çalışan mantık yaptım. Reject yani eğer bu tabloda kayıt yok ise, bildirime onay vermiştir. Kayıt var ise bildirim istemiyor demektir.
            int currentUserRecid = methods.Request_userRecid();
            return _db.tmp_WINF_Mobile_BILDIRIM_Reject.FirstOrDefault(x => x.userRecid.Equals(currentUserRecid)) == null ? true : false;
        }
        [HttpPost]
        [Route("update/notification-mobile-application-approve-reject")]
        public void notification_mobile_application_approve_reject()
        {
            int currentUserRecid = methods.Request_userRecid();
            var query = _db.tmp_WINF_Mobile_BILDIRIM_Reject.FirstOrDefault(x => x.userRecid.Equals(currentUserRecid));
            if (query == null)
            {
                Models.Ado.tmp_WINF_Mobile_BILDIRIM_Reject tmp_WINF_Mobile_BILDIRIM_Reject = new Models.Ado.tmp_WINF_Mobile_BILDIRIM_Reject();
                tmp_WINF_Mobile_BILDIRIM_Reject.userRecid = currentUserRecid;
                _db.tmp_WINF_Mobile_BILDIRIM_Reject.Add(tmp_WINF_Mobile_BILDIRIM_Reject);
                _db.SaveChanges();
            }
            else
            {
                _db.tmp_WINF_Mobile_BILDIRIM_Reject.Remove(query);
                _db.SaveChanges();
            }
        }
        [HttpGet]
        [Route("control/notification-sms")]
        public Models.User.NotificationSms notification_sms()
        {
            Models.User.NotificationSms result = new Models.User.NotificationSms();
            result.durum = false;
            int currentUserRecid = methods.Request_userRecid();
            var query = _db.tmp_WINF_SMS_BILDIRIM.FirstOrDefault(x => x.userRecid.Equals(currentUserRecid) & x.durum);
            if (query != null)
            {
                result.durum = true;
                result.gezen = query.gezen;
                result.mesaj = query.mesaj;
                result.online = query.online;
                result.recid = query.recid;
                result.saatAralikTip = query.saatAralikTip;
            }
            return result;
        }
        [HttpPost]
        [Route("update/notification-sms-approve")]
        public void notification_sms_approve(bool status)
        {
            int currentUserRecid = methods.Request_userRecid();
            var query = _db.tmp_WINF_SMS_BILDIRIM.FirstOrDefault(x => x.userRecid.Equals(currentUserRecid));
            query.durum = status;
            query.mesaj = true;
            query.gezen = true;
            query.saatAralikTip = 1;
            _db.SaveChanges();
        }
        [HttpPost]
        [Route("update/notification-sms-type")]
        public void notification_sms_type(int typeId, bool status)
        {
            int currentUserRecid = methods.Request_userRecid();
            var query = _db.tmp_WINF_SMS_BILDIRIM.FirstOrDefault(x => x.userRecid.Equals(currentUserRecid));
            if (typeId == 1)
                query.mesaj = status;
            else if (typeId == 2)
                query.gezen = status;
            _db.SaveChanges();
            if (status)
                query.durum = true;
            else
            {
                if (!query.gezen & !query.mesaj)
                    query.durum = false;
            }
            _db.SaveChanges();
        }
        [HttpPost]
        [Route("update/notification-time-type")]
        public void notification_time_type(int typeId)
        {
            int currentUserRecid = methods.Request_userRecid();
            var query = _db.tmp_WINF_SMS_BILDIRIM.FirstOrDefault(x => x.userRecid.Equals(currentUserRecid));
            query.saatAralikTip = byte.Parse(typeId.ToString());
            _db.SaveChanges();
        }
        [HttpPost]
        [Route("change/notication-mobile-app-token")]
        public void notication_mobile_app_token(string token)
        {
            int currentUserRecid = methods.Request_userRecid();
            var query = _db.users.First(x => x.recid.Equals(currentUserRecid));
            query.mobileAppNotification_Token = token;
            query.usersDetail.aktif = false;
            _db.SaveChanges();
        }
        [HttpPost]
        [Route("update/notification-mobile-app-badge-reset")]
        public void notification_mobile_app_badge_reset()
        {
            int currentUserRecid = methods.Request_userRecid();
            var query = _db.users.First(x => x.recid.Equals(currentUserRecid));
            query.mobileAppNotification_BadgeCount = 0;
            _db.SaveChanges();
        }
        [HttpGet]
        [Route("check/notification-count")]
        public int check_notification_count()
        {
            int currentUserRecid = methods.Request_userRecid();
            string re = _db.usersDetail.First(x => x.userRecid.Equals(currentUserRecid)).virtualBildirim.ToString();
            return string.IsNullOrEmpty(re) ? 0 : int.Parse(re);
        }
        [HttpGet]
        [Route("check/is-message-value")]
        public bool check_is_message_value()
        {
            int currentUserRecid = methods.Request_userRecid();
            return _db.goldElit.FirstOrDefault(x => x.userRecid.Equals(currentUserRecid) & x.bitisTarih > DateTime.Now) == null ? false : true;
        }
        [HttpPost]
        [Route("change/message-setting")]
        public void message_setting(string ages)
        {
            int currentUserRecid = methods.Request_userRecid();
            var query = _db.users.First(x => x.recid.Equals(currentUserRecid));
            query.usersDetail.ayarMesajYasAraligi = ages;
            _db.SaveChanges();
        }        
        [HttpGet]
        [Route("membership-signed-agremeents")]
        public List<signedAgremeents> membership_signed_agremeents()
        {
            int currentUserRecid = methods.Request_userRecid();
            var queryUser = _db.users.FirstOrDefault(x => x.recid.Equals(currentUserRecid));
            List<signedAgremeents> signedList = new List<signedAgremeents>();
            signedList = _db.signedAgremeents.Where(x => x.userRecid == currentUserRecid).ToList();
            signedAgremeents registerSigned = new signedAgremeents() { createDate = queryUser.uyelikTarihi, recid = currentUserRecid, userRecid = currentUserRecid, mPay = currentUserRecid, orderObjectId = currentUserRecid.ToString(), agreementType = 0 };
            signedList.Add(registerSigned);
            return signedList.OrderByDescending(x => x.createDate).ToList();
        }
    }
}
