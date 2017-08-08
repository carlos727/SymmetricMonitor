using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Redsis.EVA.Client.Common.Telemetria
{
    public interface ICanal
    {
        long NumeroConexiones { get; }
        long NumeroExcepciones { get;  }
        long MaxLatenciaEnMs { get;  }

        void Enviar(string log, string msj);
    }
}
