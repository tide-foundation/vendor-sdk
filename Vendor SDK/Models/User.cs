using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vendor_SDK.Models
{
    public class User
    {
        public string id { get; set; }
        public string entry_S { get; set; }
        public string entry_R2 { get; set; }
        public long timestamp { get; set; }
        public string Public { get; set; } // change this!
        public string orks { get; set; }
    }
}
