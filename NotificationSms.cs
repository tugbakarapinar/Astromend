using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Models.User
{
    public class NotificationSms
    {
        public int recid { get; set; }
        public int saatAralikTip { get; set; }
        public bool online { get; set; }
        public bool gezen { get; set; }
        public bool mesaj { get; set; }
        public bool durum { get; set; }
    }
}