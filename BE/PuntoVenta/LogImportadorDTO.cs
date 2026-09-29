namespace BE.PuntoVenta;

public class LogImportadorDTO
{
    public int? ID { get; set; }
    public string? OV { get; set; }
    public int? DOC_ENTRY { get; set; }
    public string? FECHA_HORA { get; set; }
    public string? DESCRIPCION { get; set; }
    public string? TIPO_ERROR { get; set; }
    public string? RUC_CLIENTE { get; set; }
    public string? NOMBRE_CLIENTE { get; set; }
    public string? USUARIO_CREACION { get; set; }
    public decimal? MONTO_TOTAL { get; set; }
}
