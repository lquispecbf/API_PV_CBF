namespace BE.PuntoVenta;

public class NotaCreditoClienteSapDTO
{
    public int DOCENTRY { get; set; }
    public string? NUMATCARD { get; set; }
    public int DOCNUM { get; set; }
    public string? CODIGO_CLIENTE { get; set; }
    public string? FECHA { get; set; }
    public decimal SALDO { get; set; }
    public decimal TOTAL { get; set; }
    public string? MONEDA { get; set; }
    public bool EN_USO { get; set; }
    public int? DOCENTRY_ORDEN_USO { get; set; }
    public string? DOCNUM_SAP_ORDEN_USO { get; set; }
}
