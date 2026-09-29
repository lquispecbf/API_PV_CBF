namespace BE.PuntoVenta;

public class VentaLoteCargadoDTO
{
    public int LINE_HEADER { get; set; }
    public string? ITEMCODE { get; set; }
    public decimal QUANTITY { get; set; }
    public string? SYSNUMBER { get; set; }
    public string? DISTNUMBER { get; set; }
}
