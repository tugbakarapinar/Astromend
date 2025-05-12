using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace app_api.Classes
{
    public class DataCompile
    {
        Models.Ado.Entities _db = new Models.Ado.Entities();
        public Models.User.User DCUser(Models.Ado.users data)
        {
            Models.User.User user = new Models.User.User()
            {
                recid = data.recid,
                profileImages = data.usersDetail.islemResimler.resim,
                cepTelefonu = data.cepTelefonu,
                cepTelefonuOnay = data.cepTelefonuOnay,
                durumRecid = data.durumRecid.Value,
                email = data.email,
                emailOnay = data.emailOnay,
                rumuz = data.rumuz,
                cinsiyet = data.usersDetail.cinsiyet,               
                sifre = data.sifre,
                dil = data.dil,
                uyelikTarihi = data.uyelikTarihi.ToString(),
                yas = DateTime.Now.Year - data.usersDetail.dogumTarihi.Value.Year,
                mesajlasmaHakki = _db.goldElit.FirstOrDefault(x => x.userRecid.Equals(data.recid) & x.bitisTarih > DateTime.Now) != null ? true : false,
                notifications = data.usersDetail.virtualBildirim == null ? 0 : int.Parse(data.usersDetail.virtualBildirim.ToString()),
                sehir = data.usersDetail.city.ilAdi,
                profileImagesRecid = data.usersDetail.profileImagesID.Value,
                ayarMesajYasAraligi = data.usersDetail.ayarMesajYasAraligi
            };
            return user;
        }
        public Models.User.UserDetail DCUserDetail(Models.Ado.usersDetail data)
        {
            Models.User.UserDetail userDetail = new Models.User.UserDetail();
            userDetail.detayDurum = data.detayDurum.Value;
            userDetail.userRecid = data.userRecid;
            //userDetail.dogumTarihi = data.dogumTarihi.Value.ToString("yyyy");
            userDetail.dogumTarihi = data.dogumTarihi.Value.ToString("yyyy-MM-dd");
            userDetail.meslek = data.meslek.Value;
            userDetail.profilBaslik = data.profilBaslik;
            userDetail.profilBaslikOnay = data.profilBaslikOnay;
            userDetail.profilYazi = data.profilYazi;
            userDetail.profilYaziOnay = data.profilYaziOnay;
            userDetail.tempPassword = data.users.sifre;
            userDetail.tempUserName = data.users.rumuz;
            userDetail.cinsiyet = data.cinsiyet;
            userDetail.boy = data.boy ?? 0;
            userDetail.kilo = data.kilo ?? 0;
            userDetail.gozRengi = data.gozRengi ?? 0;
            userDetail.amac = data.amac ?? 0;
            userDetail.ilRecid = data.ilRecid;
            userDetail.ulkeRecid = data.ulkeRecid.Value;
            userDetail.ulke = data.ulke;
            userDetail.sehir = data.sehir;
            userDetail.googleLat = data.googleLat;
            userDetail.googleLon = data.googleLon;
            userDetail.googleTzone = data.googleTzone;
            return userDetail;
        }
    }
}