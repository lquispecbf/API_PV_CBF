namespace BE.PuntoVenta;

public class ArticuloPrecioDTO
{
    public string? CODIGO_ARTICULO { get; set; }
    public decimal PRECIO { get; set; }
    public string? PROMO_TYPE { get; set; }
    public decimal? PRECIO_PROMO { get; set; }
    public decimal? CANTIDAD_PROMO { get; set; }
    public decimal? DESCUENTO_PROMO { get; set; }
    public string? UMD_PROMO { get; set; }
}
