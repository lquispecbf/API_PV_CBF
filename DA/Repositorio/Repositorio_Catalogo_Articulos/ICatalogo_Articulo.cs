using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BE;

namespace DA.Repositorio.Repositorio_Catalogo_Articulos
{
    public interface ICatalogo_Articulo
    {
        Task<int> GrabarCatalogoArticulo(BE_Catalogo_Articulo obj);
        Task<int> ActualizarCatalogoArticulo(BE_Catalogo_Articulo obj);
        Task<(List<BE_Catalogo_Articulo>, int)> Buscar_CatalogoProductos(BE_Catalogo_Articulo obj, int pageNumber, int pageSize);
        Task<List<BE_Catalogo_Articulo>> Obtener_Datos_CatalogoProductos(BE_Catalogo_Articulo obj);
        Task<int> Inactivar_Catalogo_Articulo(BE_Catalogo_Articulo obj);
        Task<List<BE_Catalogo_Articulo>> Buscar_Catalogo_Articulos_Tipo(BE_Catalogo_Articulo obj);
        Task<List<BE_Catalogo_Articulo>> ObtenerAutoCompletadoProducto(BE_Catalogo_Articulo obj);
    }
}
