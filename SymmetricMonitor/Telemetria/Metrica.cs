using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Redsis.EVA.Client.Common.Telemetria
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class Metrica
    {
        [JsonProperty]
        public string TimeStamp { get; set; }
        [JsonProperty]
        public string Programa { get; set;  }
        [JsonProperty]
        public string Version { get; set; } = "1.0";
        [JsonProperty]
        public string Nivel { get; set; } = "inf";
        [JsonProperty]
        public string Empresa { get; set; }
        [JsonProperty]
        public string Id { get; set; }
        [JsonProperty]
        public string Usuario { get; set; }
        [JsonProperty]
        public string Operacion { get; set; } = "";
        [JsonProperty]
        internal IDictionary<string, object> Props { get; private set; } = new Dictionary<string, object>();

        public Metrica() {}
        
        public Metrica(string nombre)
        {
            Operacion = nombre;
        }
        public void AgregarPropiedad(string nombre, object valor)
        {
            if (!(valor.GetType().IsPrimitive 
                || valor.GetType() == typeof(Decimal) 
                || valor.GetType() == typeof(String) 
                || valor.GetType() == typeof(DateTime)
                || valor.GetType() == typeof(Guid)
                || valor == null))
            {
                throw new ArgumentException(
                    String.Format("Tipo '{0}' de parámetro 'valor' no soportado.", valor.GetType().ToString()));
            }
            Props.Add(nombre, valor);
        }

        /// <summary>
        /// Invocado justo al agregar la Metrica al proceso de Telemetria, para 
        /// ejecutar procesos de preparación de la métrica.
        /// </summary>
        internal virtual void Preparar() {}

        /// <summary>
        /// Inicializa el objeto de log al término de la cola de 
        /// métricas. Ejecuta los últimos cálculos que requiera la métrica.
        /// </summary>
        public abstract void CompletaObjetoLog();
    }
}
