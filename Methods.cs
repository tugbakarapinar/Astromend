using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Script.Serialization;

namespace app_api.Classes
{
    public class Methods
    {
        public MReturn Login(string userName, string password)
        {
            Models.Ado.Entities _db = new Models.Ado.Entities();
            MReturn mReturn = new MReturn();
            Classes.Result result = new Result();
            DataCompile dataCompile = new DataCompile();
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
            {
                result.success = false;
                result.message = "Kullanıcı adı veya şifre boş olamaz";
            }
            string _password = password.Length <= 16 ? MD5_Create(password) : password;
            if (isNumeric(userName))
                userName = phone_finally(userName);

            var query = _db.users.FirstOrDefault(y => (y.rumuz == (userName) || y.email == (userName) || y.cepTelefonu == (userName)) & y.sifre.Equals(_password));
            if (query == null)
            {
                result.success = false;
                var querythereis = _db.users.FirstOrDefault(y => (y.rumuz == (userName) || y.email == (userName) || y.cepTelefonu == (userName)) & y.durumRecid.Value.Equals(1));
                if (querythereis != null)
                {
                    result.message = "Hatalı şifre girdiniz. Lütfen tekrar deneyin.";
                    result.code = (int)Classes.Enums.ResultCode.Kullanici_var_Sifre_Hatali;
                }
                else
                    result.message = userName + " ile kayıtlı aktif bir hesap bulunmamaktadır.";
            }
            else
            {
                if (isNumeric(userName) & !query.cepTelefonuOnay)
                {
                    result.success = false;
                    result.code = (int)Classes.Enums.ResultCode.Ceptelefonu_Onaysiz;
                    mReturn.result = result;
                    return mReturn;
                }
                if (isEmail(userName) & !query.emailOnay)
                {
                    result.success = false;
                    result.code = (int)Classes.Enums.ResultCode.Email_Onaysiz;
                    mReturn.result = result;
                    return mReturn;
                }
                Models.Ado.usersDetail usDet = new Models.Ado.usersDetail();
                DateTime now = DateTime.Now;
                switch (query.durumRecid)
                {
                    case 1:
                        usDet = _db.usersDetail.FirstOrDefault(y => y.userRecid.Equals(query.recid));
                        if (usDet == null)
                        {
                            Models.Ado.users usRemove = _db.users.First(del => del.recid == query.recid);
                            _db.users.Remove(usRemove);
                            _db.SaveChanges();

                            result.success = false;
                            result.message = userName + " ile kayıtlı aktif bir hesap bulunmamaktadır.";
                            mReturn.result = result;
                            return mReturn;
                        }
                        usDet.aktifZaman = now.AddMinutes(45);
                        usDet.yas = yasGetir(usDet.dogumTarihi.Value);
                        usDet.sonGirisTarihi = now;
                        usDet.users.mobileAppNotification_BadgeCount = 0;
                        _db.SaveChanges();
                        mReturn.data = dataCompile.DCUser(query);
                        mReturn.key = usDet.userRecid;
                        result.success = true;
                        break;
                    case 2:
                        result.success = false;
                        result.code = (int)Classes.Enums.ResultCode.DetayEksik;
                        result.message = query.recid.ToString();
                        mReturn.result = result;
                        break;
                    case 3:
                        Models.Ado.users user = _db.users.First(x => x.recid.Equals(query.recid));
                        user.durumRecid = (int)Classes.Enums.userStatus.aktif;
                        usDet = _db.usersDetail.FirstOrDefault(y => y.userRecid.Equals(query.recid));
                        usDet.aktifZaman = now.AddMinutes(45);
                        usDet.yas = yasGetir(usDet.dogumTarihi.Value);
                        usDet.sonGirisTarihi = now;
                        result.success = true;
                        _db.SaveChanges();
                        mReturn.data = dataCompile.DCUser(user);
                        mReturn.key = usDet.userRecid;
                        break;
                    case 4:
                        result.success = false;
                        result.message = "Bu hesap silinmiştir. Artık erişilemez.";
                        break;
                    case 5:
                        result.success = false;
                        result.message = "Hesabınız kurallara uymadığınız için engellenmiştir.";
                        break;
                }
            }
            mReturn.result = result;
            return mReturn;
        }
        public int Request_userRecid()
        {
            var identity = (System.Security.Claims.ClaimsIdentity)HttpContext.Current.User.Identity;
            return int.Parse(identity.FindFirst("userRecid").Value);
        }
        public bool isNumeric(string text)
        {
            foreach (char chr in text)
            {
                if (!Char.IsNumber(chr)) return false;
            }
            return true;
        }
        public bool isEmail(string text)
        {
            const string strRegex = @"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}" +
             @"\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\" +
             @".)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$";

            return (new Regex(strRegex)).IsMatch(text);
        }
        #region SMS Request ve tanımlar...
        string Sms_Request(string PostAddress, string xmlData)
        {
            try
            {
                var res = "";
                byte[] bytes = Encoding.UTF8.GetBytes(xmlData);
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(PostAddress);
                request.Method = "POST";
                request.ContentLength = bytes.Length;
                request.ContentType = "text/xml";
                request.Timeout = 300000000;
                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(bytes, 0, bytes.Length);
                }
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        string message = String.Format(
                        "POST failed. Received HTTP {0}",
                        response.StatusCode);
                        throw new ApplicationException(message);
                    }
                    Stream responseStream = response.GetResponseStream();
                    using (StreamReader rdr = new StreamReader(responseStream))
                    {
                        res = rdr.ReadToEnd();
                    }
                    return res;
                }
            }
            catch
            {
                return "-1";
            }
        }
        public Result SendSMS(string phoneNumber, string message)
        {
            Result result = new Result();
            result.success = true;
            return result;
        }
        #endregion
        #region EMail Request ve tanımlar...
        public Result SendEMAIL(string to, string title, string message)
        {
            Result result = new Result();
            Variables variables = new Variables();
            result.success = true;
            try
            {
                System.Net.Mail.MailMessage mail = new MailMessage();
                mail.To.Add(to);
                mail.From = new MailAddress(variables.mailAdress, variables.mailtitle, System.Text.Encoding.UTF8);
                mail.Subject = title;
                mail.SubjectEncoding = System.Text.Encoding.UTF8;
                mail.Body = message;
                mail.BodyEncoding = System.Text.Encoding.UTF8;
                mail.IsBodyHtml = true;
                mail.Priority = MailPriority.High;
                SmtpClient client = new SmtpClient();
                client.Credentials = new System.Net.NetworkCredential(variables.mailAdress, variables.mailPassword);
                client.Port = 587;
                client.Host = variables.mailServer;
                client.EnableSsl = true;
                client.Send(mail);
            }
            catch (Exception ex)
            {
                result.message = ex.Message;
                result.success = false;
            }
            return result;
        }
        #endregion
        public string MD5_Create(string input)
        {
            MD5 md5Creat = MD5.Create();
            byte[] data = md5Creat.ComputeHash(Encoding.Default.GetBytes(input));
            StringBuilder build = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                build.Append(data[i].ToString("x4"));
            }
            return build.ToString();
        }
        private TripleDES tripCreat(string input)
        {
            MD5 MDD5 = new MD5CryptoServiceProvider();
            TripleDES TRP = new TripleDESCryptoServiceProvider();
            TRP.Key = MDD5.ComputeHash(Encoding.Unicode.GetBytes(input));
            TRP.IV = new byte[TRP.BlockSize / 8];
            return TRP;
        }
        public byte[] tripLeDes_Write(string key, string result)
        {
            TripleDES tles = tripCreat(result);
            ICryptoTransform ct = tles.CreateEncryptor();
            byte[] byt = Encoding.Unicode.GetBytes(key);
            return ct.TransformFinalBlock(byt, 0, byt.Length);
        }
        public string tripLeDes_Reader(string key, string result)
        {
            byte[] byt = Convert.FromBase64String(key);
            TripleDES des = tripCreat(result);
            ICryptoTransform ict = des.CreateDecryptor();
            byte[] resData = ict.TransformFinalBlock(byt, 0, byt.Length);
            return Encoding.Unicode.GetString(resData);
        }
        public string phone_finally(string phone)
        {
            if (phone.Substring(0, 1) != "0")
                phone = "0" + phone.ToString();
            return phone;
        }
        public int yasGetir(DateTime dTarihi)
        {
            try
            {
                //TimeSpan ts = DateTime.Now - dTarihi;
                //return Convert.ToInt32((ts.Days / 365).ToString());
                return DateTime.Now.Year - dTarihi.Year;
            }
            catch (Exception)
            {
                return 30;
            }
        }
        public byte onlineResult(DateTime x, bool y)
        {
            if (x >= DateTime.Now || y == true)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }
        public string aktifZaman(DateTime zaman, bool fake, byte online)
        {
            string cevrimici, azOnce, dakikaOnce, saatOnce, dun, gunOnce;

            cevrimici = "Çevrimiçi";
            azOnce = "Az Önce";
            dakikaOnce = "Dk. Önce";
            saatOnce = "Saat Önce";
            dun = "Dün";
            gunOnce = "Gün Önce";

            TimeSpan ts = DateTime.Now - zaman;
            if (fake & online == 1)
            {
                return cevrimici;
            }
            double ttday, tthour, ttminute;
            ttday = Math.Round(ts.TotalDays);
            tthour = Math.Round(ts.TotalHours);
            ttminute = Math.Round(ts.TotalMinutes);

            if (ttday == 0 & ttminute < 60 & tthour == 0)
            {
                if (ttminute <= 0)
                {
                    return azOnce;
                }
                else
                {
                    return ttminute + " " + dakikaOnce;
                }
            }
            else if (ttday <= 1)
            {
                if (ttday == 0 & tthour == 0 & ttminute == 60)
                {
                    return "1 " + saatOnce;
                }
                else if (ttday == 0)
                {
                    return tthour + " " + saatOnce;
                }
                else if (ttday == 0 & tthour < 24)
                {
                    return dun;
                }
                else
                {
                    return "1 " + gunOnce;
                }
            }
            else if (ttday > 1 & ttday <= 5)
            {
                return ttday + " " + gunOnce;
            }
            else
            {
                return zaman.ToString("dd.MM.yyyy");
            }
        }
        public string gecenZaman(DateTime zaman)
        {
            string azOnce, dakikaOnce, saatOnce, dun, gunOnce;
            azOnce = "Az Önce";
            dakikaOnce = "Dk. Önce";
            saatOnce = "Saat Önce";
            dun = "Dün";
            gunOnce = "Gün Önce";
            TimeSpan ts = DateTime.Now - zaman;
            double ttday, tthour, ttminute;
            ttday = Math.Round(ts.TotalDays);
            tthour = Math.Round(ts.TotalHours);
            ttminute = Math.Round(ts.TotalMinutes);

            if (ttday == 0 & ttminute < 60 & tthour == 0)
            {
                if (ttminute <= 3)
                {
                    return azOnce;
                }
                else
                {
                    return ttminute + " " + dakikaOnce;
                }
            }
            else if (ttday <= 1)
            {
                if (ttday == 0 & tthour == 0 & ttminute == 60)
                {
                    return "1 " + saatOnce;
                }
                else if (ttday == 0)
                {
                    return tthour + " " + saatOnce;
                }
                else if (ttday == 0 & tthour < 24)
                {
                    return dun;
                }
                else
                {
                    return "1 " + gunOnce;
                }
            }
            else if (ttday > 1 & ttday <= 5)
            {
                return ttday + " " + gunOnce;
            }
            else
            {
                return zaman.ToString("dd.MM.yyyy");
            }
        }
        public byte uyumYuzdeHesapla(int gezen, int profil)
        {
            Models.Ado.Entities _db = new Models.Ado.Entities();
            double oran = Convert.ToDouble(5.88);
            double sonuc = 0;
            var gezenQuery = _db.usersDetail.Where(x => x.userRecid.Equals(gezen)).First();
            var profilQuery = _db.usersDetail.Where(x => x.userRecid.Equals(profil)).First();
            if (gezenQuery.aranan_Yas1 >= profilQuery.aranan_Yas1 & gezenQuery.aranan_Yas2 <= profilQuery.aranan_Yas2)
            {
                sonuc = sonuc + oran;
            }
            if (gezenQuery.aranan_MedeniHal == profilQuery.aranan_MedeniHal || gezenQuery.aranan_MedeniHal == 0)
            {
                sonuc = sonuc + oran;
            }
            if (gezenQuery.aranan_Meslek == profilQuery.aranan_Meslek || gezenQuery.aranan_Meslek == 0)
            {
                sonuc = sonuc + oran;
            }
            if (gezenQuery.aranan_UlkeRecid == profilQuery.aranan_UlkeRecid || gezenQuery.aranan_UlkeRecid == 0)
            {
                sonuc = sonuc + oran;
            }
            if (gezenQuery.aranan_SehirRecid == profilQuery.aranan_SehirRecid || gezenQuery.aranan_SehirRecid == 0)
            {
                sonuc = sonuc + oran;
            }
            if (gezenQuery.aranan_EgitimDurumu == profilQuery.aranan_EgitimDurumu || gezenQuery.aranan_EgitimDurumu == 0)
            {
                sonuc = sonuc + oran;
            }
            if (gezenQuery.aranan_CocukDurumu == profilQuery.aranan_CocukDurumu || gezenQuery.aranan_CocukDurumu == 0)
            {
                sonuc = sonuc + oran;
            }
            if (gezenQuery.aranan_FizikselEngel == profilQuery.aranan_FizikselEngel || gezenQuery.aranan_FizikselEngel == 0)
            {
                sonuc = sonuc + oran;
            }
            if (gezenQuery.aranan_CalistigiKurum == profilQuery.aranan_CalistigiKurum || gezenQuery.aranan_CalistigiKurum == 0)
            {
                sonuc = sonuc + oran;
            }
            if (gezenQuery.aranan_calismaSekli == profilQuery.aranan_calismaSekli || gezenQuery.aranan_calismaSekli == 0)
            {
                sonuc = sonuc + oran;
            }
            if (gezenQuery.aranan_DiniInanc == profilQuery.aranan_DiniInanc || gezenQuery.aranan_DiniInanc == 0)
            {
                sonuc = sonuc + oran;
            }
            if (gezenQuery.aranan_Mezhep == profilQuery.aranan_Mezhep || gezenQuery.aranan_Mezhep == 0)
            {
                sonuc = sonuc + oran;
            }
            if (gezenQuery.aranan_sigaraIcsinmi == profilQuery.aranan_sigaraIcsinmi || gezenQuery.aranan_sigaraIcsinmi == 0)
            {
                sonuc = sonuc + oran;
            }
            if (gezenQuery.aranan_alkolIcsinmi == profilQuery.aranan_alkolIcsinmi || gezenQuery.aranan_alkolIcsinmi == 0)
            {
                sonuc = sonuc + oran;
            }
            if (gezenQuery.aranan_NamazKilsinMi == profilQuery.aranan_NamazKilsinMi || gezenQuery.aranan_NamazKilsinMi == 0)
            {
                sonuc = sonuc + oran;
            }
            if (gezenQuery.aranan_OrucTutsunmu == profilQuery.aranan_OrucTutsunmu || gezenQuery.aranan_OrucTutsunmu == 0)
            {
                sonuc = sonuc + oran;
            }
            if (gezenQuery.aranan_GiyimTarzi == profilQuery.aranan_GiyimTarzi || gezenQuery.aranan_GiyimTarzi == 0)
            {
                sonuc = sonuc + oran;
            }
            byte result = Convert.ToByte(Math.Ceiling(Math.Round(sonuc, 3)).ToString());
            return result < 30 ? byte.Parse("30") : result;
        }
        public string turkishConvertURL(string txt)
        {

            string[] tr = { "ı", "ğ", "İ", "Ğ", "ç", "Ç", "ş", "Ş", "ö", "Ö", "ü", "Ü", " ", "'", "?", "!", ".", ",", ":", ";", "*", "\"", "\\", "/", "+", "%", "#", "[", "[" };
            string[] en = { "i", "g", "I", "G", "c", "C", "s", "S", "o", "O", "u", "U", "-", "-", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" };
            for (int i = 0; i < tr.Length; i++)
            {

                txt = txt.Replace(tr[i], en[i]);

            }
            return txt.TrimEnd().TrimStart();
        }
        public void push_notification_mobile_app(int userRecid, string title, string message, int notification_type)
        {
            try
            {
                Models.Ado.Entities _db = new Models.Ado.Entities();
                int currentUserRecid = Request_userRecid();
                var sentUser = _db.users.First(x => x.recid.Equals(currentUserRecid));
                app_api.Classes.Variables variables = new app_api.Classes.Variables();
                var receiver_notification_permisson_rejected = _db.tmp_WINF_Mobile_BILDIRIM_Reject.FirstOrDefault(x => x.userRecid.Equals(userRecid));
                if (receiver_notification_permisson_rejected == null)
                {
                    var receiver_user = _db.users.First(x => x.recid.Equals(userRecid));
                    if (!string.IsNullOrEmpty(receiver_user.mobileAppNotification_Token))
                    {
                        string deviceId = receiver_user.mobileAppNotification_Token;
                        WebRequest tRequest = WebRequest.Create("https://fcm.googleapis.com/fcm/send");
                        tRequest.Method = "post";
                        tRequest.ContentType = "application/json";
                        Int16 _badgeCount = Convert.ToInt16(receiver_user.mobileAppNotification_BadgeCount + 1);
                        var data = new
                        {
                            to = deviceId,
                            notification = new
                            {
                                title = title,
                                body = message,
                                sound = "default",
                                image = variables.siteUserProfileImageAdress + sentUser.usersDetail.islemResimler.resim,
                            },
                            data = new
                            {
                                sent_userRecid = currentUserRecid,
                                sent_userImage = sentUser.usersDetail.islemResimler.resim,
                                sent_userNickname = sentUser.rumuz,
                                notification_type = notification_type,
                                badgeCount = _badgeCount == 0 ? 1 : _badgeCount
                            },
                        };
                        var serializer = new JavaScriptSerializer();
                        var json = serializer.Serialize(data);
                        Byte[] byteArray = Encoding.UTF8.GetBytes(json);
                        tRequest.Headers.Add(string.Format("Authorization: key={0}", variables.applicationID_firebase));
                        tRequest.Headers.Add(string.Format("Sender: id={0}", variables.senderId_firebase));
                        tRequest.ContentLength = byteArray.Length;
                        using (Stream dataStream = tRequest.GetRequestStream())
                        {
                            dataStream.Write(byteArray, 0, byteArray.Length);
                            using (WebResponse tResponse = tRequest.GetResponse())
                            {
                                using (Stream dataStreamResponse = tResponse.GetResponseStream())
                                {
                                    using (StreamReader tReader = new StreamReader(dataStreamResponse))
                                    {
                                        String sResponseFromServer = tReader.ReadToEnd();
                                        string str = sResponseFromServer;
                                        receiver_user.mobileAppNotification_BadgeCount = Convert.ToInt16(_badgeCount == 0 ? 1 : _badgeCount);
                                        _db.SaveChanges();

                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string str = ex.Message;
            }
        }
        public string GetClientIp()
        {
            var ipAddress = string.Empty;
            if (System.Web.HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"] != null)
            { ipAddress = System.Web.HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"].ToString(); }

            else if (System.Web.HttpContext.Current.Request.ServerVariables["HTTP_CLIENT_IP"] != null && System.Web.HttpContext.Current.Request.ServerVariables["HTTP_CLIENT_IP"].Length != 0) { ipAddress = System.Web.HttpContext.Current.Request.ServerVariables["HTTP_CLIENT_IP"]; } else if (System.Web.HttpContext.Current.Request.UserHostAddress.Length != 0) { ipAddress = System.Web.HttpContext.Current.Request.UserHostName; }

            return ipAddress;
        }
        public string GetClientPort()
        {
            return System.Web.HttpContext.Current.Request.ServerVariables["SERVER_PORT"];
        }
        public string GetClientBrowser()
        {
            return System.Web.HttpContext.Current.Request.Browser.Browser;
        }
        public string GetClientLanguage()
        {
            return HttpContext.Current.Request.UserLanguages[0].Substring(0, 2);
        }
        public string GetClientSystem()
        {
            return System.Web.HttpContext.Current.Request.UserAgent.ToString();
        }
        public void getlocalinformation()
        {
            try
            {
                Models.Ado.Entities _db = new Models.Ado.Entities();
                int userRecid = Request_userRecid();
                string clientIp = GetClientIp().Trim();
                var query = _db.C_Log_UserLocal.FirstOrDefault(x => x.userRecid.Equals(userRecid) & x.client_ip.Equals(clientIp) & x.client_system.Equals("APP"));
                if (query == null)
                {
                    Models.Ado.C_Log_UserLocal table = new Models.Ado.C_Log_UserLocal();
                    table.userRecid = userRecid;
                    table.client_ip = clientIp;
                    table.client_port = GetClientPort();
                    table.client_browser = "APP";
                    table.client_language = "APP";
                    table.isMobile = true;
                    table.client_system = "APP";
                    table.date_utc = DateTime.Now;
                    _db.C_Log_UserLocal.Add(table);
                    _db.SaveChanges();
                }
            }
            catch (Exception)
            {
            }

        }
        public string farketmezTranslate(string dil)
        {
            switch (dil)
            {
                case "tr":
                    return "Farketmez";
                case "en":
                    return "I don't mind";
                case "id":
                    return "Tidak masalah";
                case "ar":
                    return "ليس مهما";
                case "fa":
                    return "مهم نیست";
                default:
                    return "Farketmez";
            }
        }
        public bool nicknameBarrier(string value)
        {
            string[] noElementArray = { "yandex", "mail", "outlook", "windowslive", "gmail", "hotmail", ".com", ".net", ".org", "@", "!", "\"", "'", "#", "+", "$", "₺", "€", "^", "%", "&", "/", "{", "(", "[", ")", "]", "=", "?", "\\", "*", "¨", "~", "â", "û", "ê", "ô", "î", ",", ":", ";", "`", "|", "<", ">", " ", "530", "531", "532", "533", "534", "535", "536", "537", "538", "539", "540", "541", "542", "543", "544", "545", "546", "547", "548", "549", "505", "506", "507", "551", "552", "553", "554", "555", "556", "557", "558", "559" };
            foreach (var item in noElementArray)
            {
                if (value.IndexOf(item) > -1)
                    return true;
            }
            return false;
        }
        public async void addErrorLog(Models.Ado.C_Log_Errors c_Log_Errors)
        {
            c_Log_Errors.createdDate = DateTime.Now;
            Models.Ado.Entities _db = new Models.Ado.Entities();
            _db.C_Log_Errors.Add(c_Log_Errors);
            _db.SaveChanges();            
        }
    }
}