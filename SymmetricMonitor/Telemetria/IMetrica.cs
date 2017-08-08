using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Redsis.EVA.Client.Common.Telemetria
{
    public interface IMetrica
    {
        DateTime TimeStamp { get; }
        string Log { get; }
        string Cliente { get; }
        string Aplicacion { get;  }
        string Funcion { get; }

    }
}
