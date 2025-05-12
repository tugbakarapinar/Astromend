using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using PagedList;

namespace Controllers
{
    [Authorize]
    [RoutePrefix("app/api/member")]
    public class MemberController : ApiController
    {
        Models.Ado.Entities _db = new Models.Ado.Entities();
        app_api.Classes.Methods methods = new app_api.Classes.Methods();
        app_api.Classes.Variables variables = new app_api.Classes.Variables();
        private string idConvertText_prayer(int id, string lang)
        {
            if (lang == "en")
            {
                switch (id)
                {
                    case 1:
                        return "Yes";
                    case 2:
                        return "No";
                    case 3:
                        return "Sometimes";
                    case 4:
                        return "Friday prayers only";
                    default:
                        return "I don't mind";
                }
            }
            else if (lang == "id")
            {
                switch (id)
                {
                    case 1:
                        return "Ya";
                    case 2:
                        return "Tidak";
                    case 3:
                        return "Terkadang";
                    case 4:
                        return "Sholat jumat saja";
                    default:
                        return "Tidak masalah";
                }
            }
            else if (lang == "ar")
            {
                switch (id)
                {
                    case 1:
                        return "أجل";
                    case 2:
                        return "لا";
                    case 3:
                        return "بعض الأحيان";
                    case 4:
                        return "صلاة الجمعة";
                    default:
                        return "ليس مهما";
                }
            }
            else if (lang == "fa")
            {
                switch (id)
                {
                    case 1:
                        return "آره";
                    case 2:
                        return "نه";
                    case 3:
                        return "گاهی";
                    case 4:
                        return "نماز جمعه بخوان";
                    default:
                        return "مهم نیست";
                }
            }
            else
            {
                switch (id)
                {
                    case 1:
                        return "Düzenli Kılsın";
                    case 2:
                        return "Kılmasın";
                    case 3:
                        return "Arasıra Kılsın";
                    case 4:
                        return "Cuma Namazlarını Kılsın";
                    default:
                        return "Farketmez";
                }
            }
        }
        private string idConvertText_fast(int id, string lang)
        {
            if (lang == "en")
            {
                switch (id)
                {
                    case 1:
                        return "Yes";
                    case 2:
                        return "Sometimes";
                    case 3:
                        return "Hayıt";
                    default:
                        return "I don't mind";
                }
            }
            else if (lang == "id")
            {
                switch (id)
                {
                    case 1:
                        return "Ya";
                    case 2:
                        return "Tidak";
                    case 3:
                        return "Terkadang";
                    default:
                        return "Tidak masalah";
                }
            }
            else if (lang == "ar")
            {
                switch (id)
                {
                    case 1:
                        return "أجل";
                    case 2:
                        return "لا";
                    case 3:
                        return "بعض الأحيان";
                    default:
                        return "لا توجد مشكلة";
                }
            }
            else if (lang == "fa")
            {
                switch (id)
                {
                    case 1:
                        return "آره";
                    case 2:
                        return "نه";
                    case 3:
                        return "گاهی";
                    default:
                        return "مهم نیست";
                }
            }
            else
            {
                switch (id)
                {
                    case 1:
                        return "Oruçlarını Tutsun";
                    case 2:
                        return "Arasıra Tutsun";
                    case 3:
                        return "Tutmasın";
                    default:
                        return "Farketmez";
                }
            }
        }
        // Kullanıcı obje olarak götüntülenmediği durumlarda, id ile çağrılması için
        [HttpGet]
        [Route("member")]
        public Models.User.Member member(int memberId)
        {
            var query = _db.usersDetail.FirstOrDefault(x => x.userRecid.Equals(memberId));
            // Üyelerin veritabanından kazanmak için belli koşullara göre siliyoruz. Bu yüzden bu durum karşılaşılabilir.
            if (query == null)
                return null;
            Models.User.Member member = new Models.User.Member()
            {
                meslek = query.soruMeslek.tanim,
                profileImages = query.islemResimler.resim,
                recid = query.userRecid,
                rumuz = query.users.rumuz,
                sehir = query.city.ilAdi,
                yas = methods.yasGetir(query.dogumTarihi.Value),
                boy = query.boy ?? 0,
                kilo = query.kilo ?? 0,
                gozRengi = query.gozRengi ?? 0,
                amac = query.amac ?? 0
            };
            member.virtualOnline = methods.onlineResult(query.aktifZaman.Value, query.aktif);
            member.virtualOnTime = methods.aktifZaman(query.aktifZaman.Value, query.aktif, member.virtualOnline);
            member.aktif = query.aktif;
            List<string> list = new List<string>();
            foreach (var item_image in _db.islemResimler.Where(x => x.kullanici.Equals(memberId) & x.onay.Equals(1) & !x.resim.Contains("male")))
                list.Add(item_image.resim);
            member.photos = list.ToArray();
            return member;
        }
        public class active_status_fields
        {
            public byte virtualOnline { get; set; }
            public string virtualOnTime { get; set; }
            public bool aktif { get; set; }
        }
        [HttpGet]
        [Route("member/active-status")]
        public active_status_fields member_active_status(int memberId)
        {
            var query = _db.usersDetail.First(x => x.userRecid.Equals(memberId));
            active_status_fields result = new active_status_fields();
            result.virtualOnline = methods.onlineResult(query.aktifZaman.Value, query.aktif);
            result.virtualOnTime = methods.aktifZaman(query.aktifZaman.Value, query.aktif, result.virtualOnline);
            result.aktif = query.aktif;
            return result;
        }
        #region Listeler
        [HttpGet]
        [Route("list/online")]
        public List<Models.User.Member> list_online(int page = 1)
        {
            int currentUserRecid = methods.Request_userRecid();
            var currentUser = _db.users.First(x => x.recid.Equals(currentUserRecid));

            List<Models.User.Member> members = new List<Models.User.Member>();

            var query = _db.usersDetail.Where(x => x.users.recid != variables.guestId & x.users.durumRecid.Value == 1 & x.detayDurum == true & x.cinsiyet != currentUser.usersDetail.cinsiyet & (x.aktifZaman >= DateTime.Now || x.aktif == true))
                              .Where(e => !_db.islemEngel.Where(l => l.engelleyen == currentUserRecid & l.engellenen == e.userRecid).Any())
                              .Where(e => !_db.islemEngel.Where(l => l.engelleyen == e.userRecid & l.engellenen == currentUserRecid).Any())
                              .OrderBy(x => (x.aktifZaman >= DateTime.Now ? (!x.islemResimler.resim.Contains("male") ? 1 : 2) : !x.islemResimler.resim.Contains("male") ? 3 : 4))
                              .ThenByDescending(x => x.vitrinZaman)
                              .Take(page == 1 ? 5 : 200);



            foreach (var item in query)
            {
                Models.User.Member member = new Models.User.Member()
                {
                    meslek = item.soruMeslek.tanim,
                    profileImages = item.islemResimler.resim,
                    recid = item.userRecid,
                    rumuz = item.users.rumuz,
                    sehir = item.city.ilAdi,
                    yas = methods.yasGetir(item.dogumTarihi.Value),
                    virtualOnline = Convert.ToByte(item.aktifZaman.Value >= DateTime.Now ? 1 : 0),
                    aktif = item.aktif,
                    boy = item.boy ?? 0,
                    kilo = item.kilo ?? 0,
                    gozRengi = item.gozRengi ?? 0,
                    amac = item.amac ?? 0
                };
                List<string> list = new List<string>();
                foreach (var item_image in _db.islemResimler.Where(x => x.kullanici.Equals(item.userRecid) & x.onay.Equals(1) & !x.resim.Contains("male")))
                    list.Add(item_image.resim);
                member.photos = list.ToArray();
                members.Add(member);
            }
            return members;
        }
        [HttpGet]
        [Route("list/home")]
        public List<Models.User.Member> list_home(int page = 1)
        {
            int currentUserRecid = methods.Request_userRecid();
            var currentUser = _db.users.First(x => x.recid.Equals(currentUserRecid));

            List<Models.User.Member> members = new List<Models.User.Member>();

            var query = _db.usersDetail.Where(x => x.users.recid != variables.guestId & x.users.durumRecid.Value == 1 & x.detayDurum == true & x.cinsiyet != currentUser.usersDetail.cinsiyet & (x.aktifZaman >= DateTime.Now || x.aktif == true))
                  .Where(e => !_db.islemEngel.Where(l => l.engelleyen == currentUserRecid & l.engellenen == e.userRecid).Any())
                  .Where(e => !_db.islemEngel.Where(l => l.engelleyen == e.userRecid & l.engellenen == currentUserRecid).Any())
                  .OrderBy(x => (x.aktifZaman >= DateTime.Now ? (!x.islemResimler.resim.Contains("male") ? 1 : 2) : !x.islemResimler.resim.Contains("male") ? 3 : 4))
                  .ThenByDescending(x => x.vitrinZaman)
                  .Take(page == 1 ? 5 : 210);


            foreach (var item in query)
            {
                Models.User.Member member = new Models.User.Member();
                member.recid = item.userRecid;
                member.rumuz = item.users.rumuz;
                member.sehir = item.city.ilAdi;
                member.yas = methods.yasGetir(item.dogumTarihi.Value);
                member.virtualOnline = Convert.ToByte(item.aktifZaman.Value >= DateTime.Now ? 1 : 0);
                member.virtualOnTime = methods.aktifZaman(item.aktifZaman.Value, item.aktif, member.virtualOnline);
                member.meslek = item.soruMeslek.tanim;
                member.profileImages = item.islemResimler.resim;
                member.boy = item.boy ?? 0;
                member.kilo = item.kilo ?? 0;
                member.gozRengi = item.gozRengi ?? 0;
                member.amac = item.amac ?? 0;
                member.aktif = item.aktif;
                List<string> list = new List<string>();
                foreach (var item_image in _db.islemResimler.Where(x => x.kullanici.Equals(item.userRecid) & x.onay.Equals(1) & !x.resim.Contains("male")))
                    list.Add(item_image.resim);
                member.photos = list.ToArray();
                members.Add(member);
            }
            return members;
        }

        [HttpGet]
        [Route("list/all")]
        public List<Models.User.Member> list_all(int page = 1)
        {
            int currentUserRecid = methods.Request_userRecid();
            var currentUser = _db.users.First(x => x.recid.Equals(currentUserRecid));

            List<Models.User.Member> members = new List<Models.User.Member>();
            
            var query = _db.usersDetail
                            .Where(x => x.users.recid != variables.guestId && x.users.durumRecid.Value == 1 && x.detayDurum == true && x.cinsiyet != currentUser.usersDetail.cinsiyet)
                            .Where(e => !_db.islemEngel.Where(l => l.engelleyen == currentUserRecid && l.engellenen == e.userRecid).Any())  
                            .Where(e => !_db.islemEngel.Where(l => l.engelleyen == e.userRecid && l.engellenen == currentUserRecid).Any())  
                            .OrderByDescending(x => x.users.uyelikTarihi) 
                            .Skip((page - 1) * 5)  
                            .Take(5);  

            foreach (var item in query)
            {
                Models.User.Member member = new Models.User.Member();
                member.recid = item.userRecid;
                member.rumuz = item.users.rumuz;
                member.sehir = item.city.ilAdi;
                member.yas = methods.yasGetir(item.dogumTarihi.Value);
                member.virtualOnline = Convert.ToByte(item.aktifZaman.Value >= DateTime.Now ? 1 : 0);
                member.virtualOnTime = methods.aktifZaman(item.aktifZaman.Value, item.aktif, member.virtualOnline);
                member.meslek = item.soruMeslek.tanim;
                member.profileImages = item.islemResimler.resim;
                member.aktif = item.aktif;
                member.boy = item.boy ?? 0;
                member.kilo = item.kilo ?? 0;
                member.gozRengi = item.gozRengi ?? 0;
                member.amac = item.amac ?? 0;

                
                List<string> list = new List<string>();
                foreach (var item_image in _db.islemResimler.Where(x => x.kullanici.Equals(item.userRecid) && x.onay.Equals(1) && !x.resim.Contains("male")))
                {
                    list.Add(item_image.resim);
                }
                member.photos = list.ToArray();

                members.Add(member);
            }

            return members;
        }



        [HttpGet]
        [Route("list/new")]
        public List<Models.User.Member> list_new(int page = 1)
        {
            DateTime now = DateTime.Now.AddDays(-7);
            int currentUserRecid = methods.Request_userRecid();
            var currentUser = _db.users.First(x => x.recid.Equals(currentUserRecid));

            List<Models.User.Member> members = new List<Models.User.Member>();

            var query = _db.usersDetail.Where(x => x.users.recid != variables.guestId & x.users.durumRecid.Value == 1 & x.detayDurum == true & x.cinsiyet != currentUser.usersDetail.cinsiyet)
                               .Where(x => x.users.uyelikTarihi.Value > now)
                               .Where(e => !_db.islemEngel.Where(l => l.engelleyen == currentUserRecid & l.engellenen == e.userRecid).Any())
                               .Where(e => !_db.islemEngel.Where(l => l.engelleyen == e.userRecid & l.engellenen == currentUserRecid).Any())
                               .OrderByDescending(x => x.users.uyelikTarihi).Take(page == 1 ? 5 : 100);


            foreach (var item in query)
            {
                Models.User.Member member = new Models.User.Member();
                member.recid = item.userRecid;
                member.rumuz = item.users.rumuz;
                member.sehir = item.city.ilAdi;
                member.yas = methods.yasGetir(item.dogumTarihi.Value);
                member.virtualOnline = Convert.ToByte(item.aktifZaman.Value >= DateTime.Now ? 1 : 0);
                member.virtualOnTime = methods.aktifZaman(item.aktifZaman.Value, item.aktif, member.virtualOnline);
                member.meslek = item.soruMeslek.tanim;
                member.profileImages = item.islemResimler.resim;
                member.aktif = item.aktif;
                member.boy = item.boy ?? 0;
                member.kilo = item.kilo ?? 0;
                member.gozRengi = item.gozRengi ?? 0;
                member.amac = item.amac ?? 0;
                List<string> list = new List<string>();
                foreach (var item_image in _db.islemResimler.Where(x => x.kullanici.Equals(item.userRecid) & x.onay.Equals(1) & !x.resim.Contains("male")))
                    list.Add(item_image.resim);
                member.photos = list.ToArray();
                members.Add(member);
            }
            return members;
        }
        [HttpGet]
        [Route("list/photo")]
        public List<Models.User.Member> list_photo(int page = 1)
        {
            int currentUserRecid = methods.Request_userRecid();
            var currentUser = _db.users.First(x => x.recid.Equals(currentUserRecid));

            List<Models.User.Member> members = new List<Models.User.Member>();

            var query = _db.usersDetail.Where(x => x.users.durumRecid.Value == 1 & x.detayDurum == true & x.cinsiyet != currentUser.usersDetail.cinsiyet)
                               .Where(x => x.islemResimler.resim != null & !x.islemResimler.resim.Contains("male") & x.islemResimler.onay == 1)
                               .Where(e => !_db.islemEngel.Where(l => l.engelleyen == currentUserRecid & l.engellenen == e.userRecid).Any())
                               .Where(e => !_db.islemEngel.Where(l => l.engelleyen == e.userRecid & l.engellenen == currentUserRecid).Any())
                               .OrderByDescending(x => x.aktifZaman).Take(page == 1 ? 5 : 250);


            foreach (var item in query)
            {
                Models.User.Member member = new Models.User.Member();
                member.recid = item.userRecid;
                member.rumuz = item.users.rumuz;
                member.sehir = item.city.ilAdi;
                member.yas = methods.yasGetir(item.dogumTarihi.Value);
                member.virtualOnline = Convert.ToByte(item.aktifZaman.Value >= DateTime.Now ? 1 : 0);
                member.virtualOnTime = methods.aktifZaman(item.aktifZaman.Value, item.aktif, member.virtualOnline);
                member.meslek = item.soruMeslek.tanim;
                member.profileImages = item.islemResimler.resim;
                member.aktif = item.aktif;
                member.boy = item.boy ?? 0;
                member.kilo = item.kilo ?? 0;
                member.gozRengi = item.gozRengi ?? 0;
                member.amac = item.amac ?? 0;
                List<string> list = new List<string>();
                foreach (var item_image in _db.islemResimler.Where(x => x.kullanici.Equals(item.userRecid) & x.onay.Equals(1) & !x.resim.Contains("male")))
                    list.Add(item_image.resim);
                member.photos = list.ToArray();
                members.Add(member);
            }
            return members;
        }
        [HttpGet]
        [Route("list/end-entry")]
        public List<Models.User.Member> list_end_entry(int page = 1)
        {
            int currentUserRecid = methods.Request_userRecid();
            var currentUser = _db.users.First(x => x.recid.Equals(currentUserRecid));


            var query = _db.usersDetail.Where(x => x.users.recid != variables.guestId & x.users.durumRecid.Value == 1 & x.detayDurum == true & x.cinsiyet != currentUser.usersDetail.cinsiyet)
                              .Where(e => !_db.islemEngel.Where(l => l.engelleyen == currentUserRecid & l.engellenen == e.userRecid).Any())
                              .Where(e => !_db.islemEngel.Where(l => l.engelleyen == e.userRecid & l.engellenen == currentUserRecid).Any())
                              .OrderByDescending(x => x.sonGirisTarihi).Take(page == 1 ? 5 : 100);


            List<Models.User.Member> members = new List<Models.User.Member>();

            foreach (var item in query)
            {
                Models.User.Member member = new Models.User.Member();
                member.recid = item.userRecid;
                member.rumuz = item.users.rumuz;
                member.sehir = item.city.ilAdi;
                member.yas = methods.yasGetir(item.dogumTarihi.Value);
                member.virtualOnline = Convert.ToByte(item.aktifZaman.Value >= DateTime.Now ? 1 : 0);
                member.virtualOnTime = methods.aktifZaman(item.aktifZaman.Value, item.aktif, member.virtualOnline);
                member.meslek = item.soruMeslek.tanim;
                member.profileImages = item.islemResimler.resim;
                member.aktif = item.aktif;
                member.boy = item.boy ?? 0;
                member.kilo = item.kilo ?? 0;
                member.gozRengi = item.gozRengi ?? 0;
                member.amac = item.amac ?? 0;
                List<string> list = new List<string>();
                foreach (var item_image in _db.islemResimler.Where(x => x.kullanici.Equals(item.userRecid) & x.onay.Equals(1) & !x.resim.Contains("male")))
                    list.Add(item_image.resim);
                member.photos = list.ToArray();
                members.Add(member);
            }
            return members;
        }
        [HttpGet]
        [Route("list/not-far")]
        public List<Models.User.Member> list_not_far(int page = 1)
        {
            int currentUserRecid = methods.Request_userRecid();
            var currentUser = _db.users.First(x => x.recid.Equals(currentUserRecid));


            var query = _db.usersDetail.Where(x => x.users.durumRecid.Value == 1 & x.detayDurum == true & x.cinsiyet != currentUser.usersDetail.cinsiyet)
                              .Where(x => x.ilRecid == currentUser.usersDetail.ilRecid)
                              .Where(e => !_db.islemEngel.Where(l => l.engelleyen == currentUserRecid & l.engellenen == e.userRecid).Any())
                              .Where(e => !_db.islemEngel.Where(l => l.engelleyen == e.userRecid & l.engellenen == currentUserRecid).Any())
                              .OrderByDescending(x => x.aktifZaman).Take(page == 1 ? 5 : 100);


            List<Models.User.Member> members = new List<Models.User.Member>();
            foreach (var item in query)
            {
                Models.User.Member member = new Models.User.Member();
                member.recid = item.userRecid;
                member.rumuz = item.users.rumuz;
                member.sehir = item.city.ilAdi;
                member.yas = methods.yasGetir(item.dogumTarihi.Value);
                member.virtualOnline = Convert.ToByte(item.aktifZaman.Value >= DateTime.Now ? 1 : 0);
                member.virtualOnTime = methods.aktifZaman(item.aktifZaman.Value, item.aktif, member.virtualOnline);
                member.meslek = item.soruMeslek.tanim;
                member.profileImages = item.islemResimler.resim;
                member.aktif = item.aktif;
                member.boy = item.boy ?? 0;
                member.kilo = item.kilo ?? 0;
                member.gozRengi = item.gozRengi ?? 0;
                member.amac = item.amac ?? 0;
                List<string> list = new List<string>();
                foreach (var item_image in _db.islemResimler.Where(x => x.kullanici.Equals(item.userRecid) & x.onay.Equals(1) & !x.resim.Contains("male")))
                    list.Add(item_image.resim);
                member.photos = list.ToArray();
                members.Add(member);
            }
            return members;
        }
        [HttpGet]
        [Route("list/filter-nickname")]
        public List<Models.User.Member> list_filter_nickname(string nickname, int page = 1)
        {
            int currentUserRecid = methods.Request_userRecid();
            var currentUser = _db.users.First(x => x.recid.Equals(currentUserRecid));
            List<Models.User.Member> members = new List<Models.User.Member>();


            var query = _db.usersDetail.Where(x => x.users.durumRecid.Value == 1 & x.detayDurum == true & x.cinsiyet != currentUser.usersDetail.cinsiyet)
                               .Where(e => e.users.rumuz.ToLower().StartsWith(nickname.ToLower()))
                               .Where(e => !_db.islemEngel.Where(l => l.engelleyen == currentUserRecid & l.engellenen == e.userRecid).Any())
                               .Where(e => !_db.islemEngel.Where(l => l.engelleyen == e.userRecid & l.engellenen == currentUserRecid).Any())
                               .OrderByDescending(x => x.sonGirisTarihi).Take(page == 1 ? 5 : 100);


            foreach (var item in query)
            {
                Models.User.Member member = new Models.User.Member();
                member.recid = item.userRecid;
                member.rumuz = item.users.rumuz;
                member.sehir = item.city.ilAdi;
                member.yas = methods.yasGetir(item.dogumTarihi.Value);
                member.virtualOnline = Convert.ToByte(item.aktifZaman.Value >= DateTime.Now ? 1 : 0);
                member.virtualOnTime = methods.aktifZaman(item.aktifZaman.Value, item.aktif, member.virtualOnline);
                member.meslek = item.soruMeslek.tanim;
                member.profileImages = item.islemResimler.resim;
                member.aktif = item.aktif;
                member.boy = item.boy ?? 0;
                member.kilo = item.kilo ?? 0;
                member.gozRengi = item.gozRengi ?? 0;
                member.amac = item.amac ?? 0;
                List<string> list = new List<string>();
                foreach (var item_image in _db.islemResimler.Where(x => x.kullanici.Equals(item.userRecid) & x.onay.Equals(1) & !x.resim.Contains("male")))
                    list.Add(item_image.resim);
                member.photos = list.ToArray();
                members.Add(member);
            }
            return members;
        }
        [HttpGet]
        [Route("list/filter-parameters")]
        public List<Models.User.Member> list_filter_parameters(int country_recid = 0, int city_recid = 0, bool online = false, bool photo = false, int age_first = 0, int age_end = 0, int page = 1)
        {
            int currentUserRecid = methods.Request_userRecid();
            var currentUser = _db.users.First(x => x.recid.Equals(currentUserRecid));
            List<Models.User.Member> members = new List<Models.User.Member>();


            var query = _db.usersDetail.Where(x => x.users.durumRecid.Value == 1 & x.detayDurum == true & x.cinsiyet != currentUser.usersDetail.cinsiyet)
                               .Where(x => x.yas >= age_first & x.yas <= age_end)
                               .Where(x => x.ulkeRecid == country_recid)
                               .Where(e => !_db.islemEngel.Where(l => l.engelleyen == currentUserRecid & l.engellenen == e.userRecid).Any())
                               .Where(e => !_db.islemEngel.Where(l => l.engelleyen == e.userRecid & l.engellenen == currentUserRecid).Any())
                               .Where(x => city_recid > 0 ? x.ilRecid == city_recid : x.ilRecid > -1) // Filtre : Şehir
                               .Where(x => online ? x.aktifZaman >= DateTime.Now || x.aktif == true : x.aktifZaman != null) // Filtre : Online
                               .Where(x => photo ? !x.islemResimler.resim.Contains("male") : x.islemResimler.recid > -1) // Filtre : Resim
                               .ToList().OrderByDescending(x => x.sonGirisTarihi).Take(page == 1 ? 5 : 100);


            foreach (var item in query)
            {
                Models.User.Member member = new Models.User.Member();
                member.recid = item.userRecid;
                member.rumuz = item.users.rumuz;
                member.sehir = item.city.ilAdi;
                member.yas = methods.yasGetir(item.dogumTarihi.Value);
                member.virtualOnline = Convert.ToByte(item.aktifZaman.Value >= DateTime.Now ? 1 : 0);
                member.virtualOnTime = methods.aktifZaman(item.aktifZaman.Value, item.aktif, member.virtualOnline);
                member.meslek = item.soruMeslek.tanim;
                member.profileImages = item.islemResimler.resim;
                member.aktif = item.aktif;
                member.boy = item.boy ?? 0;
                member.kilo = item.kilo ?? 0;
                member.gozRengi = item.gozRengi ?? 0;
                member.amac = item.amac ?? 0;
                List<string> list = new List<string>();
                foreach (var item_image in _db.islemResimler.Where(x => x.kullanici.Equals(item.userRecid) & x.onay.Equals(1) & !x.resim.Contains("male")))
                    list.Add(item_image.resim);
                member.photos = list.ToArray();
                members.Add(member);
            }
            return members;
        }
        [HttpGet]
        [Route("list/harmony")]
        public List<Models.User.Member> list_harmony(int page = 1)
        {
            int currentUserRecid = methods.Request_userRecid();
            List<Models.User.Member> members = new List<Models.User.Member>();


            var query = _db.usersUygunUyeler.Where(x => x.kullanici == currentUserRecid)
                     .Where(e => !_db.islemEngel.Where(l => l.engelleyen == currentUserRecid & l.engellenen == e.userRecid).Any())
                     .Where(e => !_db.islemEngel.Where(l => l.engelleyen == e.userRecid & l.engellenen == currentUserRecid).Any())
                     .OrderByDescending(x => x.uyumOrani).Take(page == 1 ? 5 : 100);


            foreach (var item in query)
            {
                Models.User.Member member = new Models.User.Member();
                member.recid = item.userRecid;
                member.rumuz = item.usersDetail.users.rumuz;
                member.sehir = item.usersDetail.city.ilAdi;
                member.yas = methods.yasGetir(item.usersDetail.dogumTarihi.Value);
                member.virtualOnline = methods.onlineResult(item.usersDetail.aktifZaman.Value, item.usersDetail.aktif);
                member.virtualOnTime = methods.aktifZaman(item.usersDetail.aktifZaman.Value, item.usersDetail.aktif, member.virtualOnline);
                member.meslek = item.usersDetail.soruMeslek.tanim;
                member.profileImages = item.usersDetail.islemResimler.resim;
                member.aktif = item.usersDetail.aktif;
                member.boy = item.usersDetail.boy ?? 0;
                member.kilo = item.usersDetail.kilo ?? 0;
                member.gozRengi = item.usersDetail.gozRengi ?? 0;
                member.amac = item.usersDetail.amac ?? 0;
                List<string> list = new List<string>();
                foreach (var item_image in _db.islemResimler.Where(x => x.kullanici.Equals(item.userRecid) & x.onay.Equals(1) & !x.resim.Contains("male")))
                    list.Add(item_image.resim);
                member.photos = list.ToArray();
                members.Add(member);
            }
            return members;
        }
        [HttpGet]
        [Route("list/birthday")]
        public List<Models.User.Member> list_birthday(int page = 1)
        {
            int currentUserRecid = methods.Request_userRecid();
            var currentUser = _db.users.First(x => x.recid.Equals(currentUserRecid));
            List<Models.User.Member> members = new List<Models.User.Member>();
            DateTime dt = DateTime.Now;

            var query = _db.usersDetail.Where(x => x.users.durumRecid.Value == 1 & x.detayDurum == true & x.cinsiyet != currentUser.usersDetail.cinsiyet & x.dogumTarihi.Value.Month == dt.Month & x.dogumTarihi.Value.Day == dt.Day)
                     .Where(e => !_db.islemEngel.Where(l => l.engelleyen == currentUserRecid & l.engellenen == e.userRecid).Any())
                     .Where(e => !_db.islemEngel.Where(l => l.engelleyen == e.userRecid & l.engellenen == currentUserRecid).Any())
                     .OrderByDescending(x => x.aktifZaman).Take(page == 1 ? 5 : 100);


            foreach (var item in query)
            {
                Models.User.Member member = new Models.User.Member();
                member.recid = item.userRecid;
                member.rumuz = item.users.rumuz;
                member.sehir = item.city.ilAdi;
                member.yas = methods.yasGetir(item.dogumTarihi.Value);
                member.virtualOnline = Convert.ToByte(item.aktifZaman.Value >= DateTime.Now ? 1 : 0);
                member.virtualOnTime = methods.aktifZaman(item.aktifZaman.Value, item.aktif, member.virtualOnline);
                member.meslek = item.soruMeslek.tanim;
                member.profileImages = item.islemResimler.resim;
                member.aktif = item.aktif;
                member.boy = item.boy ?? 0;
                member.kilo = item.kilo ?? 0;
                member.gozRengi = item.gozRengi ?? 0;
                member.amac = item.amac ?? 0;
                List<string> list = new List<string>();
                foreach (var item_image in _db.islemResimler.Where(x => x.kullanici.Equals(item.userRecid) & x.onay.Equals(1) & !x.resim.Contains("male")))
                    list.Add(item_image.resim);
                member.photos = list.ToArray();
                members.Add(member);
            }
            return members;
        }
        [HttpGet]
        [Route("list/my-like")]
        public List<Models.User.Member> list_my_like(int page = 1)
        {

            int currentUserRecid = methods.Request_userRecid();
            List<Models.User.Member> members = new List<Models.User.Member>();


            var query = _db.islemBegen.Where(x => x.begenen == currentUserRecid & x.sil.Value == false & x.gizle.Value != currentUserRecid & x.users1.durumRecid.Value == 1 & x.users1.usersDetail.detayDurum == true)
                        .Where(e => !_db.islemEngel.Where(l => l.engelleyen == currentUserRecid & l.engellenen == e.begenilen).Any())
                        .Where(e => !_db.islemEngel.Where(l => l.engelleyen == e.begenilen & l.engellenen == currentUserRecid).Any())
                        .OrderByDescending(x => x.begenmeTarihi).Take(page == 1 ? 5 : 100);


            foreach (var item in query)
            {
                Models.User.Member member = new Models.User.Member();
                member.recid = item.users1.usersDetail.userRecid;
                member.rumuz = item.users1.usersDetail.users.rumuz;
                member.sehir = item.users1.usersDetail.city.ilAdi;
                member.yas = methods.yasGetir(item.users1.usersDetail.dogumTarihi.Value);
                member.virtualOnline = methods.onlineResult(item.users1.usersDetail.aktifZaman.Value, item.users1.usersDetail.aktif);
                member.virtualOnTime = methods.aktifZaman(item.users1.usersDetail.aktifZaman.Value, item.users1.usersDetail.aktif, member.virtualOnline);
                member.meslek = item.users1.usersDetail.soruMeslek.tanim;
                member.profileImages = item.users1.usersDetail.islemResimler.resim;
                member.aktif = item.users1.usersDetail.aktif;
                member.processDate = item.begenmeTarihi.ToString("dd.MM.yyyy HH:mm");
                member.boy = item.users1.usersDetail.boy ?? 0;
                member.kilo = item.users1.usersDetail.kilo ?? 0;
                member.gozRengi = item.users1.usersDetail.gozRengi ?? 0;
                member.amac = item.users1.usersDetail.amac ?? 0;
                List<string> list = new List<string>();
                foreach (var item_image in _db.islemResimler.Where(x => x.kullanici.Equals(item.users1.usersDetail.userRecid) & x.onay.Equals(1) & !x.resim.Contains("male")))
                    list.Add(item_image.resim);
                member.photos = list.ToArray();
                members.Add(member);
            }
            return members;
        }
        [HttpGet]
        [Route("list/me-like")]
        public List<Models.User.Member> list_me_like(int page = 1)
        {

            int currentUserRecid = methods.Request_userRecid();
            List<Models.User.Member> members = new List<Models.User.Member>();


            var query = _db.islemBegen.Where(x => x.begenilen == currentUserRecid & x.sil.Value == false & x.gizle.Value != currentUserRecid & x.users.durumRecid.Value == 1 & x.users.usersDetail.detayDurum == true)
                        .Where(e => !_db.islemEngel.Where(l => l.engelleyen == currentUserRecid & l.engellenen == e.begenen).Any())
                        .Where(e => !_db.islemEngel.Where(l => l.engelleyen == e.begenen & l.engellenen == currentUserRecid).Any())
                        .OrderByDescending(x => x.begenmeTarihi).Take(page == 1 ? 5 : 100);


            foreach (var item in query)
            {
                Models.User.Member member = new Models.User.Member();
                member.recid = item.users.usersDetail.userRecid;
                member.rumuz = item.users.usersDetail.users.rumuz;
                member.sehir = item.users.usersDetail.city.ilAdi;
                member.yas = methods.yasGetir(item.users.usersDetail.dogumTarihi.Value);
                member.virtualOnline = methods.onlineResult(item.users.usersDetail.aktifZaman.Value, item.users.usersDetail.aktif);
                member.virtualOnTime = methods.aktifZaman(item.users.usersDetail.aktifZaman.Value, item.users.usersDetail.aktif, member.virtualOnline);
                member.meslek = item.users.usersDetail.soruMeslek.tanim;
                member.profileImages = item.users.usersDetail.islemResimler.resim;
                member.aktif = item.users.usersDetail.aktif;
                member.processDate = item.begenmeTarihi.ToString("dd.MM.yyyy HH:mm");
                member.boy = item.users.usersDetail.boy ?? 0;
                member.kilo = item.users.usersDetail.kilo ?? 0;
                member.gozRengi = item.users.usersDetail.gozRengi ?? 0;
                member.amac = item.users.usersDetail.amac ?? 0;
                List<string> list = new List<string>();
                foreach (var item_image in _db.islemResimler.Where(x => x.kullanici.Equals(item.users.usersDetail.userRecid) & x.onay.Equals(1) & !x.resim.Contains("male")))
                    list.Add(item_image.resim);
                member.photos = list.ToArray();
                members.Add(member);
            }
            return members;
        }
        [HttpGet]
        [Route("list/most-visit")]
        public List<Models.User.Member> list_most_visit(int page = 1)
        {

            int currentUserRecid = methods.Request_userRecid();
            var currentUser = _db.users.First(x => x.recid.Equals(currentUserRecid));
            List<Models.User.Member> members = new List<Models.User.Member>();

            var query = _db.usersDetail.Where(x => x.users.durumRecid.Value == 1 & x.detayDurum == true & x.cinsiyet != currentUser.usersDetail.cinsiyet)
                     .Where(e => !_db.islemEngel.Where(l => l.engelleyen == currentUserRecid & l.engellenen == e.userRecid).Any())
                     .Where(e => !_db.islemEngel.Where(l => l.engelleyen == e.userRecid & l.engellenen == currentUserRecid).Any())
                     .OrderByDescending(x => x.ziyaret).Take(page == 1 ? 5 : 15);


            foreach (var item in query)
            {
                Models.User.Member member = new Models.User.Member();
                member.recid = item.userRecid;
                member.rumuz = item.users.rumuz;
                member.sehir = item.city.ilAdi;
                member.yas = methods.yasGetir(item.dogumTarihi.Value);
                member.virtualOnline = Convert.ToByte(item.aktifZaman.Value >= DateTime.Now ? 1 : 0);
                member.virtualOnTime = methods.aktifZaman(item.aktifZaman.Value, item.aktif, member.virtualOnline);
                member.meslek = item.soruMeslek.tanim;
                member.profileImages = item.islemResimler.resim;
                member.aktif = item.aktif;
                member.boy = item.boy ?? 0;
                member.kilo = item.kilo ?? 0;
                member.gozRengi = item.gozRengi ?? 0;
                member.amac = item.amac ?? 0;
                List<string> list = new List<string>();
                foreach (var item_image in _db.islemResimler.Where(x => x.kullanici.Equals(item.userRecid) & x.onay.Equals(1) & !x.resim.Contains("male")))
                    list.Add(item_image.resim);
                member.photos = list.ToArray();
                members.Add(member);
            }
            return members;
        }
        [HttpGet]
        [Route("list/most-favorite")]
        public List<Models.User.Member> list_most_favorite(int page = 1)
        {

            int currentUserRecid = methods.Request_userRecid();
            var currentUser = _db.users.First(x => x.recid.Equals(currentUserRecid));
            List<Models.User.Member> members = new List<Models.User.Member>();

            var query = _db.usersDetail.Where(x => x.users.durumRecid.Value == 1 & x.detayDurum == true & x.cinsiyet != currentUser.usersDetail.cinsiyet)
                     .Where(e => !_db.islemEngel.Where(l => l.engelleyen == currentUserRecid & l.engellenen == e.userRecid).Any())
                     .Where(e => !_db.islemEngel.Where(l => l.engelleyen == e.userRecid & l.engellenen == currentUserRecid).Any())
                     .OrderByDescending(x => x.favori).Take(page == 1 ? 5 : 15);


            foreach (var item in query)
            {
                Models.User.Member member = new Models.User.Member();
                member.recid = item.userRecid;
                member.rumuz = item.users.rumuz;
                member.sehir = item.city.ilAdi;
                member.yas = methods.yasGetir(item.dogumTarihi.Value);
                member.virtualOnline = Convert.ToByte(item.aktifZaman.Value >= DateTime.Now ? 1 : 0);
                member.virtualOnTime = methods.aktifZaman(item.aktifZaman.Value, item.aktif, member.virtualOnline);
                member.meslek = item.soruMeslek.tanim;
                member.profileImages = item.islemResimler.resim;
                member.aktif = item.aktif;
                member.boy = item.boy ?? 0;
                member.kilo = item.kilo ?? 0;
                member.gozRengi = item.gozRengi ?? 0;
                member.amac = item.amac ?? 0;
                List<string> list = new List<string>();
                foreach (var item_image in _db.islemResimler.Where(x => x.kullanici.Equals(item.userRecid) & x.onay.Equals(1) & !x.resim.Contains("male")))
                    list.Add(item_image.resim);
                member.photos = list.ToArray();
                members.Add(member);
            }
            return members;
        }
        [HttpGet]
        [Route("list/my-barrier")]
        public List<Models.User.Member> list_my_barrier(int page = 1)
        {

            int currentUserRecid = methods.Request_userRecid();
            List<Models.User.Member> members = new List<Models.User.Member>();

            var query = _db.islemEngel.Where(x => x.engelleyen == currentUserRecid & x.sil.Value == false & x.gizle.Value != currentUserRecid & x.users1.durumRecid.Value == 1 & x.users1.usersDetail.detayDurum == true)
                        .OrderByDescending(x => x.engellemeTarihi).Take(page == 1 ? 5 : 100);


            foreach (var item in query)
            {
                Models.User.Member member = new Models.User.Member();
                member.recid = item.users1.usersDetail.userRecid;
                member.rumuz = item.users1.usersDetail.users.rumuz;
                member.sehir = item.users1.usersDetail.city.ilAdi;
                member.yas = methods.yasGetir(item.users1.usersDetail.dogumTarihi.Value);
                member.virtualOnline = methods.onlineResult(item.users1.usersDetail.aktifZaman.Value, item.users1.usersDetail.aktif);
                member.virtualOnTime = methods.aktifZaman(item.users1.usersDetail.aktifZaman.Value, item.users1.usersDetail.aktif, member.virtualOnline);
                member.meslek = item.users1.usersDetail.soruMeslek.tanim;
                member.profileImages = item.users1.usersDetail.islemResimler.resim;
                member.aktif = item.users1.usersDetail.aktif;
                member.boy = item.users1.usersDetail.boy ?? 0;
                member.kilo = item.users1.usersDetail.kilo ?? 0;
                member.gozRengi = item.users1.usersDetail.gozRengi ?? 0;
                member.amac = item.users1.usersDetail.amac ?? 0;
                member.processDate = item.engellemeTarihi.Value.ToString("dd.MM.yyyy HH:mm");
                List<string> list = new List<string>();
                foreach (var item_image in _db.islemResimler.Where(x => x.kullanici.Equals(item.users1.usersDetail.userRecid) & x.onay.Equals(1) & !x.resim.Contains("male")))
                    list.Add(item_image.resim);
                member.photos = list.ToArray();
                members.Add(member);
            }
            return members;
        }
        [HttpGet]
        [Route("list/me-favorite")]
        public List<Models.User.Member> list_me_favorite(int page = 1)
        {
            int currentUserRecid = methods.Request_userRecid();
            List<Models.User.Member> members = new List<Models.User.Member>();

            var query = _db.islemFavori.Where(x => x.eklenen == currentUserRecid & x.sil.Value == false & x.gizle.Value != currentUserRecid & x.users1.durumRecid.Value == 1 & x.users1.usersDetail.detayDurum == true)
                        .Where(e => !_db.islemEngel.Where(l => l.engelleyen == currentUserRecid & l.engellenen == e.ekleyen).Any())
                        .Where(e => !_db.islemEngel.Where(l => l.engelleyen == e.ekleyen & l.engellenen == currentUserRecid).Any())
                        .OrderByDescending(x => x.eklemeTarihi).Take(page == 1 ? 5 : 100);


            foreach (var item in query)
            {
                Models.User.Member member = new Models.User.Member();
                member.recid = item.users1.usersDetail.userRecid;
                member.rumuz = item.users1.usersDetail.users.rumuz;
                member.sehir = item.users1.usersDetail.city.ilAdi;
                member.yas = methods.yasGetir(item.users1.usersDetail.dogumTarihi.Value);
                member.virtualOnline = methods.onlineResult(item.users1.usersDetail.aktifZaman.Value, item.users1.usersDetail.aktif);
                member.virtualOnTime = methods.aktifZaman(item.users1.usersDetail.aktifZaman.Value, item.users1.usersDetail.aktif, member.virtualOnline);
                member.meslek = item.users1.usersDetail.soruMeslek.tanim;
                member.profileImages = item.users1.usersDetail.islemResimler.resim;
                member.aktif = item.users1.usersDetail.aktif;
                member.processDate = item.eklemeTarihi.ToString("dd.MM.yyyy HH:mm");
                member.boy = item.users1.usersDetail.boy ?? 0;
                member.kilo = item.users1.usersDetail.kilo ?? 0;
                member.gozRengi = item.users1.usersDetail.gozRengi ?? 0;
                member.amac = item.users1.usersDetail.amac ?? 0;
                List<string> list = new List<string>();
                foreach (var item_image in _db.islemResimler.Where(x => x.kullanici.Equals(item.users1.usersDetail.userRecid) & x.onay.Equals(1) & !x.resim.Contains("male")))
                    list.Add(item_image.resim);
                member.photos = list.ToArray();
                members.Add(member);
            }
            return members;
        }
        [HttpGet]
        [Route("list/my-favorite")]
        public List<Models.User.Member> list_my_favorite(int page = 1)
        {

            int currentUserRecid = methods.Request_userRecid();
            List<Models.User.Member> members = new List<Models.User.Member>();

            var query = _db.islemFavori.Where(x => x.ekleyen == currentUserRecid & x.sil.Value == false & x.gizle.Value != currentUserRecid & x.users.durumRecid.Value == 1 & x.users.usersDetail.detayDurum == true)
                        .Where(e => !_db.islemEngel.Where(l => l.engelleyen == currentUserRecid & l.engellenen == e.eklenen).Any())
                        .Where(e => !_db.islemEngel.Where(l => l.engelleyen == e.eklenen & l.engellenen == currentUserRecid).Any())
                        .OrderByDescending(x => x.eklemeTarihi).Take(page == 1 ? 5 : 100);


            foreach (var item in query)
            {
                Models.User.Member member = new Models.User.Member();
                member.recid = item.users.usersDetail.userRecid;
                member.rumuz = item.users.usersDetail.users.rumuz;
                member.sehir = item.users.usersDetail.city.ilAdi;
                member.yas = methods.yasGetir(item.users.usersDetail.dogumTarihi.Value);
                member.virtualOnline = methods.onlineResult(item.users.usersDetail.aktifZaman.Value, item.users.usersDetail.aktif);
                member.virtualOnTime = methods.aktifZaman(item.users.usersDetail.aktifZaman.Value, item.users.usersDetail.aktif, member.virtualOnline);
                member.meslek = item.users.usersDetail.soruMeslek.tanim;
                member.profileImages = item.users.usersDetail.islemResimler.resim;
                member.aktif = item.users.usersDetail.aktif;
                member.processDate = item.eklemeTarihi.ToString("dd.MM.yyyy HH:mm");
                member.boy = item.users.usersDetail.boy ?? 0;
                member.kilo = item.users.usersDetail.kilo ?? 0;
                member.gozRengi = item.users.usersDetail.gozRengi ?? 0;
                member.amac = item.users.usersDetail.amac ?? 0;
                List<string> list = new List<string>();
                foreach (var item_image in _db.islemResimler.Where(x => x.kullanici.Equals(item.users.usersDetail.userRecid) & x.onay.Equals(1) & !x.resim.Contains("male")))
                    list.Add(item_image.resim);
                member.photos = list.ToArray();
                members.Add(member);
            }
            return members;
        }
        [HttpGet]
        [Route("list/my-visit")]
        public List<Models.User.Member> list_my_visit(int page = 1)
        {

            int currentUserRecid = methods.Request_userRecid();
            List<Models.User.Member> members = new List<Models.User.Member>();

            var query = _db.islemGezen.Where(x => x.gezen == currentUserRecid & x.sil.Value == false & x.gizle.Value != currentUserRecid & x.users1.durumRecid.Value == 1 & x.users1.usersDetail.detayDurum == true)
                        .Where(e => !_db.islemEngel.Where(l => l.engelleyen == currentUserRecid & l.engellenen == e.gezilen).Any())
                        .Where(e => !_db.islemEngel.Where(l => l.engelleyen == e.gezilen & l.engellenen == currentUserRecid).Any())
                        .OrderByDescending(x => x.tarih).Take(page == 1 ? 5 : 100);


            foreach (var item in query)
            {
                Models.User.Member member = new Models.User.Member();
                member.recid = item.users1.usersDetail.userRecid;
                member.rumuz = item.users1.usersDetail.users.rumuz;
                member.sehir = item.users1.usersDetail.city.ilAdi;
                member.yas = methods.yasGetir(item.users1.usersDetail.dogumTarihi.Value);
                member.virtualOnline = methods.onlineResult(item.users1.usersDetail.aktifZaman.Value, item.users1.usersDetail.aktif);
                member.virtualOnTime = methods.aktifZaman(item.users1.usersDetail.aktifZaman.Value, item.users1.usersDetail.aktif, member.virtualOnline);
                member.meslek = item.users1.usersDetail.soruMeslek.tanim;
                member.profileImages = item.users1.usersDetail.islemResimler.resim;
                member.aktif = item.users1.usersDetail.aktif;
                member.processDate = item.tarih.ToString("dd.MM.yyyy HH:mm");
                member.boy = item.users1.usersDetail.boy ?? 0;
                member.kilo = item.users1.usersDetail.kilo ?? 0;
                member.gozRengi = item.users1.usersDetail.gozRengi ?? 0;
                member.amac = item.users1.usersDetail.amac ?? 0;
                List<string> list = new List<string>();
                foreach (var item_image in _db.islemResimler.Where(x => x.kullanici.Equals(item.users1.usersDetail.userRecid) & x.onay.Equals(1) & !x.resim.Contains("male")))
                    list.Add(item_image.resim);
                member.photos = list.ToArray();
                members.Add(member);
            }
            return members;
        }
        [HttpGet]
        [Route("list/me-visit")]
        public List<Models.User.Member> list_me_visit(int page = 1)
        {

            int currentUserRecid = methods.Request_userRecid();
            List<Models.User.Member> members = new List<Models.User.Member>();

            var query = _db.islemGezen.Where(x => x.gezilen == currentUserRecid & x.sil.Value == false & x.gizle.Value != currentUserRecid & x.users.durumRecid.Value == 1 & x.users.usersDetail.detayDurum == true)
                        .Where(e => !_db.islemEngel.Where(l => l.engelleyen == currentUserRecid & l.engellenen == e.gezen).Any())
                        .Where(e => !_db.islemEngel.Where(l => l.engelleyen == e.gezen & l.engellenen == currentUserRecid).Any())
                        .OrderByDescending(x => x.tarih).Take(page == 1 ? 5 : 250);


            foreach (var item in query)
            {
                Models.User.Member member = new Models.User.Member();
                member.recid = item.users.usersDetail.userRecid;
                member.rumuz = item.users.usersDetail.users.rumuz;
                member.sehir = item.users.usersDetail.city.ilAdi;
                member.yas = methods.yasGetir(item.users.usersDetail.dogumTarihi.Value);
                member.virtualOnline = methods.onlineResult(item.users.usersDetail.aktifZaman.Value, item.users.usersDetail.aktif);
                member.virtualOnTime = methods.aktifZaman(item.users.usersDetail.aktifZaman.Value, item.users.usersDetail.aktif, member.virtualOnline);
                member.meslek = item.users.usersDetail.soruMeslek.tanim;
                member.profileImages = item.users.usersDetail.islemResimler.resim;
                member.aktif = item.users.usersDetail.aktif;
                member.processDate = item.tarih.ToString("dd.MM.yyyy HH:mm");
                member.boy = item.users.usersDetail.boy ?? 0;
                member.kilo = item.users.usersDetail.kilo ?? 0;
                member.gozRengi = item.users.usersDetail.gozRengi ?? 0;
                member.amac = item.users.usersDetail.amac ?? 0;
                List<string> list = new List<string>();
                foreach (var item_image in _db.islemResimler.Where(x => x.kullanici.Equals(item.users.usersDetail.userRecid) & x.onay.Equals(1) & !x.resim.Contains("male")))
                    list.Add(item_image.resim);
                member.photos = list.ToArray();
                members.Add(member);
            }
            return members;
        }
        [HttpGet]
        [Route("list/me-score")]
        public List<Models.User.Member> list_me_score(int page = 1)
        {
            int currentUserRecid = methods.Request_userRecid();
            List<Models.User.Member> members = new List<Models.User.Member>();

            var query = _db.islemPuan.Where(x => x.puanAlan == currentUserRecid & x.sil.Value == false & x.gizle.Value != currentUserRecid & x.users.durumRecid.Value == 1 & x.users.usersDetail.detayDurum == true)
                        .Where(e => !_db.islemEngel.Where(l => l.engelleyen == currentUserRecid & l.engellenen == e.puanVeren).Any())
                        .Where(e => !_db.islemEngel.Where(l => l.engelleyen == e.puanVeren & l.engellenen == currentUserRecid).Any())
                        .OrderByDescending(x => x.tarih).Take(page == 1 ? 5 : 100);


            foreach (var item in query)
            {

                Models.User.Member member = new Models.User.Member();
                member.recid = item.users.usersDetail.userRecid;
                member.rumuz = item.users.usersDetail.users.rumuz;
                member.sehir = item.users.usersDetail.city.ilAdi;
                member.yas = methods.yasGetir(item.users.usersDetail.dogumTarihi.Value);
                member.virtualOnline = methods.onlineResult(item.users.usersDetail.aktifZaman.Value, item.users.usersDetail.aktif);
                member.virtualOnTime = methods.aktifZaman(item.users.usersDetail.aktifZaman.Value, item.users.usersDetail.aktif, member.virtualOnline);
                member.meslek = item.users.usersDetail.soruMeslek.tanim;
                member.profileImages = item.users.usersDetail.islemResimler.resim;
                member.aktif = item.users.usersDetail.aktif;
                member.processDate = item.tarih.ToString("dd.MM.yyyy HH:mm");
                member.boy = item.users.usersDetail.boy ?? 0;
                member.kilo = item.users.usersDetail.kilo ?? 0;
                member.gozRengi = item.users.usersDetail.gozRengi ?? 0;
                member.amac = item.users.usersDetail.amac ?? 0;
                List<string> list = new List<string>();
                foreach (var item_image in _db.islemResimler.Where(x => x.kullanici.Equals(item.users.usersDetail.userRecid) & x.onay.Equals(1) & !x.resim.Contains("male")))
                    list.Add(item_image.resim);
                member.photos = list.ToArray();
                members.Add(member);
            }
            return members;
        }
        [HttpGet]
        [Route("list/my-score")]
        public List<Models.User.Member> list_my_score(int page = 1)
        {
            int currentUserRecid = methods.Request_userRecid();
            List<Models.User.Member> members = new List<Models.User.Member>();

            var query = _db.islemPuan.Where(x => x.puanVeren == currentUserRecid & x.sil.Value == false & x.gizle.Value != currentUserRecid & x.users1.durumRecid.Value == 1 & x.users1.usersDetail.detayDurum == true)
                        .Where(e => !_db.islemEngel.Where(l => l.engelleyen == currentUserRecid & l.engellenen == e.puanAlan).Any())
                        .Where(e => !_db.islemEngel.Where(l => l.engelleyen == e.puanAlan & l.engellenen == currentUserRecid).Any())
                        .OrderByDescending(x => x.tarih).Take(page == 1 ? 5 : 100);

            foreach (var item in query)
            {
                Models.User.Member member = new Models.User.Member();
                member.recid = item.users1.usersDetail.userRecid;
                member.rumuz = item.users1.usersDetail.users.rumuz;
                member.sehir = item.users1.usersDetail.city.ilAdi;
                member.yas = methods.yasGetir(item.users1.usersDetail.dogumTarihi.Value);
                member.virtualOnline = methods.onlineResult(item.users1.usersDetail.aktifZaman.Value, item.users1.usersDetail.aktif);
                member.virtualOnTime = methods.aktifZaman(item.users1.usersDetail.aktifZaman.Value, item.users1.usersDetail.aktif, member.virtualOnline);
                member.meslek = item.users1.usersDetail.soruMeslek.tanim;
                member.profileImages = item.users1.usersDetail.islemResimler.resim;
                member.aktif = item.users1.usersDetail.aktif;
                member.processDate = item.tarih.ToString("dd.MM.yyyy HH:mm");
                member.boy = item.users1.usersDetail.boy ?? 0;
                member.kilo = item.users1.usersDetail.kilo ?? 0;
                member.gozRengi = item.users1.usersDetail.gozRengi ?? 0;
                member.amac = item.users1.usersDetail.amac ?? 0;
                List<string> list = new List<string>();
                foreach (var item_image in _db.islemResimler.Where(x => x.kullanici.Equals(item.users1.usersDetail.userRecid) & x.onay.Equals(1) & !x.resim.Contains("male")))
                    list.Add(item_image.resim);
                member.photos = list.ToArray();
                members.Add(member);
            }
            return members;
        }
        [HttpGet]
        [Route("list/my-wallshare-likes")]
        public List<Models.User.Member> list_my_wallshare_likes(int page = 1, int wsrecid = 0)
        {

            int currentUserRecid = methods.Request_userRecid();
            List<Models.User.Member> members = new List<Models.User.Member>();

            var query = _db.duvarCanliBegenenler.Where(x => x.duvarId.Equals(wsrecid) & x.paylasanId.Equals(currentUserRecid) & x.usersDetail.users.durumRecid == 1)
                        .Where(e => !_db.islemEngel.Where(l => l.engelleyen == currentUserRecid & l.engellenen == e.usersDetail.userRecid).Any())
                        .Where(e => !_db.islemEngel.Where(l => l.engelleyen == e.usersDetail.userRecid & l.engellenen == currentUserRecid).Any())
                        .OrderByDescending(x => x.tarih).Take(page == 1 ? 5 : 100);

            foreach (var item in query)
            {
                Models.User.Member member = new Models.User.Member();
                member.recid = item.usersDetail.userRecid;
                member.rumuz = item.usersDetail.users.rumuz;
                member.sehir = item.usersDetail.city.ilAdi;
                member.yas = methods.yasGetir(item.usersDetail.dogumTarihi.Value);
                member.virtualOnline = methods.onlineResult(item.usersDetail.aktifZaman.Value, item.usersDetail.aktif);
                member.virtualOnTime = methods.aktifZaman(item.usersDetail.aktifZaman.Value, item.usersDetail.aktif, member.virtualOnline);
                member.meslek = item.usersDetail.soruMeslek.tanim;
                member.profileImages = item.usersDetail.islemResimler.resim;
                member.aktif = item.usersDetail.aktif;
                member.boy = item.usersDetail.boy ?? 0;
                member.kilo = item.usersDetail.kilo ?? 0;
                member.gozRengi = item.usersDetail.gozRengi ?? 0;
                member.amac = item.usersDetail.amac ?? 0;
                List<string> list = new List<string>();
                foreach (var item_image in _db.islemResimler.Where(x => x.kullanici.Equals(item.usersDetail.userRecid) & x.onay.Equals(1) & !x.resim.Contains("male")))
                    list.Add(item_image.resim);
                member.photos = list.ToArray();
                members.Add(member);
            }
            return members;
        }
        #endregion
        #region işlemler
        [HttpPost]
        [Route("add/visit")]
        public void add_visit(int memberId)
        {
            app_api.Classes.Result result = new app_api.Classes.Result();
            int currentUserRecid = methods.Request_userRecid();
            var currenUser = _db.users.First(x => x.recid.Equals(currentUserRecid));
            Models.Ado.islemGezen gez = new Models.Ado.islemGezen();
            gez.gezen = currentUserRecid;
            gez.gezilen = memberId;
            gez.tarih = DateTime.Now;
            gez.gizle = 0;
            gez.sil = false;
            gez.sms_case = false;
            _db.islemGezen.Add(gez);
            Models.Ado.usersDetail det = new Models.Ado.usersDetail();
            det = _db.usersDetail.First(x => x.userRecid.Equals(memberId));
            if (det.ziyaret == null)
            {
                det.ziyaret = 0;
            }
            det.ziyaret = det.ziyaret + 1;
            det.firabase_set = false;
            det.firebase_gezen = det.firebase_gezen + 1;
            _db.SaveChanges();
            methods.push_notification_mobile_app(memberId, "Profiliniz Ziyaret Edildi", _db.users.First(x => x.recid.Equals(currentUserRecid)).rumuz + " Sizin Profilinizi Gezdi", (int)app_api.Classes.Enums.NotificationTypes.Gezen);
        }
        [HttpPost]
        [Route("change/favorite")]
        public bool chance_favorite(int memberId)
        {
            int currentUserRecid = methods.Request_userRecid();
            var query = _db.islemFavori.FirstOrDefault(x => x.ekleyen.Equals(currentUserRecid) & x.eklenen.Equals(memberId));
            // Ekle
            if (query == null)
            {
                Models.Ado.islemFavori table = new Models.Ado.islemFavori();
                table.eklemeTarihi = DateTime.Now;
                table.ekleyen = currentUserRecid;
                table.eklenen = memberId;
                table.gizle = 0;
                table.sil = false;
                table.sms_case = true;
                _db.islemFavori.Add(table);
                Models.Ado.usersDetail usersDetail = _db.usersDetail.First(x => x.userRecid.Equals(memberId));
                usersDetail.firabase_set = false;
                usersDetail.firebase_favori = usersDetail.firebase_favori + 1;
                _db.SaveChanges();
                methods.push_notification_mobile_app(memberId, "Favori Olarak Eklendiniz", _db.users.First(x => x.recid.Equals(currentUserRecid)).rumuz + " Sizi Favori Listesine Ekledi", (int)app_api.Classes.Enums.NotificationTypes.Favori);
                return true;
            }
            else // Çıkar
            {
                _db.islemFavori.Remove(query);
                Models.Ado.usersDetail usersDetail = _db.usersDetail.First(x => x.userRecid.Equals(memberId));
                usersDetail.firabase_set = false;
                usersDetail.firebase_favori = usersDetail.firebase_favori <= 0 ? 0 : usersDetail.firebase_favori - 1;
                _db.SaveChanges();
                return false;
            }
        }
        [HttpPost]
        [Route("change/like")]
        public bool chance_like(int memberId)
        {
            int currentUserRecid = methods.Request_userRecid();
            var query = _db.islemBegen.FirstOrDefault(x => x.begenen.Equals(currentUserRecid) & x.begenilen.Equals(memberId));
            // Ekle
            if (query == null)
            {
                Models.Ado.islemBegen table = new Models.Ado.islemBegen();
                table.begenmeTarihi = DateTime.Now;
                table.begenen = currentUserRecid;
                table.begenilen = memberId;
                table.gizle = 0;
                table.sil = false;
                _db.islemBegen.Add(table);
                Models.Ado.usersDetail usersDetail = _db.usersDetail.First(x => x.userRecid.Equals(memberId));
                usersDetail.firabase_set = false;
                usersDetail.firebase_begen = usersDetail.firebase_begen + 1;
                _db.SaveChanges();
                methods.push_notification_mobile_app(memberId, "Profiliniz Beğenildi", _db.users.First(x => x.recid.Equals(currentUserRecid)).rumuz + " Sizin Profilinizi Beğendi", (int)app_api.Classes.Enums.NotificationTypes.Begeni);
                return true;
            }
            else // Çıkar
            {
                _db.islemBegen.Remove(query);
                Models.Ado.usersDetail usersDetail = _db.usersDetail.First(x => x.userRecid.Equals(memberId));
                usersDetail.firabase_set = false;
                usersDetail.firebase_begen = usersDetail.firebase_begen <= 0 ? 0 : usersDetail.firebase_begen - 1;
                _db.SaveChanges();
                return false;
            }
        }
        [HttpPost]
        [Route("change/barrier")]
        public bool chance_barrier(int memberId)
        {
            int currentUserRecid = methods.Request_userRecid();
            var query = _db.islemEngel.FirstOrDefault(x => x.engelleyen.Equals(currentUserRecid) & x.engellenen.Equals(memberId));
            // Ekle
            if (query == null)
            {
                Models.Ado.islemEngel table = new Models.Ado.islemEngel();
                table.engellemeTarihi = DateTime.Now;
                table.engelleyen = currentUserRecid;
                table.engellenen = memberId;
                table.gizle = 0;
                table.sil = false;
                _db.islemEngel.Add(table);
                _db.SaveChanges();
                return true;
            }
            else // Çıkar
            {
                _db.islemEngel.Remove(query);
                _db.SaveChanges();
                return false;
            }
        }
        [HttpPost]
        [Route("add/point")]
        public void add_point(int memberId, int point)
        {
            int currentUserRecid = methods.Request_userRecid();
            var query = _db.islemPuan.Where(x => x.puanVeren.Equals(currentUserRecid) & x.puanAlan.Equals(memberId));
            if (query != null)
            {
                foreach (var item in query)
                    _db.islemPuan.Remove(item);
            }
            Models.Ado.islemPuan table = new Models.Ado.islemPuan();
            table.tarih = DateTime.Now;
            table.puanVeren = currentUserRecid;
            table.puanAlan = memberId;
            table.puan = point;
            table.gizle = 0;
            table.sil = false;
            _db.islemPuan.Add(table);
            _db.SaveChanges();
        }
        [HttpPost]
        [Route("add/complaint")]
        public void add_complaint(int memberId, string subject = "", string detail = "")
        {
            int currentUserRecid = methods.Request_userRecid();
            Models.Ado.islemSikayet table = new Models.Ado.islemSikayet();
            table.tarih = DateTime.Now;
            table.sikayetEden = currentUserRecid;
            table.sikateyEttigiKisi = memberId;
            table.sikayetTipi = "Profil";
            table.baslik = subject;
            table.aciklama = string.IsNullOrEmpty(detail) ? subject : detail;
            table.durum = 1;
            _db.islemSikayet.Add(table);
            _db.SaveChanges();
        }
        #endregion
        #region Oturum açan üye ile görüntülenen üye arasındaki olayları yakala. beğenilmiş mi? favori olarak eklenmiş mi? vs
        public class myevents
        {
            public bool like { get; set; }
            public bool favorite { get; set; }
            public int point { get; set; }
            public int harmonyRate { get; set; }
            public bool barrier_my { get; set; }
            public bool barrier_me { get; set; }
            public int status { get; set; }
            public string profileTitle { get; set; }
            public string profileWrite { get; set; }
        }
        [HttpGet]
        [Route("visit/events")]
        public myevents visit_events(int memberId)
        {
            myevents result = new myevents();
            int currentUserRecid = methods.Request_userRecid();
            result.like = _db.islemBegen.FirstOrDefault(x => x.begenen.Equals(currentUserRecid) & x.begenilen.Equals(memberId)) == null ? false : true;
            result.favorite = _db.islemFavori.FirstOrDefault(x => x.ekleyen.Equals(currentUserRecid) & x.eklenen.Equals(memberId)) == null ? false : true;
            var point = _db.islemPuan.FirstOrDefault(x => x.puanVeren.Equals(currentUserRecid) & x.puanAlan.Equals(memberId));
            result.point = point == null ? 0 : point.puan;
            result.harmonyRate = methods.uyumYuzdeHesapla(currentUserRecid, memberId);
            result.barrier_my = _db.islemEngel.FirstOrDefault(x => x.engellenen.Equals(memberId) & x.engelleyen.Equals(currentUserRecid)) == null ? false : true;
            result.barrier_me = _db.islemEngel.FirstOrDefault(x => x.engellenen.Equals(currentUserRecid) & x.engelleyen.Equals(memberId)) == null ? false : true;
            var ouseer = _db.users.First(x => x.recid.Equals(memberId));
            result.status = int.Parse(ouseer.durumRecid.Value.ToString());
            result.profileTitle = ouseer.usersDetail.profilBaslikOnay == 1 ? ouseer.usersDetail.profilBaslik : "";
            result.profileWrite = ouseer.usersDetail.profilYaziOnay == 1 ? ouseer.usersDetail.profilYazi : "";
            if (ouseer.usersDetail.cinsiyet == _db.usersDetail.First(x => x.userRecid.Equals(currentUserRecid)).cinsiyet)
                result.status = 5;
            return result;
        }
        public class yourstatics
        {
            public int totalShare { get; set; }
            public int totalFavorite { get; set; }
            public int totalLike { get; set; }
            public double averagePoint { get; set; }
        }
        public double average(int memberId)
        {
            double memberCount, totalPoint;
            memberCount = _db.islemPuan.Count(x => x.puanAlan.Equals(memberId));
            var y = (from f in _db.islemPuan.Where(x => x.puanAlan.Equals(memberId)) select new { p = f.puan }).ToList();
            if (y.Count == 0)
            {
                return 0;
            }
            else
            {
                totalPoint = y.AsEnumerable().Sum(s => s.p);
                double result = (totalPoint / memberCount);
                return Math.Round(result, 2);
            }
        }
        [HttpGet]
        [Route("statics")]
        public yourstatics statics(int memberId)
        {
            yourstatics result = new yourstatics();
            result.averagePoint = average(memberId);
            result.totalFavorite = _db.islemFavori.Count(x => x.eklenen.Equals(memberId));
            result.totalLike = _db.islemBegen.Count(x => x.begenilen.Equals(memberId));
            result.totalShare = _db.duvarCanli.Count(x => x.userRecid.Equals(memberId));
            return result;
        }
        #endregion
    }
}
