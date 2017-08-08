using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Redsis.EVA.Client.Common.Telemetria
{
    internal class EventoPing
    {
        int _minutosPing;
        public EventoPing(int minutosPing)
        {
            _minutosPing = minutosPing;
        }
        public void ProcesaTemporizador(Object dato)
        { 
            var minutosDesdeUltimoPing = (DateTime.Now - Telemetria.Instancia.TiempoUltimoMensaje).Minutes;
            if (minutosDesdeUltimoPing >= _minutosPing)
            {
                // Ok, se cumple tiempo para enviar ping
                var ping = new Evento("Ping");
                ping.AgregarPropiedad("nro", Telemetria.Instancia.IncrementaNroPings());
                Telemetria.Instancia.AgregaMetrica(ping);
            }
        }
    }
}
