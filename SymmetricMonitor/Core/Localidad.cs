using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SymmetricMonitor.Core
{
    public class Localidad
    {
        public string cod_localidad { get; set; }
        public string descrip { get; set; }
        public DateTime fecha_apertura { get; set; }
        public DateTime? fecha_cierre { get; set; }
        public DateTime last_update_time { get; set; }
        public string status { get; set; }

        public string descripFormatted()
        {
            string description = descrip
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
            return description;
        }
    }
}
