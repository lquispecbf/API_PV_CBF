namespace BE.PuntoVenta;

public class PromoArticuloSapDTO
{
    public string? CODIGO_ARTICULO { get; set; }
    public string? PROMO_TYPE { get; set; }
    public decimal PRECIO { get; set; }
    public decimal CANTIDAD { get; set; }
    public decimal DESCUENTO { get; set; }
    public string? WMS_GIF { get; set; }
    public string? UMD { get; set; }
    public string? CODIGO_LISTA_PRECIO { get; set; }
}
