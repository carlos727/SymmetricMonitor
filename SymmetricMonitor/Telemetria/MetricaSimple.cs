using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Redsis.EVA.Client.Common.Telemetria
{
    [JsonObject(MemberSerialization.OptIn)]
    public class MetricaSimple : Metrica
    {
        [JsonProperty]
        public double Valor { get; set; }

        public MetricaSimple(string nombre)
        {
            Operacion = nombre;
            Valor = 0;
        }
        public MetricaSimple(string nombre, double valor)
        {
            Operacion = nombre;
            Valor = valor;
        }

        public new MetricaSimple AgregarPropiedad(string nombre, object valor)
        {
            base.AgregarPropiedad(nombre, valor);
            return this;
        }

        public override void CompletaObjetoLog() {}
    }
}
