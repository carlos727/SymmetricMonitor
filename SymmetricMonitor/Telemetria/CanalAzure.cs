using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;


namespace Redsis.EVA.Client.Common.Telemetria
{
    public class CanalAzure : ICanal
    {
        private long _maxLatenciaEnMs = 0;
        private long _nroConexiones = 0;
        private long _nroExcepciones = 0;
        private Stopwatch _stopWatch = new Stopwatch();
        private string _customerId;
        private string _sharedKey;
        private string _url;
        private string TimeStampField = "";
        private HttpClient client = new HttpClient();
       

        public long NumeroConexiones
        {
            get
            {
                return _nroConexiones;
            }
        }

        public long NumeroExcepciones
        {
            get
            {
                return _nroExcepciones;
            }
        }

        public long MaxLatenciaEnMs
        {
            get
            {
                return _maxLatenciaEnMs;
            }
        }

        public CanalAzure(string customerId, string shareKey)
        {
            _customerId = customerId;
            _sharedKey = shareKey;
            _url = "https://" + _customerId + ".ods.opinsights.azure.com/api/logs?api-version=2016-04-01";
        }

        private string BuildSignature(string message, string secret)
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

        private void PostData(string signature, string date, string log, string json)
        {
            try
            {

                // Aunque la documentación de .Net indica que HttpClient 
                // debe estar dentro de un "using", realmente no es correcto,
                // de hacero asi no maneja correctamente conexiones TCP (las deja abiertas).
                // Esta clase es thread safe y debería ser un método static.
                // Ver https://aspnetmonsters.com/2016/08/2016-08-27-httpclientwrong/
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("Log-Type", log);
                client.DefaultRequestHeaders.Add("Authorization", signature);
                client.DefaultRequestHeaders.Add("x-ms-date", date);
                client.DefaultRequestHeaders.Add("time-generated-field", TimeStampField);

                HttpContent httpContent = new StringContent(json, Encoding.UTF8);
                httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                //Debug.WriteLine("POST... json: {0}", json);
                _nroConexiones++;
                _stopWatch.Start();
                var response = client.PostAsync(new Uri(_url), httpContent);
                //TODO: Excepciones por respuestas que no estan en el rango 200
                //Debug.WriteLine("httpContent: {0}, respuesta.StatusCode: {1}", httpContent, response.Result.StatusCode);
                _stopWatch.Stop();
                long tiempoRespuesta = _stopWatch.Elapsed.Milliseconds;
                if (_maxLatenciaEnMs < tiempoRespuesta)
                    _maxLatenciaEnMs = tiempoRespuesta;
                //Debug.WriteLine(" listo. response: " + response);
                //Debug.WriteLine(" listo. response.Result.Content: " + response.Result.Content.ReadAsStringAsync());
                //Debug.WriteLine(" listo. response.Result.StatusCode: " + response.Result.StatusCode);
                //Debug.WriteLine(" listo. response.Result.ReasonPhrase: " + response.Result.ReasonPhrase);
            }
            catch (Exception excep)
            {
                // TODO: manejar excepciones por fallas en la conexión, timeout, etc.
                _nroExcepciones++;
                //Debug.WriteLine("API Post Exception: " + excep.Message);
                Telemetria.Instancia.Id("symException").AgregaMetrica(new Excepcion(excep));
            }
        }

        public void Enviar(string log, string json)
        {
            // Create a hash for the API signature
            var datestring = DateTime.UtcNow.ToString("r");
            string stringToHash = "POST\n" 
                + json.Length 
                + "\napplication/json\n" 
                + "x-ms-date:" 
                + datestring 
                + "\n/api/logs";
            string hashedString = BuildSignature(stringToHash, _sharedKey);
            string signature = "SharedKey " + _customerId + ":" + hashedString;

            PostData(signature, datestring, log, json);
        }
    }
    
}
