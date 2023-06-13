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
            var user = JsonSerializer.Deserialize<User>(await _httpClient.GetAsync("http://host.docker.internal:2000/" + jwt.payload.uid).Result.Content.ReadAsStringAsync());
            var pub_p = Point.FromBase64(user.GCVK);
            if (!EdDSA.Verify(jwt.GetDataToSign(), jwt.signature, pub_p)) return Unauthorized();
            // then create session here
            return Redirect(_options.Value.RedirectUrl); // only if everything is good
        }
    }
}