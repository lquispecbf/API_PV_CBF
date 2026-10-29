namespace BE.PuntoVenta;

public class MerchArticuloVentaDTO
{
    public int IdArticulo { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string UnidadMedida { get; set; } = "UND";
    public int StockDisponible { get; set; }
    public int IdSede { get; set; } = 2; // Almacén Nicolás Arriola
    public string RutaImagen { get; set; } = string.Empty;
}

public class MerchBusquedaFiltroDTO
{
    public string? Filtro { get; set; }
}
