namespace BE.PuntoVenta;

public class VentaListaDTO
{
    public string? CLIENTE { get; set; }
    public string? RUC_DNI { get; set; }
    public string? FECHA { get; set; }
    public string? VENDEDOR { get; set; }
    public string? ALMACEN { get; set; }
    public string? COMENTARIO { get; set; }
    public string? LUGAR_ENTREGA { get; set; }
    public decimal? TOTAL { get; set; }
    public string? NROSAP { get; set; }
    public string? ESTADO_ENVIO { get; set; }
    public string? USUARIO_MODIFICA { get; set; }
    public string? FECHA_HORA { get; set; }
    public string? PC { get; set; }
    public int? DOCENTRY { get; set; }
    public string? DOCSTATUS { get; set; }
    public int? DOCENTRY_SAP { get; set; }
    public int? DOCENTRY_OWTR { get; set; }
}
