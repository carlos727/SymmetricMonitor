using System;
using System.Collections.Generic;
using System.Linq;
using LinqStatistics;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Redsis.EVA.Client.Common.Telemetria
{
    [JsonObject(MemberSerialization.OptIn)]
    public class MetricaAgregada : Metrica
    {
        public readonly string[] NOMBRES_RESERVADOS = { "CantidadItems", "SumaItems", "Maximo", "Minimo", "Promedio", "Mediana", "Moda", "Varianza", "DesvStd" };
        private List<double> valores = new List<double>();

        [JsonProperty]
        public int CantidadItems { get; private set; }
        [JsonProperty]
        public double SumaItems { get; private set; }
        [JsonProperty]
        public double Maximo { get; private set; }
        [JsonProperty]
        public double Minimo { get; private set; }
        [JsonProperty]
        public double Promedio { get; private set; }
        [JsonProperty]
        public double Mediana { get; private set; }
        [JsonProperty]
        public double Moda { get; private set; }
        [JsonProperty]
        public double Varianza { get; private set; }
        [JsonProperty]
        public double DesvStd { get; private set; }

        public MetricaAgregada(string nombre)
        {
            Operacion = nombre;
        }

        public MetricaAgregada AgregarValor(double d)
        {
            valores.Add(d);
            return this;
        }

        public new MetricaAgregada AgregarPropiedad(string nombre, object valor)
        {
            if (Array.IndexOf(NOMBRES_RESERVADOS, nombre) > -1)
            {
                throw new ArgumentException(
                    String.Format("Nombre '{0}' esta reservado, use otro.", nombre));
            }
            base.AgregarPropiedad(nombre, valor);
            return this;
        }

        public override void CompletaObjetoLog()
        {
            if (valores == null || valores.Count == 0)
                return;
            CantidadItems = valores.Count;
            SumaItems = valores.Sum();
            Minimo = valores.Min();
            Maximo = valores.Max();
            Promedio = valores.Average();
            Mediana = valores.Median();
            Moda = (double)valores.Mode();
            Varianza = valores.Variance();
            DesvStd = valores.StandardDeviation();
        }
    }
}
