using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Redsis.EVA.Client.Common.Telemetria
{
    [JsonObject(MemberSerialization.OptIn)]
    public class MetricaTemporizador : MetricaSimple
    {
        private Stopwatch reloj = new Stopwatch();

        public MetricaTemporizador(string nombre) : base(nombre)
        {
            Inicia();
        }
        
        public MetricaTemporizador Inicia()
        {
            if (!reloj.IsRunning)
                reloj.Start();
            return this;
        }

        public MetricaTemporizador Para()
        {
            reloj.Stop();
            this.Valor = reloj.ElapsedMilliseconds;
            return this;
        }

        public new MetricaTemporizador AgregarPropiedad(string nombre, object valor)
        {
            base.AgregarPropiedad(nombre, valor);
            return this;
        }

        public Boolean EnEjecucion
        {
            get
            {
                return reloj.IsRunning;
            }
        }

        internal override void Preparar()
        {
            // Si el temporizador esta activo, lo paramos.
            if (EnEjecucion) Para();
        }
    }
}
