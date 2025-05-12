using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Models.User
{
    public class User
    {
        public int recid { get; set; }
        public string profileImages { get; set; }
        public int profileImagesRecid { get; set; }
        public string rumuz { get; set; }
        public string cinsiyet { get; set; }
        public string email { get; set; }
        public bool emailOnay { get; set; }
        public string cepTelefonu { get; set; }
        public bool cepTelefonuOnay { get; set; }
        public string sifre { get; set; }
        public int durumRecid { get; set; }
        public string uyelikTarihi { get; set; }
        public byte kulTip { get; set; }
        public int notifications { get; set; }
        public string dil { get; set; }
        public int yas { get; set; }
        public string sehir { get; set; }
        public bool mesajlasmaHakki { get; set; }
        public string ayarMesajYasAraligi { get; set; }     
    }
}