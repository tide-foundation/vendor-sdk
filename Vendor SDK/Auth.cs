using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TinySDK.Ed25519;
using TinySDK.Math;
using TinySDK.Models;
using TinySDK.Tools;
using Vendor_SDK.Models;

namespace Vendor_SDK
{
    public class Auth
    {
        public static bool VerifyJwt(string jwt, string tidePublicKey)
        {
            // Verify jwt and it's timestamp
            var JWT = new TideJWT(jwt, true); 
            var pub = Point.FromBase64(tidePublicKey);
            if (!EdDSA.Verify(JWT.GetDataToSign(), JWT.signature, pub)) return false;
            long epochNow_seconds = Utils.GetEpochSeconds();
            if (JWT.payload.exp <= epochNow_seconds) return false;
            return true;
        }
    }
}
