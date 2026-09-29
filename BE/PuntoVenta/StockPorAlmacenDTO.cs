namespace BE.PuntoVenta;

public class StockPorAlmacenDTO
{
    public string? CODIGO { get; set; }

    public string? DESCRIPCION { get; set; }

    public string? UMD { get; set; }

    public string? LABORATORIO { get; set; }

    public decimal PRECIO { get; set; }

    public decimal PRECIO_CAJA { get; set; }

    public decimal STOCK { get; set; }

    public string? FECHA_VENCIMIENTO { get; set; }

    public string? PRINCIPIO_ACTIVO { get; set; }

    public string? ESTADO_SKU { get; set; }

    public string? OBSERVACION { get; set; }
}
