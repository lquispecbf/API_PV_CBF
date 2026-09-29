using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BE
{
    public  class BE_CambioClave
    {
        public string? ID { get; set; }
        public string? CLAVE_ACTUAL { get; set; }
        public string? CLAVE_NUEVA { get; set; }

        public string? USUARIO_MODIFICACION { get; set; }
        public string? FECHA_CAMBIO_CLAVE { get; set; }
    }
}
