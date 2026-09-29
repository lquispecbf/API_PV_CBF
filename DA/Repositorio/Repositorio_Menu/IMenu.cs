using BE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DA.Repositorio.Repositorio_Menu
{
    public interface IMenu
    {
        Task<List<BE_Menu>> Listar_Menu();

        Task<List<BE_Menu>> Listar_Permisos_Mantenimiento(BE_Usuario obj_Usuario);
        Task<List<BE_Menu>> Listar_Permisos_Mastercard(BE_Usuario obj_Usuario);
        Task<List<BE_Menu>> Listar_Permisos_Seguridad(BE_Usuario obj_Usuario);
        Task<List<BE_Menu>> Listar_Permisos_Requerimientos(BE_Usuario obj_Usuario);
        Task<List<BE_Menu>> Listar_Permisos_Macador(BE_Usuario obj_Usuario);
        Task<List<BE_Menu>> Lista_Permisos_Recursos(BE_Usuario obj_Usuario);
        Task<List<BE_Menu>> Lista_Permisos_Comercial(BE_Usuario obj_Usuario);
        Task<List<BE_Menu>> Lista_Permisos_Finanzas(BE_Usuario obj_Usuario);
        Task<List<BE_Menu>> Listar_Permisos_Reportes(BE_Usuario obj_Usuario);
        Task<List<BE_Menu>> Listar_Permisos_PuntoVenta(BE_Usuario obj_Usuario);
        Task<List<BE_Menu>> Listar_Permisos_Sistemas(BE_Usuario obj_Usuario);
        Task<List<BE_Menu>> Listar_Permisos_Administracion(BE_Usuario obj_Usuario);

        Task<List<BE_Usuario>> Buscar_Usuario_Permiso(BE_Usuario obj_Usuario);

        Task<int> Guardar_Permiso(BE_Usuario obj_Usuario);

        Task<List<BE_Usuario>> Mostrar_Menu(BE_Usuario obj_Usuario);
        Task<List<BE_Menu>> Listar_Permisos_Facturacion(BE_Usuario obj_Usuario);
        Task<List<BE_Menu>> Listar_Permisos_Operaciones(BE_Usuario obj_Usuario);
        Task<List<BE_Menu>> Lista_Permisos_DireccionTecnica(BE_Usuario obj_Usuario);

        Task<bool> Validar_Permiso_Usuario_Accion(int idUsuario, string controller, string action);
    }
}
