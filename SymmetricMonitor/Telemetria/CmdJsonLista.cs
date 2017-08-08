using Newtonsoft.Json;
using System.Collections;

namespace Redsis.EVA.Client.Common.Telemetria
{
    public class CmdJsonLista : ICmd
    {
        private ICanal _canal;
        private string _nombreLog;
        private IList _metricas;

        public CmdJsonLista(ICanal canal, string nombreLog)
        {
            _canal = canal;
            _nombreLog = nombreLog;
        }

        public CmdJsonLista(ICanal canal, string nombreLog, IList metricas)
        {
            _canal = canal;
            _nombreLog = nombreLog;
            _metricas = metricas;
        }

        public void AgregarLista(IList metricas)
        {
            _metricas = metricas;
        }

        public string ConvertirJson(object dato)
        {
            return JsonConvert.SerializeObject(dato);
        }

        public string Procesar()
        {
            foreach (Metrica m in _metricas)
            {
                m.CompletaObjetoLog();
            }
            string json = ConvertirJson(_metricas);
            _canal.Enviar(_nombreLog, json);
            return json;
        }
    }
}
