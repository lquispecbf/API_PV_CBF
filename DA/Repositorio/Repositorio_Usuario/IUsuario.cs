using BE;
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DA.Repositorio.Repositorio_Usuario
{
    public interface IUsuario
    {

        Task<List<BE_Usuario>> Buscar_Usuarios(BE_Usuario obj_Usuario);
        Task<List<BE_Usuario>> Obtener_Usuarios(BE_Usuario obj_Usuario);
        Task<int> Actualizar_Usuarios(BE_Usuario obj_Usuario);
        Task<int> Grabar_Usuarios(BE_Usuario obj_Usuario);
        Task<int> Cambiar_Clave(BE_Usuario obj_Usuario);
        Task<int> Inactivar_Usuario(BE_Usuario obj_Usuario);

        Task<string> Actualizar_ImagenSolicitud(string id, string imgAdjunto);
        Task<string> ObtenerRutaArchivo(string id);

        Task<List<string>> Obtener_Historial_Claves(BE_CambioClave obj);  
        //Task<int> Forzar_Cambio_Clave_Todos(BE_Usuario obj_Usuario);

        Task<int> Cambiar_Clave_Segura(BE_CambioClave obj_Usuario);

        Task<string> Obtener_Clave_Usuario(int id);

        Task <List<BE_Empleado>> Autocompletar_Empleado(BE_Empleado empleado);
        Task<BE_Empleado> ObtenerEmpleadoPorId(long idEmpleado);

    }
}
