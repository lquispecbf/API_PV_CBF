namespace BE.PuntoVenta;

public class VentaDetalleDTO
{
    public int LINE_ID { get; set; }
    public string? ITEMCODE { get; set; }
    public string? ITEMNAME { get; set; }
    public string? CODEBARS { get; set; }
    public int QUANTITY { get; set; }
    public decimal PRICE { get; set; }
    public decimal DSCT_PERCENT { get; set; }
    public string? IGV_AFECT { get; set; }
    public decimal LINE_TOTAL { get; set; }
    public decimal FORSALE { get; set; }
    public int UOMENTRY { get; set; }
    public string? UMD_NOMBRE { get; set; }
    public decimal QUANTITY_UOM { get; set; }
    public int TOTAL_UOM { get; set; }
    public decimal PRICE_UOM { get; set; }
    public int UGPENTRY { get; set; }
    public decimal PRICE_BEF { get; set; }
    public string? PRICE_LIST { get; set; }
    public string? WMS_GIF { get; set; }
    public decimal WMS_DSC { get; set; }
    public string? TIPO_CONTROLADO { get; set; } = "01";

    public List<VentaLoteDTO> LOTES { get; set; } = new();
}
