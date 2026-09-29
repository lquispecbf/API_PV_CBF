using BE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DA.Repositorio.Repositorio_Extranet
{
    public interface ICambioClave
    {
        Task<List<BE_UsuarioExtranet>> Buscar_Usuarios(BE_UsuarioExtranet obj_Usuario);
        Task<List<BE_UsuarioExtranet>> Buscar_Usuario(BE_UsuarioExtranet obj_Usuario);
        Task<int> Actualizar_Clave(BE_UsuarioExtranet obj_Usuario);
        Task<List<BE_ListadoPerfil>> Listar_Perfiles();
    }
}
