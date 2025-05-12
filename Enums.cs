using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace app_api.Classes
{
    public class Enums
    {
        public enum ResultCode
        {
            Token_Talebinde_Scope_Parametresi_Bulunamadı = 0,
            Ceptelefonu_Onaysiz = 1,
            Email_Onaysiz = 2,
            Kullanici_var_Sifre_Hatali = 3,
            DetayEksik = 4
        }
        public enum SiteValue
        {
            Elitislamievlilik_com = 1,
            astromend_com = 2
        }
        public enum userStatus
        {
            aktif = 1,
            hesapDondurulmus = 3,
            hesapSilinmis = 4,
            hesapEngellenmis = 5
        }
        public enum NotificationTypes
        {
            Mesaj = 1,
            Favori = 2,
            Begeni = 3,
            Gezen = 4
        }
    }
}