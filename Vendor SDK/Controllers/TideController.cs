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
        /// To test the decentralized decryption flow during sign up.
        /// </summary>
        /// <param name="encryptedByGCVK"></param>
        /// <param name="encryptedByGVVK"></param>
        /// <param name="jwt"></param>
        /// <returns></returns>
        public async Task<IActionResult> DecryptionTest(string encryptedByGCVK, string encryptedByGVVK, string jwt, string cvkOrkUrl)
        {
            // TODO: make all this into a proper flow

            TideJWT tideJWT = new TideJWT(jwt);
            // Get orks of this jwt's uid
            var orkInfoTask = new NodeClient(cvkOrkUrl).GetKeyOrks(tideJWT.payload.uid, true);
            //-------------------------------------- Sim Request Start

            // Doing here to save time
            // Get first 32 bytes of encryptedByGCVK (c1) and bytes after 32th index (c2)
            byte[] c1 = Convert.FromBase64String(encryptedByGCVK).Take(32).ToArray();
            byte[] c2 = Convert.FromBase64String(encryptedByGCVK).Skip(32).ToArray();

            // Encrypt data to send => c1 and JWT
            var secret = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(new TestDecrypt
            {
                c1 = c1,
                jwt = jwt
            }));

            //-------------------------------------- Sim Request End
            var orkInfo = await orkInfoTask; 
            NodeClient[] clients = orkInfo.orkUrls.Select(url => new NodeClient(url)).ToArray();

            byte[][] aesKeys = new byte[clients.Length][];
            for (int i = 0; i < clients.Length; i++)
            {
                aesKeys[i] = RandomNumberGenerator.GetBytes(32);
            }

            var encryptedKeysi = orkInfo.orkPubs.Select((pub, i) => {
                (byte[] c1, byte[] c2) = ElGamal.Encrypt(aesKeys[i], Point.FromBase64(pub));
                return Convert.ToBase64String(c1.Concat(c2).ToArray());
            }).ToArray();

            var encryptedDatai = aesKeys.Select(key => AES.Encrypt(secret, key)).ToArray();
    
            var tasks = clients.Select((client, i) => client.TestDecrypt(encryptedDatai[i], encryptedKeysi[i]));
            //-------------------------------------- Node Test Decrypt Request Start

            // Determine Lis while waiting for node response
            var ids = orkInfo.orkIds.Select(id => BigInteger.Parse(id));
            BigInteger[] lis = ids.Select(id => SecretSharing.EvalLi(id, ids, Curve.N)).ToArray();

            // Decrypt secret encryptedByGVVK
            byte[] decrypted_byVendor = ElGamal.Decrypt(encryptedByGVVK, _options.Value.PrivateKey);

            //-------------------------------------- Node Test Decrypt Request End
            string[] encrypted_responses = await Task.WhenAll(tasks);

            byte[][] applied_c1s = encrypted_responses.Select((e, i) => (AES.Decrypt(e, aesKeys[i]))).ToArray();

            // Decrypt original c1 encrypted by cvk from user
            byte[] decrypted_byNode = ElGamal.DecentralizedDecrypt(applied_c1s, c2, lis);

            // Compare decrypted secrets
            if (!(decrypted_byNode.SequenceEqual(decrypted_byVendor))) return Ok("Test Failed");
            return Ok("Test Passed");
        }
    }
}