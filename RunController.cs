using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using System.Web.Script.Serialization;

namespace Controllers
{
    [RoutePrefix("app/api/run")]
    public class RunController : ApiController
    {
        Models.Ado.Entities _db = new Models.Ado.Entities();
        app_api.Classes.Methods methods = new app_api.Classes.Methods();
        public class prices
        {
            public int id { get; set; }
            public decimal price { get; set; }
        }
        [HttpGet]
        [Route("list/prices")]
        public List<prices> list_pices()
        {
            List<prices> _prices = new List<prices>();
            foreach (var item in _db.uyelikTurPaket.Where(x => x.paketGecerli & (x.recid == 1 || x.recid == 4 || x.recid == 5)))
            {
                prices _p = new prices()
                {
                    id = item.recid,
                    price = item.paketFiyat
                };
                _prices.Add(_p);
            }
            return _prices;
        }
        #region kullanıcının uyumluluk oranına göre hesaplama daha önce yapılmış mı?
        [Authorize]
        [HttpGet]
        [Route("harmony-status")]
        public bool harmony_status()
        {
            int currentUserRecid = methods.Request_userRecid();
            var currentUser = _db.users.First(x => x.recid.Equals(currentUserRecid));
            if (currentUser.usersDetail.uygunlukAnalizTarihi == null)
                return false;
            else
            {
                TimeSpan span = DateTime.Now - currentUser.usersDetail.uygunlukAnalizTarihi.Value;
                if (span.Days >= 15)
                {
                    _db.Database.ExecuteSqlCommand("delete usersUygunUyeler where kullanici=" + currentUserRecid + "");
                    return false;
                }
                return true;
            }
        }
        #endregion
        #region kullanıcının uyumluluk oranını hesaplayıp tampon tabloda sakla
        [Authorize]
        [HttpPost]
        [Route("harmony-create")]
        public int harmony_creat()
        {
            int currentUserRecid = methods.Request_userRecid();
            var currentUser = _db.users.First(x => x.recid.Equals(currentUserRecid));
            _db.Database.ExecuteSqlCommand("delete usersUygunUyeler where kullanici=" + currentUserRecid + "");

            // Üyeleri yakala... Sorgu yükünün azalması için olabildiğince kriterleri daralt...
            var query = _db.usersDetail.Where(x => x.users.durumRecid.Value == 1 & x.detayDurum == true & x.cinsiyet != currentUser.usersDetail.cinsiyet).OrderByDescending(x => x.userRecid)
                               .Where(e => !_db.islemEngel.Where(l => l.engelleyen == currentUserRecid & l.engellenen == e.userRecid).Any())
                               .Where(e => !_db.islemEngel.Where(l => l.engelleyen == e.userRecid & l.engellenen == currentUserRecid).Any())
                               .Where(x => x.aranan_Yas1 >= currentUser.usersDetail.aranan_Yas1 & x.aranan_Yas2 <= currentUser.usersDetail.aranan_Yas2 & (x.aranan_MedeniHal == currentUser.usersDetail.aranan_MedeniHal || x.aranan_MedeniHal == 0) & (x.aranan_UlkeRecid == currentUser.usersDetail.aranan_UlkeRecid || x.aranan_UlkeRecid == 0));

            // Hesapla ve oranları 70 den fazla çıkanları tampon tabloya yaz. En fazla 50 kişi üzerinden yap...
            int say = 0;
            foreach (var item in query)
            {
                byte oran = methods.uyumYuzdeHesapla(currentUserRecid, item.userRecid);
                if (oran >= 70)
                {
                    Models.Ado.usersUygunUyeler table = new Models.Ado.usersUygunUyeler();
                    say += 1;
                    table.kullanici = currentUserRecid;
                    table.tarih = DateTime.Now;
                    table.userRecid = item.userRecid;
                    table.uyumOrani = oran;
                    _db.usersUygunUyeler.Add(table);
                    if (say == 75)
                        break;
                }
            }
            currentUser.usersDetail.uygunlukAnalizTarihi = DateTime.Now;
            _db.SaveChanges();
            return say;
        }
        #endregion       
        [HttpGet]
        [Route("list/citys")]
        public List<Classes.SelectorItem> list_citys(int countryRecid)
        {
            List<Classes.SelectorItem> _citys = new List<Classes.SelectorItem>();
            foreach (var item in _db.city.Where(x => x.country.recid == countryRecid))
            {
                Classes.SelectorItem _item = new Classes.SelectorItem()
                {
                    key = item.recid,
                    label = item.ilAdi
                };
                _citys.Add(_item);
            }
            return _citys.OrderBy(x => x.label).ToList();
        }
        [HttpPost]
        [Route("feedback-contact")]
        public app_api.Classes.Result feedback_contact(string device, string ipAdress, string nameSurname, string email, string phone, string subject, string message)
        {
            app_api.Classes.Result result = new app_api.Classes.Result();
            try
            {
                Models.Ado.defaultContact defaultContact = new Models.Ado.defaultContact();
                defaultContact.device = device.Trim();
                defaultContact.adSoyad = nameSurname.Trim();
                defaultContact.e_posta = string.IsNullOrEmpty(email) ? "" : email.Trim();
                defaultContact.telefon = string.IsNullOrEmpty(phone) ? "" : phone.Trim();
                defaultContact.konu = subject.Trim();
                defaultContact.mesaj = message.Trim();
                defaultContact.tarih = DateTime.Now;
                defaultContact.ip_adres = ipAdress.Trim();
                defaultContact.tip = 1;
                var identity = (System.Security.Claims.ClaimsIdentity)System.Web.HttpContext.Current.User.Identity;
                defaultContact.userRecid = identity.Claims.Count() > 0 ? int.Parse(identity.FindFirst("userRecid").Value) : 0;
                _db.defaultContact.Add(defaultContact);
                _db.SaveChanges();
                result.success = true;
            }
            catch (Exception ex)
            {
                result.success = false;
                result.message = ex.Message;
            }
            return result;
        }
        [HttpPost]
        [Route("feedback-bug")]
        public app_api.Classes.Result feedback_bug(string device, string ipAdress, string phone, string message)
        {
            app_api.Classes.Result result = new app_api.Classes.Result();
            try
            {
                Models.Ado.defaultSorunBildir table = new Models.Ado.defaultSorunBildir();
                table.device = device.Trim();
                table.sorun = "Telefon Numarası : " + phone.Trim() + " " + message.Trim();
                table.tarih = DateTime.Now;
                table.ip_Adress = ipAdress.Trim();
                table.tarayici = "Application";
                table.browser = "Application";
                table.url = "Application";
                table.sonuc = false;
                var identity = (System.Security.Claims.ClaimsIdentity)System.Web.HttpContext.Current.User.Identity;
                table.userRecid = identity.Claims.Count() > 0 ? int.Parse(identity.FindFirst("userRecid").Value) : 0;
                _db.defaultSorunBildir.Add(table);
                _db.SaveChanges();
                result.success = true;
            }
            catch (DbEntityValidationException e)
            {
                foreach (var eve in e.EntityValidationErrors)
                {
                    //Console.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                    //  eve.Entry.Entity.GetType().Name, eve.Entry.State);
                    foreach (var ve in eve.ValidationErrors)
                    {
                        result.message += "Property: " + ve.PropertyName + " - Error: " + ve.ErrorMessage;
                    }
                }
                result.success = false;
            }
            return result;
        }
        [HttpPost]
        [Route("notification-reset")]
        public void notification_reset()
        {
            int currentUserRecid = methods.Request_userRecid();
            foreach (var item in _db.usersNotifications.Where(x => x.userRecid.Equals(currentUserRecid) & x.okundu.Equals(false)))
            {
                item.okundu = true;
            }
            Models.Ado.usersDetail det = new Models.Ado.usersDetail();
            det = _db.usersDetail.First(x => x.userRecid.Equals(currentUserRecid));
            det.virtualBildirim = 0;
            _db.SaveChanges();
        }
        #region Resim işleme
        // Base64 convert bitmap
        public System.Drawing.Bitmap Base64StringToBitmap(string base64String)
        {
            System.Drawing.Bitmap bmpReturn = null;
            byte[] byteBuffer = Convert.FromBase64String(base64String);
            System.IO.MemoryStream memoryStream = new System.IO.MemoryStream(byteBuffer);
            memoryStream.Position = 0;
            bmpReturn = (System.Drawing.Bitmap)System.Drawing.Bitmap.FromStream(memoryStream);
            memoryStream.Close();
            memoryStream = null;
            byteBuffer = null;
            return bmpReturn;
        }
        // Sıkıştır
        private static System.Drawing.Imaging.ImageCodecInfo GetEncoderInfo(System.Drawing.Imaging.ImageFormat format)
        {
            return System.Drawing.Imaging.ImageCodecInfo.GetImageDecoders().SingleOrDefault(c => c.FormatID == format.Guid);
        }
        public static void ResizeImage(System.Drawing.Bitmap image, int maxWidth, int maxHeight, int quality, string filePath)
        {
            int originalWidth = image.Width;
            int originalHeight = image.Height;
            float ratioX = (float)maxWidth / (float)originalWidth;
            float ratioY = (float)maxHeight / (float)originalHeight;
            float ratio = Math.Min(ratioX, ratioY);
            int newWidth = (int)(originalWidth * ratio);
            int newHeight = (int)(originalHeight * ratio);
            System.Drawing.Bitmap newImage = new System.Drawing.Bitmap(newWidth, newHeight, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(newImage))
            {
                graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                graphics.DrawImage(image, 0, 0, newWidth, newHeight);
            }
            System.Drawing.Imaging.ImageCodecInfo imageCodecInfo = GetEncoderInfo(System.Drawing.Imaging.ImageFormat.Jpeg);
            System.Drawing.Imaging.Encoder encoder = System.Drawing.Imaging.Encoder.Quality;
            System.Drawing.Imaging.EncoderParameters encoderParameters = new System.Drawing.Imaging.EncoderParameters(1);
            System.Drawing.Imaging.EncoderParameter encoderParameter = new System.Drawing.Imaging.EncoderParameter(encoder, quality);
            encoderParameters.Param[0] = encoderParameter;
            newImage.Save(filePath, imageCodecInfo, encoderParameters);
            image.Dispose();
            newImage.Dispose();
        }
        #endregion
        [Authorize]
        [HttpPost]
        [Route("add-image-server")]
        public app_api.Classes.Result add_image_server(string folder)
        {
            app_api.Classes.Variables variables = new app_api.Classes.Variables();
            app_api.Classes.Result result = new app_api.Classes.Result();
            try
            {
                var httpRequest = System.Web.HttpContext.Current.Request;
                var file = httpRequest.Files[0];
                //if (file.ContentType != "image/gif" & file.ContentType != "image/png" & file.ContentType != "image/jpg" & file.ContentType != "image/jpeg")
                //{
                //    result.success = false;
                //    result.message = "Yalnızca gif,png,jpg veya jpeg dosya türlerinden birini yükleyebilirsiniz!";
                //    return result;
                //}
                var fileFullName = Guid.NewGuid().ToString() + System.IO.Path.GetExtension(file.FileName);
                System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(file.InputStream);
                string path = @"C:\inetpub\vhosts\" + variables.sitedomain + @"\httpdocs\" + folder + @"\" + fileFullName + "";
                ResizeImage(bmp, 500, 500, 100, path);
                result.success = true;
                result.message = fileFullName;
            }
            catch (Exception ex)
            {
                result.success = false;
                result.message = ex.Message;
            }
            return result;
        }
        #region sohbet için bu kişi uygun mu?
        [Authorize]
        [HttpGet]
        [Route("control/member-chat-valid")]
        public int member_chat_valid(int memberId)
        {
            int currentUserRecid = methods.Request_userRecid();
            var user_current = _db.users.First(x => x.recid.Equals(currentUserRecid));
            var user_member = _db.users.First(x => x.recid.Equals(memberId));
            // Ben engellediysem
            if (_db.islemEngel.FirstOrDefault(x => x.engelleyen.Equals(currentUserRecid) & x.engellenen.Equals(memberId)) != null)
                return -1;
            // O engellediyse
            if (_db.islemEngel.FirstOrDefault(x => x.engelleyen.Equals(memberId) & x.engellenen.Equals(currentUserRecid)) != null)
                return -2;
            // Oturum sahibi aktif değilse. Sistemden engellemeler veya diğer platformlardan hesap devre dışı kalmışsa
            if (_db.users.First(x => x.recid.Equals(currentUserRecid)).durumRecid != 1)
                return -3;
            // Aynı cinsiyet mesajlaşmasını engelleme. Bir şekilde bug bulunur kişi ile iletişime geçilirse...
            if (user_current.usersDetail.cinsiyet == user_member.usersDetail.cinsiyet)
                return -3;
            // Mesajlaşma hakkı yok ise
            if (_db.goldElit.FirstOrDefault(x => x.userRecid.Equals(currentUserRecid) & x.bitisTarih > DateTime.Now) == null)
                return -4;
            // Benim yaş aralığıma uygun değilse
            if (!string.IsNullOrEmpty(user_current.usersDetail.ayarMesajYasAraligi))
            {
                int y_1 = int.Parse(user_current.usersDetail.ayarMesajYasAraligi.Substring(0, 2));
                int y_2 = int.Parse(user_current.usersDetail.ayarMesajYasAraligi.Substring(3));

                int k_y = methods.yasGetir(user_member.usersDetail.dogumTarihi.Value);

                if (k_y < y_1 || k_y > y_2)
                    return -5;
            }
            // Ben onun yaş aralığıma uygun değilsem
            if (!string.IsNullOrEmpty(user_member.usersDetail.ayarMesajYasAraligi))
            {
                int y_1 = int.Parse(user_member.usersDetail.ayarMesajYasAraligi.Substring(0, 2));
                int y_2 = int.Parse(user_member.usersDetail.ayarMesajYasAraligi.Substring(3));

                int k_y = methods.yasGetir(user_current.usersDetail.dogumTarihi.Value);

                if (k_y < y_1 || k_y > y_2)
                    return -6;
            }
            // Engel yoksa üyenin durumunu gönder...     
            return user_member.durumRecid.Value;

        }
        #endregion
        [Authorize]
        [HttpPost]
        [Route("control/user-valid")]
        public int control_user_valid()
        {
            int currentUserRecid = methods.Request_userRecid();
            return _db.users.First(x => x.recid.Equals(currentUserRecid)).durumRecid.Value;
        }
        [Authorize]
        [HttpPost]
        [Route("message-delete")]
        public void message_delete(int memberId)
        {
            int currentUserRecid = methods.Request_userRecid();
            foreach (var item in _db.islemMesaj.Where(x => x.gonderen == currentUserRecid & x.alici == memberId || x.alici == currentUserRecid & x.gonderen == memberId).ToList())
            {
                if (item.visible.Equals(memberId))
                {
                    item.sil = true;
                }
                else
                {
                    item.visible = currentUserRecid;
                }
            }
            _db.SaveChanges();
        }
        [HttpGet]
        [Route("application-version-required")]
        public bool application_version_required(string appversion, string appdevice = "")
        {
            int _serverVersion = int.Parse(_db.ApplicationControl.Where(x => x.required & x.devicePlatform == appdevice).OrderByDescending(x => x.recid).Take(1).First().version.Replace(".", ""));
            int _appversion = int.Parse(appversion.Replace(".", ""));
            return _appversion < _serverVersion ? true : false;
        }
        /// <summary>
        /// İlgili version mağaza onayı için bekliyor mu? Eğer bekliyorsa uygulama içinde politika ihlalerini eziyoruz
        /// </summary>
        /// <param name="appversion"></param>
        /// <param name="appdevice"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("application-version-puplished")]
        public bool application_version_puplished(string appversion, string appdevice = "")
        {
            var r = _db.ApplicationControl.FirstOrDefault(x => x.version == appversion.Trim() & x.devicePlatform == appdevice.Trim());
            return r == null ? false : r.published;
        }
        [HttpPost]
        [Route("userDetail")]
        public Models.User.UserDetail usersDetail(int userRecid)
        {
            app_api.Classes.DataCompile dataCompile = new app_api.Classes.DataCompile();
            return dataCompile.DCUserDetail(_db.usersDetail.First(x => x.userRecid.Equals(userRecid)));
        }

        [HttpPost]
        [Route("userAstrology")]
        public Models.Ado.usersAstrology userAstrology(int userRecid)
        {
            var userAstrology = _db.usersAstrology.FirstOrDefault(x => x.userRecid == userRecid);

            if (userAstrology == null)
            {
                return null;

            }

            return userAstrology;
        }


        [HttpPost]
        [Route("userDetail/update")]
        public app_api.Classes.Result userDetail_update(Models.User.UserDetail data)
        {
            app_api.Classes.Result result = new app_api.Classes.Result();
            try
            {
                var userDetail = _db.usersDetail.First(x => x.userRecid.Equals(data.userRecid));
                userDetail.ulkeRecid = data.ulkeRecid;
                userDetail.ilRecid = data.ilRecid;
                userDetail.dogumTarihi = Convert.ToDateTime("01.01." + data.dogumTarihi);
                userDetail.meslek = data.meslek;
                userDetail.profilBaslik = data.profilBaslik;
                userDetail.profilBaslikOnay = 0;
                userDetail.profilYazi = data.profilYazi;
                userDetail.profilYaziOnay = 0;
                if (data.boy != 0)
                {
                    userDetail.boy = byte.Parse(data.boy.ToString());
                    userDetail.kilo = byte.Parse(data.kilo.ToString());
                    userDetail.gozRengi = byte.Parse(data.gozRengi.ToString());
                    userDetail.amac = byte.Parse(data.amac.ToString());
                }
                _db.SaveChanges();
                result.success = true;
            }
            catch (Exception ex)
            {
                result.success = false;
                result.message = ex.Message;

                #region hatayı log olarak sakla...
                Models.Ado.C_Log_Errors c_Log_Errors = new Models.Ado.C_Log_Errors();
                c_Log_Errors.process = "Üye Detay Ekle/Güncelle";
                c_Log_Errors.platform = "Application";
                c_Log_Errors.method = "run/userDetail/update";
                c_Log_Errors.parameters = new JavaScriptSerializer().Serialize(data);
                c_Log_Errors.errorMessage = ex.InnerException.InnerException.Message;
                methods.addErrorLog(c_Log_Errors);
                #endregion
            }
            //catch (DbEntityValidationException dbEx)
            //{
            //    result.success = false;
            //    string Message = "";
            //    foreach (var validationErrors in dbEx.EntityValidationErrors)
            //    {
            //        foreach (var validationError in validationErrors.ValidationErrors)
            //        {
            //            Message = "Error: PropertyName=" + validationError.PropertyName + " - Message=" + validationError.ErrorMessage;
            //            result.message = Message;
            //        }
            //    }
            //}
            return result;

        }

        public class insertValuesChart
        {
            public int userRecid { get; set; }
            public string chartUrl { get; set; }
        }
        public class insertValues
        {
            public DateTime dateofbirth { get; set; }  
            public TimeSpan timeofbirth { get; set; }  
            public string countryGoogle { get; set; }
            public string cityGoogle { get; set; }
            public string latGoogle { get; set; }
            public string lonGoogle { get; set; }
            public string tzoneGoogle { get; set; }
            public int countryRecid { get; set; }
            public int cityRecid { get; set; }
            public int birthYear { get; set; }
            public int job { get; set; }
            public string password { get; set; }
            public string gender { get; set; }
            public string nickname { get; set; }
            public int height { get; set; }
            public int width { get; set; }
            public int eyeColor { get; set; }
            public int aim { get; set; }
            public string session_id { get; set; }
        }

        [HttpPost]
        [Route("user/insertChart")]

        public app_api.Classes.Result user_insertChart(insertValuesChart formData)
        {
            app_api.Classes.Methods methods = new app_api.Classes.Methods();
            app_api.Classes.Variables variables = new app_api.Classes.Variables();
            app_api.Classes.Result result = new app_api.Classes.Result();
            try
            {



                if (!string.IsNullOrEmpty(formData.chartUrl))
                {
                    Models.Ado.usersAstrology userAstrology = new Models.Ado.usersAstrology();
                    userAstrology.userRecid = formData.userRecid;
                    userAstrology.dogumHaritasi = formData.chartUrl;
                    userAstrology.burc = null;
                    userAstrology.career = null;
                    userAstrology.love = null;
                    userAstrology.finance = null;
                    userAstrology.houseType = "placidus";
                    _db.usersAstrology.Add(userAstrology);
                }

                

                _db.SaveChanges();
                result.success = true;
            }
            catch (Exception ex)
            {
                result.success = false;
                result.message = "Exception: " + ex.Message;
                #region hatayı log olarak sakla...
                Models.Ado.C_Log_Errors c_Log_Errors = new Models.Ado.C_Log_Errors();
                c_Log_Errors.process = "Üye Kayıt";
                c_Log_Errors.platform = "Application";
                c_Log_Errors.method = "run/user/insert";
                c_Log_Errors.parameters = new JavaScriptSerializer().Serialize(formData);
                c_Log_Errors.errorMessage = ex.InnerException.InnerException.Message;

                methods.addErrorLog(c_Log_Errors);
                #endregion
            }
            return result;
        }

        [HttpPost]
        [Route("user/insert")]
        public app_api.Classes.Result user_insert(insertValues formData)
        {
            app_api.Classes.Methods methods = new app_api.Classes.Methods();
            app_api.Classes.Variables variables = new app_api.Classes.Variables();
            app_api.Classes.Result result = new app_api.Classes.Result();
            try
            {
                if (methods.nicknameBarrier(formData.nickname))
                {
                    result.success = false;
                    result.message = "Rumuzda uygun olmayan yazı karakterleri bulunmaktadır.\nYalnızca harf (a-z) rakam (0-9) \nve (. - _ ) karakterleri kullanılabilir.\nBoşluk, e-posta veya telefon numarası gibi iletişim bilgileri kullanılamaz.";
                    return result;
                }
                var query = _db.users.FirstOrDefault(y => y.rumuz.ToLower().Equals(formData.nickname.ToLower()));
                if (query != null)
                {
                    result.success = false;
                    result.message = "Bu rumuz kullanılmaktadır. Lütfen farklı bir rumuz yazın";
                    return result;
                }
                Models.Ado.users us = new Models.Ado.users();
                Models.Ado.usersDetail usDet = new Models.Ado.usersDetail();
                us.rumuz = formData.nickname;
                us.sifre = methods.MD5_Create(formData.password);
                us.durumRecid = 1;
                us.uyelikTarihi = DateTime.Now;
                us.cepTelefonuOnay = false;
                us.emailOnay = false;
                us.dil = "tr";
                us.kulTip = 2;

                usDet.aktifZaman = DateTime.Now;
                usDet.sonGirisTarihi = DateTime.Now;

                #region Version2 : Yeni itemlar eklendi! İletişim bilgisi artık zorunlu. İletişim bilgisi onaylanmadan kayıt etmeye izin verme.
                //if (formData.session_id != null)
                //{
                //    var query_contact = _db.tmp_UserContactReqired.FirstOrDefault(xreq => xreq.session_id.Equals(formData.session_id) & (xreq.phoneActive.Value || xreq.emailActive.Value));
                //    if (query_contact == null)
                //    {
                //        result.success = false;
                //        result.message = "E-posta veya Cep telefonu onaylı olmak zorunda.";
                //        return result;
                //    }
                //    us.cepOnayActivCode = query_contact.phoneActivationCode;
                //    us.cepTelefonu = query_contact.phone;
                //    us.cepTelefonuOnay = query_contact.phoneActive ?? false;
                //    us.email = query_contact.email;
                //    us.emailOnay = query_contact.emailActive ?? false;
                //    us.emailOnayActivCode = query_contact.emailActivationCode;

                //    usDet.boy = byte.Parse(formData.height.ToString());
                //    usDet.kilo = byte.Parse(formData.width.ToString());
                //    usDet.gozRengi = byte.Parse(formData.eyeColor.ToString());
                //    usDet.amac = byte.Parse(formData.aim.ToString());

                //    if (us.cepTelefonuOnay)
                //    {
                //        var lastQ = _db.users.Where(x_last_phone => x_last_phone.cepTelefonu.Equals(us.cepTelefonu));
                //        foreach (var item in lastQ)
                //        {
                //            item.cepTelefonu = null;
                //            item.cepOnayActivCode = null;
                //            item.cepTelefonuOnay = false;
                //        }
                //        if (lastQ.Count() > 0)
                //            _db.SaveChanges();
                //    }

                //    if (us.emailOnay)
                //    {
                //        var lastQ = _db.users.Where(x_last_email => x_last_email.email.Equals(us.email));
                //        foreach (var item in lastQ)
                //        {
                //            item.email = null;
                //            item.emailOnayActivCode = null;
                //            item.emailOnay = false;
                //        }
                //        if (lastQ.Count() > 0)
                //            _db.SaveChanges();
                //    }
                //}
                #endregion

                _db.users.Add(us);
                _db.SaveChanges();

                byte[] userDes = methods.tripLeDes_Write(us.recid.ToString(), "");
                usDet.userRecid = us.recid;
                usDet.userIDDes = Convert.ToBase64String(userDes);
                usDet.ilRecid = formData.cityRecid;
                usDet.ulkeRecid = formData.countryRecid;
                usDet.cinsiyet = formData.gender;
                usDet.detayDurum = true;
                usDet.ayarMesajGonderim = 0;
                usDet.ayarResimGosterim = 0;
                usDet.aktif = false;
                //usDet.dogumTarihi = Convert.ToDateTime("01.01." + formData.birthYear);
                usDet.meslek = formData.job;

                //Yeni eklendi
                usDet.ulke = formData.countryGoogle;
                usDet.sehir = formData.cityGoogle;
                usDet.googleLat = formData.latGoogle;
                usDet.googleLon = formData.lonGoogle;
                usDet.googleTzone = formData.tzoneGoogle;

                usDet.dogumTarihi = formData.dateofbirth;
                usDet.dogumSaati = formData.timeofbirth;


                if (formData.gender == "E")
                {
                    usDet.profileImages = "male.png";
                    usDet.profileImagesID = variables.defaultProfileImagesRecid_male;
                }
                else
                {
                    usDet.profileImages = "famale.png";
                    usDet.profileImagesID = variables.defaultProfileImagesRecid_famale;
                }
                _db.usersDetail.Add(usDet);


                for (int i = 0; i < 5; i++)
                {
                    Models.Ado.islemResimler addImages = new Models.Ado.islemResimler();
                    addImages.gosterTxt = "Göster";
                    addImages.onay = 0;
                    addImages.goster = true;
                    addImages.kullanici = us.recid;
                    addImages.yuklenmeTarihi = DateTime.Now;
                    if (usDet.cinsiyet == "E")
                    {
                        addImages.resim = "male.png";
                    }
                    else
                    {
                        addImages.resim = "famale_2.png";
                    }
                    _db.islemResimler.Add(addImages);
                }

                _db.SaveChanges();
                result.success = true;
                result.code = us.recid;
            }
            catch (Exception ex)
            {
                result.success = false;
                result.message = "Exception: " + ex.Message;
                #region hatayı log olarak sakla...
                Models.Ado.C_Log_Errors c_Log_Errors = new Models.Ado.C_Log_Errors();
                c_Log_Errors.process = "Üye Kayıt";
                c_Log_Errors.platform = "Application";
                c_Log_Errors.method = "run/user/insert";
                c_Log_Errors.parameters = new JavaScriptSerializer().Serialize(formData);
                c_Log_Errors.errorMessage = ex.InnerException.InnerException.Message;

                methods.addErrorLog(c_Log_Errors);
                #endregion
            }
            return result;
        }
        public class insertValues_V2
        {
            public string countryGoogle { get; set; }
            public string cityGoogle { get; set; }
            public string latGoogle { get; set; }
            public string lonGoogle { get; set; }
            public string tzoneGoogle { get; set; }
            public int countryRecid { get; set; }
            public int cityRecid { get; set; }
            public int birthYear { get; set; }
            public int job { get; set; }
            public string password { get; set; }
            public string gender { get; set; }
            public string nickname { get; set; }
            public int height { get; set; }
            public int width { get; set; }
            public int eyeColor { get; set; }
            public int aim { get; set; }
        }
        [HttpPost]
        [Route("user/insert-v2")]
        public app_api.Classes.Result user_insert(insertValues_V2 formData)
        {
            app_api.Classes.Methods methods = new app_api.Classes.Methods();
            app_api.Classes.Variables variables = new app_api.Classes.Variables();
            app_api.Classes.Result result = new app_api.Classes.Result();
            try
            {
                if (methods.nicknameBarrier(formData.nickname))
                {
                    result.success = false;
                    result.message = "Rumuzda uygun olmayan yazı karakterleri bulunmaktadır.\nYalnızca harf (a-z) rakam (0-9) \nve (. - _ ) karakterleri kullanılabilir.\nBoşluk, e-posta veya telefon numarası gibi iletişim bilgileri kullanılamaz.";
                    return result;
                }
                var query = _db.users.FirstOrDefault(y => y.rumuz.ToLower().Equals(formData.nickname.ToLower()));
                if (query != null)
                {
                    result.success = false;
                    result.message = "Bu rumuz kullanılmaktadır. Lütfen farklı bir rumuz yazın";
                    return result;
                }
                Models.Ado.users us = new Models.Ado.users();
                Models.Ado.usersDetail usDet = new Models.Ado.usersDetail();
                us.rumuz = formData.nickname;
                us.sifre = methods.MD5_Create(formData.password);
                us.durumRecid = 1;
                us.uyelikTarihi = DateTime.Now;
                us.cepTelefonuOnay = false;
                us.emailOnay = false;
                us.dil = "tr";
                us.kulTip = 2;

                usDet.aktifZaman = DateTime.Now;
                usDet.sonGirisTarihi = DateTime.Now;
                usDet.boy = byte.Parse(formData.height.ToString());
                usDet.kilo = byte.Parse(formData.width.ToString());
                usDet.gozRengi = byte.Parse(formData.eyeColor.ToString());
                usDet.amac = byte.Parse(formData.aim.ToString());

                _db.users.Add(us);
                _db.SaveChanges();

                byte[] userDes = methods.tripLeDes_Write(us.recid.ToString(), "");
                usDet.userRecid = us.recid;
                usDet.userIDDes = Convert.ToBase64String(userDes);
                usDet.ilRecid = formData.cityRecid;
                usDet.ulkeRecid = formData.countryRecid;
                usDet.cinsiyet = formData.gender;
                usDet.detayDurum = true;
                usDet.ayarMesajGonderim = 0;
                usDet.ayarResimGosterim = 0;
                usDet.aktif = false;
                usDet.dogumTarihi = Convert.ToDateTime("01.01." + formData.birthYear);
                usDet.meslek = formData.job;

                if (formData.gender == "E")
                {
                    usDet.profileImages = "male.png";
                    usDet.profileImagesID = variables.defaultProfileImagesRecid_male;
                }
                else
                {
                    usDet.profileImages = "famale.png";
                    usDet.profileImagesID = variables.defaultProfileImagesRecid_famale;
                }
                _db.usersDetail.Add(usDet);

                for (int i = 0; i < 5; i++)
                {
                    Models.Ado.islemResimler addImages = new Models.Ado.islemResimler();
                    addImages.gosterTxt = "Göster";
                    addImages.onay = 0;
                    addImages.goster = true;
                    addImages.kullanici = us.recid;
                    addImages.yuklenmeTarihi = DateTime.Now;
                    if (usDet.cinsiyet == "E")
                    {
                        addImages.resim = "male.png";
                    }
                    else
                    {
                        addImages.resim = "famale_2.png";
                    }
                    _db.islemResimler.Add(addImages);
                }

                _db.SaveChanges();
                result.success = true;
                result.code = us.recid;
            }
            catch (Exception ex)
            {
                result.success = false;
                result.message = "Exception: " + ex.Message;
                #region hatayı log olarak sakla...
                Models.Ado.C_Log_Errors c_Log_Errors = new Models.Ado.C_Log_Errors();
                c_Log_Errors.process = "Üye Kayıt";
                c_Log_Errors.platform = "Application";
                c_Log_Errors.method = "run/user/insert";
                c_Log_Errors.parameters = new JavaScriptSerializer().Serialize(formData);
                c_Log_Errors.errorMessage = ex.InnerException.InnerException.Message;
                methods.addErrorLog(c_Log_Errors);
                #endregion
            }
            return result;
        }
        [HttpGet]
        [Route("user/control/nickname")]
        public bool control_nickname(string nickname)
        {
            return _db.users.FirstOrDefault(y => y.rumuz.ToLower().Equals(nickname.ToLower())) == null ? true : false;
        }
        [HttpPost]
        [Route("user/forgot-password")]
        public app_api.Classes.Result forgot_password(string keyname)
        {
            app_api.Classes.Variables variables = new app_api.Classes.Variables();
            app_api.Classes.Methods methods = new app_api.Classes.Methods();
            app_api.Classes.Result result = new app_api.Classes.Result();
            try
            {
                bool isPhone = false;
                bool isMail = false;

                if (methods.isNumeric(keyname))
                {
                    keyname = methods.phone_finally(keyname);
                    isPhone = true;
                }
                else
                    isMail = true;

                Random rd = new Random();
                string newPassword = rd.Next(100000, 999999).ToString();
                var query = new Models.Ado.users();

                if (isPhone)
                    query = _db.users.FirstOrDefault(x => x.cepTelefonuOnay & x.cepTelefonu.Equals(keyname));
                else
                    query = _db.users.FirstOrDefault(x => x.emailOnay & x.email.Equals(keyname));

                if (query == null)
                {
                    result.success = false;
                    result.message = "Onaylı Kullanıcı Bilgisi Bulunamadı!";
                    return result;
                }
                else
                {
                    string newPasswordMD5 = methods.MD5_Create(newPassword);
                    query.sifre = newPasswordMD5;
                    _db.SaveChanges();
                    if (isPhone)
                    {
                        string msg = variables.sitedomain + "'dan Mesajınız Var; Kullanıcı Adınız : " + query.rumuz + " Yeni Şifreniz : " + newPassword + "";
                        methods.SendSMS(keyname, msg);
                        result.message = "Telefon numaranıza SMS ile yeni şifreniz gönderilmiştir.";
                    }
                    else
                    {
                        string mailBody = System.IO.File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "Models/MailTemplates/_MailTemplateResetMailAdress.html");
                        string bodyTeamplate = String.Format(mailBody, newPassword, keyname.Trim(), query.rumuz);

                        methods.SendEMAIL(keyname.Trim(), "Şifre Sıfırlama Talebi", bodyTeamplate);
                        result.message = "E-posta adresinize yeni şifreniz gönderilmiştir. E-posta gelmez ise, gereksizler veya spam kutusunu kontrol edin.";
                    }
                }
                result.success = true;
            }
            catch (Exception ex)
            {
                result.success = false;
                result.message = ex.Message;
                #region hatayı log olarak sakla...
                Models.Ado.C_Log_Errors c_Log_Errors = new Models.Ado.C_Log_Errors();
                c_Log_Errors.process = "Şifremi Unuttum";
                c_Log_Errors.platform = "Application";
                c_Log_Errors.method = "run/user/forgot-password";
                c_Log_Errors.parameters = "keyName:" + keyname;
                c_Log_Errors.errorMessage = ex.Message;
                methods.addErrorLog(c_Log_Errors);
                #endregion
            }
            return result;
        }
        [Authorize]
        [HttpPost]
        [Route("member/push-notification")]
        public void push_notification(int userRecid, string title, string message, int notification_type)
        {
            methods.push_notification_mobile_app(userRecid, title, message, notification_type);
        }
        [Authorize]
        [HttpPost]
        [Route("clearlist/my-favorite")]
        public void clearlist_my_favorite()
        {
            int currentUserRecid = methods.Request_userRecid();
            var query = _db.islemFavori.Where(x => x.ekleyen.Equals(currentUserRecid) & x.sil.Value != true).ToList();
            foreach (var item in query)
            {
                if (item.gizle.Equals(0))
                {
                    item.gizle = currentUserRecid;
                }
                else
                {
                    item.sil = true;
                }
            }
            _db.SaveChanges();
        }
        [Authorize]
        [HttpPost]
        [Route("clearlist/me-favorite")]
        public void clearlist_me_favorite()
        {
            int currentUserRecid = methods.Request_userRecid();
            var query = _db.islemFavori.Where(x => x.eklenen.Equals(currentUserRecid) & x.sil.Value != true).ToList();
            foreach (var item in query)
            {
                if (item.gizle.Equals(0))
                {
                    item.gizle = currentUserRecid;
                }
                else
                {
                    item.sil = true;
                }
            }
            _db.SaveChanges();
        }
        [Authorize]
        [HttpPost]
        [Route("clearlist/my-visit")]
        public void clearlist_my_visit()
        {
            int currentUserRecid = methods.Request_userRecid();
            var query = _db.islemGezen.Where(x => x.gezen.Equals(currentUserRecid) & x.sil.Value != true).ToList();
            foreach (var item in query)
            {
                if (item.gizle.Equals(0))
                {
                    item.gizle = currentUserRecid;
                }
                else
                {
                    item.sil = true;
                }
            }
            _db.SaveChanges();
        }
        [Authorize]
        [HttpPost]
        [Route("clearlist/me-visit")]
        public void clearlist_me_visit()
        {
            int currentUserRecid = methods.Request_userRecid();
            var query = _db.islemGezen.Where(x => x.gezilen.Equals(currentUserRecid) & x.sil.Value != true).ToList();
            foreach (var item in query)
            {
                if (item.gizle.Equals(0))
                {
                    item.gizle = currentUserRecid;
                }
                else
                {
                    item.sil = true;
                }
            }
            _db.SaveChanges();
        }
        [Authorize]
        [HttpPost]
        [Route("clearlist/my-like")]
        public void clearlist_my_like()
        {
            int currentUserRecid = methods.Request_userRecid();
            var query = _db.islemBegen.Where(x => x.begenen.Equals(currentUserRecid) & x.sil.Value != true).ToList();
            foreach (var item in query)
            {
                if (item.gizle.Equals(0))
                {
                    item.gizle = currentUserRecid;
                }
                else
                {
                    item.sil = true;
                }
            }
            _db.SaveChanges();
        }
        [Authorize]
        [HttpPost]
        [Route("clearlist/me-like")]
        public void clearlist_me_like()
        {
            int currentUserRecid = methods.Request_userRecid();
            var query = _db.islemBegen.Where(x => x.begenilen.Equals(currentUserRecid) & x.sil.Value != true).ToList();
            foreach (var item in query)
            {
                if (item.gizle.Equals(0))
                {
                    item.gizle = currentUserRecid;
                }
                else
                {
                    item.sil = true;
                }
            }
            _db.SaveChanges();
        }
        [Authorize]
        [HttpPost]
        [Route("clearlist/my-score")]
        public void clearlist_my_score()
        {
            int currentUserRecid = methods.Request_userRecid();
            var query = _db.islemPuan.Where(x => x.puanVeren.Equals(currentUserRecid) & x.sil.Value != true).ToList();
            foreach (var item in query)
            {
                if (item.gizle.Equals(0))
                {
                    item.gizle = currentUserRecid;
                }
                else
                {
                    item.sil = true;
                }
            }
            _db.SaveChanges();
        }
        [Authorize]
        [HttpPost]
        [Route("clearlist/me-score")]
        public void clearlist_me_score()
        {
            int currentUserRecid = methods.Request_userRecid();
            var query = _db.islemPuan.Where(x => x.puanAlan.Equals(currentUserRecid) & x.sil.Value != true).ToList();
            foreach (var item in query)
            {
                if (item.gizle.Equals(0))
                {
                    item.gizle = currentUserRecid;
                }
                else
                {
                    item.sil = true;
                }
            }
            _db.SaveChanges();
        }
        [Authorize]
        [HttpPost]
        [Route("clearlist/my-barrier")]
        public void clearlist_my_barrier()
        {
            int currentUserRecid = methods.Request_userRecid();
            var query = _db.islemEngel.Where(x => x.engelleyen.Equals(currentUserRecid) & x.sil.Value != true).ToList();
            foreach (var item in query)
            {
                if (item.gizle.Equals(0))
                {
                    item.gizle = currentUserRecid;
                }
                else
                {
                    item.sil = true;
                }
            }
            _db.SaveChanges();
        }
        [Authorize]
        [HttpPost]
        [Route("user/image-redirect")]
        public bool imageRedirect()
        {
            int currentUserRecid = methods.Request_userRecid();
            var query = _db.islemResimler.FirstOrDefault(x => x.kullanici.Equals(currentUserRecid) & !x.resim.Contains("male") & x.onay != 2);
            if (query == null)
                return true;
            else
                return false;
        }
        private app_api.Classes.Result registerRequiredControl_Email_Approve(string frmValue, string code, string session_id)
        {
            app_api.Classes.Result result = new app_api.Classes.Result();
            var q = _db.tmp_UserContactReqired.FirstOrDefault(x => x.session_id.Equals(session_id) & x.email.Equals(frmValue) & x.emailActivationCode.Equals(code));
            if (q != null)
            {
                q.emailActive = true;
                _db.SaveChanges();
                result.success = true;
                result.message = "Tebrikler! E-posta adresiniz onaylandı. Üyelik işleminize devam edebilirsiniz.";
                return result;
            }
            else
            {
                result.success = false;
                result.message = "Onay kodunuz hatalı! Lütfen kontrol ederek tekrar deneyin.";
                return result;
            }
        }
        private app_api.Classes.Result registerRequiredControl_Email(string frmValue, string session_id)
        {
            app_api.Classes.Result result = new app_api.Classes.Result();
            try
            {
                if (!methods.isEmail(frmValue))
                {
                    result.success = false;
                    result.message = "Lütfen geçerli bir E-posta adresi girin.";
                    return result;
                }

                app_api.Classes.Variables variables = new app_api.Classes.Variables();
                Random rd = new Random();
                string akey = rd.Next(100000, 999999).ToString();
                string mailBody = System.IO.File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "Models/MailTemplates/_MailTemplateConfirmMailAdress.html");
                string bodyTeamplate = String.Format(mailBody, akey, frmValue.Trim());
                var sendMail = methods.SendEMAIL(frmValue.Trim(), "E-posta Aktivasyon Kodu", bodyTeamplate);

                foreach (var item in _db.tmp_UserContactReqired.Where(x => x.session_id.Equals(session_id) & !string.IsNullOrEmpty(x.email)).ToList())
                {
                    _db.tmp_UserContactReqired.Remove(item);
                }
                _db.SaveChanges();

                Models.Ado.tmp_UserContactReqired table = new Models.Ado.tmp_UserContactReqired();
                table.createdDate = DateTime.Now;
                table.email = frmValue;
                table.emailActivationCode = akey;
                table.session_id = session_id;
                _db.tmp_UserContactReqired.Add(table);
                _db.SaveChanges();

                result.success = true;
                result.value = table.recid;
                result.returnText = frmValue;
                result.message = "Lütfen E-posta kutunuzu kontrol edin. 6 hanelik aktivasyon kodu gönderildi.\nE-posta görünmüyor ise, spam kutunuzu kontrol edin.";
                return result;
            }
            catch (Exception ex)
            {
                result.success = false;
                result.message = "E-posta gönderilirken bir sorun oluştu. Daha sonra tekrar deneyin. Detay : " + ex.Message;
                return result;
            }
        }
        private app_api.Classes.Result registerRequiredControl_Phone_Approve(string frmValue, string code, string session_id)
        {
            app_api.Classes.Result result = new app_api.Classes.Result();
            var q = _db.tmp_UserContactReqired.FirstOrDefault(x => x.session_id.Equals(session_id) & x.phone.Equals(frmValue) & x.phoneActivationCode.Equals(code));
            if (q != null)
            {
                q.phoneActive = true;
                _db.SaveChanges();
                result.success = true;
                result.message = "Tebrikler! Telefon numaranız onaylandı. Üyelik işleminize devam edebilirsiniz.";
                return result;
            }
            else
            {
                result.success = false;
                result.message = "Onay kodunuz hatalı! Lütfen kontrol ederek tekrar deneyin.";
                return result;
            }
        }
        private app_api.Classes.Result registerRequiredControl_Phone(string frmValue, string session_id)
        {
            app_api.Classes.Result result = new app_api.Classes.Result();
            try
            {
                if (!methods.isNumeric(frmValue))
                {
                    result.success = false;
                    result.message = "Lütfen geçerli bir telefon numarası girin.";
                    return result;
                }
                string phoneNumber = methods.phone_finally(frmValue);
                if (phoneNumber.Length != 11)
                {
                    result.success = false;
                    result.message = "Lütfen geçerli bir telefon numarası girin.";
                    return result;
                }

                Random rd = new Random();
                string akey = rd.Next(100000, 999999).ToString();
                string msg = akey + " Kodu ile astromend.com'da üyeliğinizi tamamlayabilirsiniz.";
                var sendSms = methods.SendSMS(phoneNumber, msg);

                foreach (var item in _db.tmp_UserContactReqired.Where(x => x.session_id.Equals(session_id) & !string.IsNullOrEmpty(x.phone)).ToList())
                {
                    _db.tmp_UserContactReqired.Remove(item);
                }
                _db.SaveChanges();

                Models.Ado.tmp_UserContactReqired table = new Models.Ado.tmp_UserContactReqired();
                table.createdDate = DateTime.Now;
                table.phone = phoneNumber;
                table.phoneActivationCode = akey;
                table.session_id = session_id;
                _db.tmp_UserContactReqired.Add(table);
                _db.SaveChanges();

                result.success = true;
                result.value = table.recid;
                result.returnText = phoneNumber;
                result.message = "Lütfen cep telefonunuzu kontrol edin. 6 hanelik aktivasyon kodu gönderildi.";
                return result;
            }
            catch (Exception ex)
            {
                result.success = false;
                result.message = "SMS gönderilirken bir sorun oluştu. Daha sonra tekrar deneyin. Detay : " + ex.Message;
                return result;
            }
        }
        [HttpPost]
        [Route("register/rrc-send-activation-code")]
        public app_api.Classes.Result registerRequiredControl_Send_Activation_Code(string frmValue, string session_id)
        {
            app_api.Classes.Result result = new app_api.Classes.Result();
            bool isPhone = methods.isNumeric(frmValue) ? true : false;
            if (isPhone)
                result = registerRequiredControl_Phone(frmValue, session_id);
            else
                result = registerRequiredControl_Email(frmValue, session_id);
            return result;
        }
        [HttpPost]
        [Route("register/rrc-approve")]
        public app_api.Classes.Result registerRequiredControl_Approve(string frmValue, string code, string session_id)
        {
            app_api.Classes.Result result = new app_api.Classes.Result();
            bool isPhone = methods.isNumeric(frmValue) ? true : false;
            if (isPhone)
                result = registerRequiredControl_Phone_Approve(frmValue, code, session_id);
            else
                result = registerRequiredControl_Email_Approve(frmValue, code, session_id);
            return result;
        }
        [HttpPost]
        [Route("register/rrc-check")]
        public app_api.Classes.Result registerRequiredControl_Check(string session_id)
        {
            app_api.Classes.Result result = new app_api.Classes.Result();
            var query = _db.tmp_UserContactReqired.FirstOrDefault(x => x.session_id.Equals(session_id) & (x.phoneActive.Value || x.emailActive.Value));
            if (query == null)
            {
                result.success = false;
                result.message = "E-posta veya Cep telefonu onaylı olmak zorunda.";
                return result;
            }
            else
            {
                result.success = true;
                result.message = "Onay Başarılı!";
            }
            return result;
        }
        [HttpGet]
        [Route("create-server-session-id")]
        public string create_server_session_id()
        {
            Random random = new Random();
            string session_id = DateTime.UtcNow.ToString("ddMMyyyyHHmmssfff") + random.Next(0, 100000);
            return session_id;
        }       
    }
}