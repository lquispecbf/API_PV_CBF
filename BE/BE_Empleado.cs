using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BE
{
    public class BE_Empleado
    {

        public long IDEMPLEADO { get; set; }
        public string NOMBRECOMPLETO { get; set; }
        public string NOMBRE { get; set; }
        public string APELLIDOS { get; set; }
        public string CARGO { get; set; }
        public string DOCUMENTO { get; set; }

        public string AREA { get; set; }

        public bool TIENE_USUARIO { get; set; }
    }
}
