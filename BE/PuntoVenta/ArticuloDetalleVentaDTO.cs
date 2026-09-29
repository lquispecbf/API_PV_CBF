namespace BE.PuntoVenta;

public class ArticuloDetalleVentaDTO
{
    public ArticuloBusquedaSapDTO? ARTICULO { get; set; }

    public List<UnidadMedidaArticuloSapDTO> UMDS { get; set; } = new();

    public List<PromoArticuloSapDTO> PROMOS { get; set; } = new();
    public ArticuloFraccionadoDTO? FRACCIONADO { get; set; }
}

public class ArticuloDetalleVentaItemRequestDTO
{
    public string CodigoArticulo { get; set; } = string.Empty;
    public int? CodigoUmd { get; set; }
}

public class ArticuloDetalleVentaBatchRequestDTO
{
    public List<ArticuloDetalleVentaItemRequestDTO> Items { get; set; } = new();
    public int CodigoListaPrecio { get; set; }
    public int? ListaPrecio { set { if (value.HasValue && CodigoListaPrecio == 0) CodigoListaPrecio = value.Value; } }
    public string CodigoAlmacen { get; set; } = string.Empty;
    public string? Almacen { set { if (!string.IsNullOrEmpty(value) && string.IsNullOrEmpty(CodigoAlmacen)) CodigoAlmacen = value; } }
    public string CodigoCliente { get; set; } = string.Empty;
}
