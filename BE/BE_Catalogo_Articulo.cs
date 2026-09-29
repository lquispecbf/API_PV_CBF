using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BE
{
    public class BE_Catalogo_Articulo
    {
        public string? CODIGO { get; set; }
        public string? DESCRIPCION { get; set; }
        public string? ESTADO { get; set; }
        public string? IMG_NOMBRE_ORIGINAL { get; set; }
        public string? IMG_NOMBRE_GENERADO { get; set; }
        public string? IMG_RUTA { get; set; }
        public string? PRINCIPIO_ACTIVO { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int Draw { get; set; }
        public string? TIPO { get; set; }
        public string? BUSCAR_ARTICULO { get; set; }
    }
}
