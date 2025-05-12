using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using PagedList;

namespace Controllers
{
    [Authorize]
    [RoutePrefix("app/api/wallshare")]
    public class WallShareController : ApiController
    {
        Models.Ado.Entities _db = new Models.Ado.Entities();
        app_api.Classes.Methods methods = new app_api.Classes.Methods();
        public class wall
        {
            public int recid { get; set; }
            public string memberImage { get; set; }
            public string date { get; set; }
            public int totalLike { get; set; }
            public string status { get; set; }
            public int userRecid { get; set; }
            public string nickName { get; set; }
            public string text { get; set; }
            public Models.User.Member member { get; set; }
            public int tip { get; set; }
        }
        [HttpGet]
        [Route("slider")]
        public List<wall> slider()
        {
            List<wall> walls = new List<wall>();
            int currentUserRecid = methods.Request_userRecid();
            var currentUser = _db.users.First(x => x.recid.Equals(currentUserRecid));
            var query = _db.duvarCanli.Where(x => x.durum.Equals(1) & x.usersDetail.users.durumRecid.Value == 1 & x.usersDetail.cinsiyet != currentUser.usersDetail.cinsiyet).OrderByDescending(x => x.tarih)
                                .Where(e => !_db.islemEngel.Where(l => l.engelleyen == currentUserRecid & l.engellenen == e.userRecid).Any())
                                .Where(e => !_db.islemEngel.Where(l => l.engelleyen == e.userRecid & l.engellenen == currentUserRecid).Any()).Take(15).ToList();
            foreach (var item in query)
            {
                wall wall = new wall()
                {
                    date = methods.gecenZaman(item.tarih),
                    memberImage = item.usersDetail.islemResimler.resim,
                    nickName = item.usersDetail.users.rumuz,
                    recid = item.recid,
                    totalLike = item.begen.Value,
                    userRecid = item.userRecid,
                    tip = item.tip
                };
                #region Kullanıcı objesini oluştur
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
                List<string> list = new List<string>();
                foreach (var item_image in _db.islemResimler.Where(x => x.kullanici.Equals(item.userRecid) & x.onay.Equals(1) & !x.resim.Contains("male")))
                    list.Add(item_image.resim);
                member.photos = list.ToArray();
                wall.member = member;
                #endregion
                switch (item.tip)
                {
                    case 2:
                        wall.text = "Yeni fotoğraf ekledi";
                        break;
                    case 3:
                        wall.text = "Profil resmini güncelledi";
                        break;
                    default:
                        wall.text = item.detay;
                        break;
                }
                walls.Add(wall);
            }
            return walls;
        }
        [HttpGet]
        [Route("list")]
        public List<wall> list(int page)
        {
            List<wall> walls = new List<wall>();
            int currentUserRecid = methods.Request_userRecid();
            var currentUser = _db.users.First(x => x.recid.Equals(currentUserRecid));
            var query = _db.duvarCanli.Where(x => x.durum.Equals(1) & x.usersDetail.users.durumRecid.Value == 1 & x.usersDetail.cinsiyet != currentUser.usersDetail.cinsiyet).OrderByDescending(x => x.tarih)
                                .Where(e => !_db.islemEngel.Where(l => l.engelleyen == currentUserRecid & l.engellenen == e.userRecid).Any())
                                .Where(e => !_db.islemEngel.Where(l => l.engelleyen == e.userRecid & l.engellenen == currentUserRecid).Any()).ToPagedList(page, 20);
            foreach (var item in query)
            {
                wall wall = new wall()
                {
                    date = methods.gecenZaman(item.tarih),
                    memberImage = item.usersDetail.islemResimler.resim,
                    nickName = item.usersDetail.users.rumuz,
                    recid = item.recid,
                    totalLike = item.begen.Value,
                    userRecid = item.userRecid
                };
                #region Kullanıcı objesini oluştur
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
                List<string> list = new List<string>();
                foreach (var item_image in _db.islemResimler.Where(x => x.kullanici.Equals(item.userRecid) & x.onay.Equals(1) & !x.resim.Contains("male")))
                    list.Add(item_image.resim);
                member.photos = list.ToArray();
                wall.member = member;
                #endregion
                switch (item.tip)
                {
                    case 2:
                        wall.text = "Yeni fotoğraf ekledi";
                        break;
                    case 3:
                        wall.text = "Profil resmini güncelledi";
                        break;
                    default:
                        wall.text = item.detay;
                        break;
                }
                walls.Add(wall);
            }
            return walls;
        }
        [HttpGet]
        [Route("member")]
        public List<wall> member(int page, int memberId)
        {
            List<wall> walls = new List<wall>();
            int currentUserRecid = methods.Request_userRecid();
            var query = _db.duvarCanli.Where(x => x.durum.Equals(1) & x.userRecid.Equals(memberId))                                
                                .Where(e => !_db.islemEngel.Where(l => l.engelleyen == currentUserRecid & l.engellenen == e.userRecid).Any())
                                .Where(e => !_db.islemEngel.Where(l => l.engelleyen == e.userRecid & l.engellenen == currentUserRecid).Any())
                                .OrderByDescending(x => x.tarih)
                                .ToPagedList(page, 20);
            foreach (var item in query)
            {
                wall wall = new wall()
                {
                    date = methods.gecenZaman(item.tarih),
                    memberImage = item.usersDetail.islemResimler.resim,
                    nickName = item.usersDetail.users.rumuz,
                    recid = item.recid,
                    totalLike = item.begen.Value,
                    userRecid = item.userRecid
                };
                #region Kullanıcı objesini oluştur
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
                List<string> list = new List<string>();
                foreach (var item_image in _db.islemResimler.Where(x => x.kullanici.Equals(item.userRecid) & x.onay.Equals(1) & !x.resim.Contains("male")))
                    list.Add(item_image.resim);
                member.photos = list.ToArray();
                wall.member = member;
                #endregion
                switch (item.tip)
                {
                    case 2:
                        wall.text = "Yeni fotoğraf ekledi";
                        break;
                    case 3:
                        wall.text = "Profil resmini güncelledi";
                        break;
                    default:
                        wall.text = item.detay;
                        break;
                }
                walls.Add(wall);
            }
            return walls;
        }
        [HttpGet]
        [Route("my")]
        public List<wall> my(int page)
        {
            List<wall> walls = new List<wall>();
            int currentUserRecid = methods.Request_userRecid();
            var currentUser = _db.users.First(x => x.recid.Equals(currentUserRecid));
            var query = _db.duvarCanli.Where(x => x.userRecid.Equals(currentUserRecid)).OrderByDescending(x => x.tarih)
                                .ToPagedList(page, 20);
            foreach (var item in query)
            {
                wall wall = new wall()
                {
                    date = methods.gecenZaman(item.tarih),
                    memberImage = item.usersDetail.islemResimler.resim,
                    nickName = item.usersDetail.users.rumuz,
                    recid = item.recid,
                    totalLike = item.begen.Value,
                    userRecid = item.userRecid
                };
                if (item.durum == 0)
                    wall.status = "Onay bekliyor";
                else if (item.durum == 2)
                    wall.status = "Reddedildi";
                switch (item.tip)
                {
                    case 2:
                        wall.text = "Yeni fotoğraf ekledi";
                        break;
                    case 3:
                        wall.text = "Profil resmini güncelledi";
                        break;
                    default:
                        wall.text = item.detay;
                        break;
                }
                walls.Add(wall);
            }
            return walls;
        }
        [HttpPost]
        [Route("like")]
        public int like(int recid)
        {
            int currentUserRecid = methods.Request_userRecid();
            var currentUser = _db.users.First(x => x.recid.Equals(currentUserRecid));
            var query = _db.duvarCanliBegenenler.FirstOrDefault(x => x.duvarId.Equals(recid) & x.begenenId.Equals(currentUserRecid));
            Models.Ado.duvarCanli oDuvar = _db.duvarCanli.First(x => x.recid == recid);
            // Beğeni yap
            if (query == null)
            {
                Models.Ado.duvarCanliBegenenler tableList = new Models.Ado.duvarCanliBegenenler();
                tableList.duvarId = recid;
                tableList.tarih = DateTime.Now;
                tableList.begenenId = currentUserRecid;
                tableList.paylasanId = oDuvar.userRecid;
                _db.duvarCanliBegenenler.Add(tableList);
                Models.Ado.usersNotifications tableNotifi = new Models.Ado.usersNotifications();
                tableNotifi.baslik = "Paylaşımınız Beğenildi!";
                tableNotifi.detay = "<a href=/Duvar/Begenenler/" + recid + "><b>" + currentUser.rumuz + "</b> Paylaşımınızı Beğendi, Paylaşımı görmek için <b> Tıklayın</b></a>";
                tableNotifi.okundu = false;
                tableNotifi.rumuz = currentUser.rumuz;
                tableNotifi.tip = recid;
                tableNotifi.tarih = DateTime.Now;
                tableNotifi.userRecid = oDuvar.userRecid;
                _db.usersNotifications.Add(tableNotifi);
                oDuvar.begen = oDuvar.begen + 1;
                _db.SaveChanges();
            }
            // Beğeniyi geri al
            else
            {
                Models.Ado.duvarCanliBegenenler tableDelete = _db.duvarCanliBegenenler.First(x => x.duvarId == recid & x.begenenId == currentUserRecid);
                _db.duvarCanliBegenenler.Remove(tableDelete);
                Models.Ado.usersNotifications tableNotifiDelete = _db.usersNotifications.First(x => x.tip == recid);
                _db.usersNotifications.Remove(tableNotifiDelete);
                oDuvar.begen = oDuvar.begen - 1;
                _db.SaveChanges();
            }
            return oDuvar.begen.Value;
        }
        public class sharerequest
        {
            public string text { get; set; }
        }
        [HttpPost]
        [Route("share")]
        public void share(sharerequest request)
        {
            int currentUserRecid = methods.Request_userRecid();
            Models.Ado.duvarCanli duvarCanli = new Models.Ado.duvarCanli();
            duvarCanli.begen = 0;
            duvarCanli.detay = request.text.Trim();
            duvarCanli.durum = 0;
            duvarCanli.routeBaslik = methods.turkishConvertURL(request.text);
            duvarCanli.tip = 1;
            duvarCanli.tarih = DateTime.Now;
            duvarCanli.userRecid = currentUserRecid;
            _db.duvarCanli.Add(duvarCanli);
            _db.SaveChanges();
        }
    }
}
