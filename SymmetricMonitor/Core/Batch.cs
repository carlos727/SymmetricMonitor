using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SymmetricMonitor.Core
{
    class Batch
    {
        // Batch attributes
        public int? batch_id { get; set; } = 0;
        public string channel_id { get; set; } = "";
        public string status { get; set; } = "";
        public int? byte_count { get; set; } = 0;
        public string sql_state { get; set; } = "";
        public int? sql_code { get; set; } = 0;
        public string sql_message { get; set; } = "";
        public DateTime? last_update_time { get; set; }
        // Localidad attributes
        public string cod_localidad { get; set; }
        public string descrip { get; set; }
        public DateTime? fecha_apertura { get; set; }

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