using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Redsis.EVA.Client.Common.LogRemoto
{
    public class LogAzure
    {

        // An example JSON object, with key/value pairs
        static string json = @"[{""IdLocalidad"":""1010"",""IdPOS"":""1010-01""},{""IdUsuario"":""jtorres"",""Op"":""ingreso""}]";

        // Update customerId to your Operations Management Suite workspace ID
        static string customerId = "5ad3b611-4284-49f4-927d-5cafee30bf4b";

        // For sharedKey, use either the primary or the secondary Connected Sources client authentication key   
        static string sharedKey = "Yv2g4l1xIE3kKNZrPafBvX7FVxDOSPksTX8bS2uB30+wju5wHjjjOXKOSQJ/iNFQleCye6Ufe+zVPbNxiSO+KA==";



        // LogName is name of the event type that is being submitted to Log Analytics
        static string LogName = "PosAutenticacion";

        // You can use an optional field to specify the timestamp from the data. If the time field is not specified, Log Analytics assumes the time is the message ingestion time
        static string TimeStampField = "";
  
        public static void Ejecutar()
        {
            // Create a hash for the API signature
            var datestring = DateTime.UtcNow.ToString("r");
            string stringToHash = "POST\n" + json.Length + "\napplication/json\n" + "x-ms-date:" + datestring + "\n/api/logs";
            string hashedString = BuildSignature(stringToHash, sharedKey);
            string signature = "SharedKey " + customerId + ":" + hashedString;

            PostData(signature, datestring, json);
        }

        // Build the API signature
        public static string BuildSignature(string message, string secret)
        {
            var encoding = new System.Text.ASCIIEncoding();
            byte[] keyByte = Convert.FromBase64String(secret);
            byte[] messageBytes = encoding.GetBytes(message);
            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                byte[] hash = hmacsha256.ComputeHash(messageBytes);
                return Convert.ToBase64String(hash);
            }
        }

        // Send a request to the POST API endpoint
        public static void PostData(string signature, string date, string json)
        {
            try
            {
                string url = "https://" + customerId + ".ods.opinsights.azure.com/api/logs?api-version=2016-04-01";

                // Aunque la documentación de .Net indica que HttpClient debe estar dentro de un "using",
                // realmente no es correcto. Esta clase es thread safe y debería ser un método static.
                // Ver https://aspnetmonsters.com/2016/08/2016-08-27-httpclientwrong/
                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("Log-Type", LogName);
                client.DefaultRequestHeaders.Add("Authorization", signature);
                client.DefaultRequestHeaders.Add("x-ms-date", date);
                client.DefaultRequestHeaders.Add("time-generated-field", TimeStampField);


                HttpContent httpContent = new StringContent(json, Encoding.UTF8);
                httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                Console.Write("Haciendo POST...");
                //Task<HttpResponseMessage> response = client.PostAsync(new Uri(url), httpContent);
                var response = client.PostAsync(new Uri(url), httpContent);
                Console.WriteLine(" listo. response: " + response);
                Console.WriteLine(" listo. response.Result.Content: " + response.Result.Content.ReadAsStringAsync());
                Console.WriteLine(" listo. response.Result.StatusCode: " + response.Result.StatusCode);
                Console.WriteLine(" listo. response.Result.ReasonPhrase: " + response.Result.ReasonPhrase);

                //HttpContent responseContent = response.Result.Content;
                //Console.WriteLine("Return responseContent: " + responseContent.ReadAsStringAsync().Status);
                //string result = responseContent.ReadAsStringAsync().Result.;
                //Console.WriteLine("Return Result: " + result);
            }
            catch (Exception excep)
            {
                Console.WriteLine("API Post Exception: " + excep.Message);
            }
        }
    }
}
