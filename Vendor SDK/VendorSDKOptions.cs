using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Vendor_SDK
{
    public class VendorSDKOptions
    {
        /// <summary>
        /// The URL you want the client to be redirected to once they are authenticated, like a home page.
        /// </summary>
        public string RedirectUrl { get; set; }
        /// <summary>
        /// Your Vendor's VVK. 
        /// </summary>
        public BigInteger PrivateKey { get; set; } // TODO: Make this more secure (no set)
    }
}
