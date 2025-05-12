using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Models.User
{
    public class UserDetail
    {
        public int userRecid { get; set; }
        public string cinsiyet { get; set; }
        public int boy { get; set; }
        public int kilo { get; set; }
        public int gozRengi { get; set; }
        public int amac { get; set; }
        public int ulkeRecid { get; set; }
        public bool detayDurum { get; set; }
        public int ilRecid { get; set; }
        public string dogumTarihi { get; set; }
        public int meslek { get; set; }       
        public string profilBaslik { get; set; }
        public int profilBaslikOnay { get; set; }
        public string profilYazi { get; set; }
        public int profilYaziOnay { get; set; }
        public string tempUserName { get; set; }
        public string tempPassword { get; set; }
        public string ulke { get; set; }
        public string sehir { get; set; }
        public string googleLat { get; set; }
        public string googleLon { get; set; }
        public string googleTzone { get; set; }
    }
}