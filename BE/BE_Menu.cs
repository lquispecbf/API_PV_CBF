using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BE
{
    public class BE_Menu
    {
        public int ID_MENU { get; set; }
        public string? DESCRIPCION_MENU { get; set; }
        public int ID_MENU_PADRE { get; set; }
        public int NIVEL_MENU { get; set; }
        
        public int ORDEN_MENU { get; set; }
        public string? ESTADO_MENU { get; set; }

        public string GRUPO_MENU { get; set; }

    }
}
