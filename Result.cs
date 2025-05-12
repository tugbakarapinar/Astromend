using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace app_api.Classes
{
    public class Result
    {
        public bool success { get; set; }
        public string message { get; set; }
        public int code { get; set; }
        public int value { get; set; }
        public string returnText { get; set; }
    }
    public class MReturn
    {
        public object data { get; set; }
        public Result result { get; set; }
        public int key { get; set; }
    }
}