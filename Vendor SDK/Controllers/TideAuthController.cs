using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;
using TinySDK.Ed25519;
using TinySDK.Math;
using TinySDK.Models;
using Vendor_SDK.Models;

namespace Vendor_SDK.Controllers
{
    public class TideAuthController : Controller
    {
        private readonly IOptions<VendorSDKOptions> _options;
        private static readonly HttpClient _httpClient = new HttpClient();
        public TideAuthController(IOptions<VendorSDKOptions> options)
        {
            _options = options;
        }
        public async Task<IActionResult> Index([FromQuery] string auth_token)
        {
            // Verify AuthToken and create session here
            var jwt = new TideJWT(auth_token, true);
            var resp = await _httpClient.GetAsync("http://host.docker.internal:2000/keyentry/" + jwt.payload.uid).Result.Content.ReadAsStringAsync(); // CHANGE SIM URL HERE LATER
            var user = JsonSerializer.Deserialize<User>(resp, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true // simulator is literally returining a field names "public" .NET does not allow "public" as field name
            });
            var pub_p = Point.FromBase64(user.Public);
            if (!EdDSA.Verify(jwt.GetDataToSign(), jwt.signature, pub_p))
            {
                return Unauthorized("JWT signature invalid");
            }
            long epochNow_seconds = (long)(DateTimeOffset.UtcNow - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)).TotalSeconds;
            if (jwt.payload.exp <= epochNow_seconds) return Unauthorized("JWT expired");
            // then create session here
            HttpContext.Session.SetString("user", jwt.payload.uid);
            return Redirect(_options.Value.RedirectUrl); // only if everything is good
        }
    }
}