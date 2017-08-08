using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Redsis.EVA.Client.Common.Telemetria
{
    public class CanalDummy : ICanal
    {
        long ICanal.MaxLatenciaEnMs
        {
            get
            {
                return 100;
            }
        }

        long ICanal.NumeroConexiones
        {
            get
            {
                return 1;
            }
        }

        long ICanal.NumeroExcepciones
        {
            get
            {
                return 0;
            }
        }

        void ICanal.Enviar(string log, string msj)
        {
            Serilog.Log.Debug("Log: [{0}], msj: [{1}]", log, msj);
        }
    }
}
