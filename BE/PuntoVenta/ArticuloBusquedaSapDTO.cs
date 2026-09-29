namespace BE.PuntoVenta;

public class ArticuloBusquedaSapDTO
{
    public string? CODIGO { get; set; }

    public string? DESCRIPCION { get; set; }

    public string? UMD { get; set; }

    public decimal PRECIO { get; set; }

    public decimal PRECIO_CAJA { get; set; }

    public decimal STOCK { get; set; }

    public decimal STOCK_CAJAS { get; set; }

    public string? GESTIONA_LOTE { get; set; }

    public string? LOTE_PROXIMO { get; set; }

    public string? FECHA_VENCIMIENTO { get; set; }

    public string? PRINCIPIO_ACTIVO { get; set; }

    public string? ESTADO_SKU { get; set; }

    public string? OBSERVACION { get; set; }

    public string? CAJON_M { get; set; }

    public string? ESPECIFICACION { get; set; }

    public string? PROTOCOLOS { get; set; }

    public string? REGISTRO_SANITARIO { get; set; }

    public string? LABORATORIO { get; set; }

    public string? CODEBARS { get; set; }

    public string? IGV_AFECT { get; set; }

    public int UOMENTRY { get; set; }

    public decimal PRICE_BEF { get; set; }
}
