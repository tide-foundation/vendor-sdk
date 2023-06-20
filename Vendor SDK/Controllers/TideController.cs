using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TinySDK.Clients;
using TinySDK.Ed25519;
using TinySDK.Math;
using TinySDK.Models;
using TinySDK.Tools;
using Vendor_SDK.Models;

namespace Vendor_SDK.Controllers
{
    public class TideController : Controller
    {
        private static string SimURL = "http://host.docker.internal:2000/keyentry/";
        private readonly IOptions<VendorSDKOptions> _options;
        private static readonly HttpClient _httpClient = new HttpClient();
        public TideController(IOptions<VendorSDKOptions> options)
        {
            _options = options;
        }
        /// <summary>
        /// The endpoint to authenticate a Tide JWT and redirect the client to a vendor specified URL.
        /// </summary>
        /// <param name="auth_token"></param>
        /// <returns></returns>
        public async Task<IActionResult> Auth([FromQuery] string auth_token)
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
        /// <summary>
        /// To test the decentralized decryption flow during sign up.
        /// </summary>
        /// <param name="encryptedByGCVK"></param>
        /// <param name="encryptedByGVVK"></param>
        /// <param name="jwt"></param>
        /// <returns></returns>
        public async Task<IActionResult> DecryptionTest(string encryptedByGCVK, string encryptedByGVVK, string jwt_p)
        {
            TideJWT jwt = new TideJWT(jwt_p);
            // Get orks of this jwt's uid
            var orkInfoTask = new SimulatorClient(SimURL).GetOrkUrls(jwt.payload.uid);
            OrkInfo orkInfo = await 
            NodeClient[] clients = orkInfo.orkUrls.Select(url => new NodeClient(url)).ToArray();

            // Get first 32 bytes of encryptedByGCVK (c1) and bytes after 32th index (c2)
            byte[] c1 = Convert.FromBase64String(encryptedByGCVK).Take(32).ToArray();
            byte[] c2 = Convert.FromBase64String(encryptedByGCVK).Skip(32).ToArray();

            // Encrypt data to send => c1 and JWT
            var secret = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(new TestDecrypt
            {
                c1 = c1,
                jwt = jwt_p
            }));

            byte[][] aesKeys = new byte[clients.Length][];
            for(int i = 0; i < clients.Length; i++)
            {
                aesKeys[i] = RandomNumberGenerator.GetBytes(32);
            }

            var encryptedKeysi = orkInfo.orkPubs.Select((pub, i) => {
                (byte[] c1, byte[] c2) = ElGamal.Encrypt(aesKeys[i], Point.FromBase64(pub));
                return Convert.ToBase64String(c1.Concat(c2).ToArray());
            }).ToArray();

            var encryptedDatai = aesKeys.Select(key => AES.Encrypt(secret, key)).ToArray();
    
            var tasks = clients.Select((client, i) => client.TestDecrypt(encryptedDatai[i], encryptedKeysi[i]));

            // Determine Lis while waiting for node response
            var ids = orkInfo.orkIds.Select(id => BigInteger.Parse(id));
            BigInteger[] lis = ids.Select(id => SecretSharing.EvalLi(id, ids, Curve.N)).ToArray();

            string[] encrypted_responses = await Task.WhenAll(tasks);

            byte[][] applied_c1s = encrypted_responses.Select((e, i) => Convert.FromBase64String(AES.Decrypt(e, aesKeys[i]))).ToArray();

            // Decrypt original c1 encrypted by cvk from user
            byte[] decrypted = ElGamal.DecentralizedDecrypt(applied_c1s, c2, lis);

            // Decrypt secret encryptedByGVVK

            // Compare secrets

        }
    }
}