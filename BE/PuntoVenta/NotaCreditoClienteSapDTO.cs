namespace BE.PuntoVenta;

public class NotaCreditoClienteSapDTO
{
    public int DOCENTRY { get; set; }
    public int DOCNUM { get; set; }
    public string? CODIGO_CLIENTE { get; set; }
    public string? FECHA { get; set; }
    public decimal TOTAL { get; set; }
    public string? MONEDA { get; set; }
}
