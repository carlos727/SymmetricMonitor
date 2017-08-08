using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Redsis.EVA.Client.Common.Telemetria
{
    public class CmdJson : ICmd
    {
        private ICanal _canal;
        private string _nombreLog;
        private Metrica _metrica;
        
        public CmdJson(ICanal canal, string nombreLog, Metrica metrica)
        {
            _canal = canal;
            _nombreLog = nombreLog;
            _metrica = metrica;
        }

        public string ConvertirJson(object dato)
        {
            return JsonConvert.SerializeObject(dato);
        }

        public string Procesar()
        {
            _metrica.CompletaObjetoLog();
            string json = ConvertirJson(_metrica);
            _canal.Enviar(_nombreLog, json);
            return json;
        }
    }
}
