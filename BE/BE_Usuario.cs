using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BE
{
    public class BE_Usuario
    {
        public int ID { get; set; }
        public int ID_MENU { get; set; }
        public string ?APELLIDOS { get; set; }
        public string ?NOMBRES { get; set; }
        public string ?CORREO { get; set; }
        public string ?USUARIO { get; set; }
        public string ?CONTRASEÑA { get; set; }
        public string ?FECHA_CREACION { get; set; }
        public string ?USUARIO_CREACION { get; set; }
        public string ?FECHA_MODIFICACION { get; set; }
        public string ?USUARIO_MODIFICACION { get; set; }
        public string ?ESTADO { get; set; }
        public string ?EMPRESA { get; set; }
        public string? CLAVE { get; set; }
        public int COD_EMPRESA { get; set; }
        public int[]? PERMISOS { get; set; }

        public string? DESCRIPCION_MENU_SYS { get; set; }
        public string? TELEFONO { get; set; }

        public string? AREA { get; set; }

        public string? DEPARTAMENTO { get; set; }

        public string? PERFIL { get; set; }
        public string? CARGO { get; set; }

        public string? GRUPO_MENU { get; set; }

        public string? NIVEL { get; set; }

        public string? FIRMA { get; set; }
        public int? CODIGO_VENDEDOR_SAP { get; set; }

        public string? FECHA_CAMBIO_CLAVE { get; set; }
        public string? FORZAR_CAMBIO_CLAVE { get; set; }
        public string? ULTIMO_CAMBIO_CLAVE { get; set; }

        public int? DIAS_RESTANTES_CLAVE { get; set; }
        public string? PROXIMO_VENCER { get; set; }
        public bool BLOQUEADO { get; set; }
        public int INTENTOS { get; set; }

        public long? IDEMPLEADO { get; set; }   // 👈 nuevo
    }
}
