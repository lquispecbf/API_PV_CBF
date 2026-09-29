using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BE
{
    public class BE_UsuarioExtranet
    {
        public int? IdUsuario { get; set; }
        public string? NombreUsuario { get; set; }
        public string? ClaveUsuario { get; set; }
        public string? CLIENTE_Ruc { get; set; }
        public string? CorreoElectronico { get; set; }
        public int? IdPerfil { get; set; }
        public string? Perfil { get; set; }
        public int? Estado { get; set; }
        public string? NombrePersona { get; set; }
    }
}
