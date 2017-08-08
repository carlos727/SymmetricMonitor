using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Redsis.EVA.Client.Common.Telemetria
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Evento : Metrica
    {
        public Evento(string nombre) : base(nombre) {}

        public new Evento AgregarPropiedad(string nombre, object valor)
        {
            base.AgregarPropiedad(nombre, valor);
            return this;
        }

        public override void CompletaObjetoLog() { }
    }
}
