using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Models.User
{
    public class Member
    {
        public int recid { get; set; }
        public string profileImages { get; set; }
        public string[] photos { get; set; }
        public string rumuz { get; set; }
        public int yas { get; set; }
        public string sehir { get; set; }
        public string meslek { get; set; }
        public string virtualOnTime { get; set; }
        public byte virtualOnline { get; set; }
        public bool star { get; set; }
        public bool aktif { get; set; }
        public string processDate { get; set; }
        public int boy { get; set; }
        public int kilo { get; set; }
        public int gozRengi { get; set; }
        public int amac { get; set; }
    }
}