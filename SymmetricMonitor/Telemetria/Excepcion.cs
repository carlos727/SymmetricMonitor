using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Redsis.EVA.Client.Common.Telemetria
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Excepcion : Metrica
    {
        //[JsonProperty]
        private Exception e = null;
        public Excepcion(Exception e)
        {
            Nivel = "err";
            this.e = e;
            Operacion = e.GetType().Name;
            PropiedadesIni();
        }

        private void PropiedadesIni()
        {
            Props.Add("msj", messageFormatted());
            Props.Add("dato", e.Data);
            Props.Add("hResult", e.HResult);
            Props.Add("source", e.Source);
            //Props.Add("stackTrace", e.StackTrace);
            Props.Add("targetSite", e.TargetSite);

            if (e.InnerException != null)
            {
                Props.Add("innerException_tipo", e.InnerException.GetType().Name);
                Props.Add("innerException_msj", innerMessageFormatted());
                Props.Add("innerException_dato", e.InnerException.Data);
                Props.Add("innerException_hResult", e.InnerException.HResult);
                Props.Add("innerException_source", e.InnerException.Source);
                Props.Add("innerException_stackTrace", e.InnerException.StackTrace);
                Props.Add("innerException_targetSite", e.InnerException.TargetSite);
            }
        }

        public override void CompletaObjetoLog() { }

        public new Excepcion AgregarPropiedad(string nombre, object valor)
        {
            base.AgregarPropiedad(nombre, valor);
            return this;
        }

        internal string messageFormatted()
        {
            string message = this.e.Message
                                    .Replace("á", "a")
                                    .Replace("é", "e")
                                    .Replace("í", "i")
                                    .Replace("ó", "o")
                                    .Replace("ú", "u")
                                    .Replace("ñ", "n")
                                    .Replace("Á", "A")
                                    .Replace("É", "E")
                                    .Replace("Í", "I")
                                    .Replace("Ó", "O")
                                    .Replace("Ú", "U")
                                    .Replace("Ñ", "N");
            return message;
        }

        internal string innerMessageFormatted()
        {
            string message = this.e.InnerException.Message
                                    .Replace("á", "a")
                                    .Replace("é", "e")
                                    .Replace("í", "i")
                                    .Replace("ó", "o")
                                    .Replace("ú", "u")
                                    .Replace("ñ", "n")
                                    .Replace("Á", "A")
                                    .Replace("É", "E")
                                    .Replace("Í", "I")
                                    .Replace("Ó", "O")
                                    .Replace("Ú", "U")
                                    .Replace("Ñ", "N");
            return message;
        }
    }
}
